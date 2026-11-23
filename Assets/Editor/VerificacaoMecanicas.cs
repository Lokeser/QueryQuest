// Assets/Editor/VerificacaoMecanicas.cs
// Bateria de verificação técnica das mecânicas do QueryQuest (base do quadro
// de verificação do artigo). Roda o jogo de verdade em Play Mode, na cena de
// combate, e confere cada mecânica pelas rotinas reais do jogo.
//
// Pelo editor:  menu QueryQuest > Executar verificação das mecânicas
// Pela linha de comando:
//   Unity.exe -batchmode -projectPath <projeto> -executeMethod VerificacaoMecanicas.Executar
//
// O relatório sai no Console e em Verificacao/resultado_verificacao.txt.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using QueryQuest.Combat;
using QueryQuest.Database;
using QueryQuest.Models;
using QueryQuest.UI;

public static class VerificacaoMecanicas
{
    const string Flag = "QQ_VERIFICACAO_MECANICAS";
    const string Cena = "Assets/Scenes/SampleScene.unity";

    static readonly StringBuilder Rel = new StringBuilder();
    static readonly List<string> Excecoes = new List<string>();
    static int _ok, _falha;
    static IEnumerator _rotina;
    static double _esperarAte;
    static int _quadros;

    [MenuItem("QueryQuest/Executar verificação das mecânicas")]
    public static void Executar()
    {
        EditorPrefs.SetInt(Flag, 1);
        EditorSceneManager.OpenScene(Cena);
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    static void Ligar()
    {
        if (EditorPrefs.GetInt(Flag, 0) != 1) return;
        Application.logMessageReceived += (msg, pilha, tipo) =>
        {
            if (tipo == LogType.Exception) Excecoes.Add(msg);
        };
        EditorApplication.update += Tick;
    }

    // Executor mínimo de rotina: yield de um double = esperar N segundos;
    // yield null = esperar um quadro.
    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        if (++_quadros < 150 || CombatManager.Instance == null ||
            CombatManager.Instance.CurrentState == CombatState.IDLE) return;

        if (_rotina == null) _rotina = Bateria();
        if (EditorApplication.timeSinceStartup < _esperarAte) return;

        bool continua;
        try { continua = _rotina.MoveNext(); }
        catch (Exception e) { Linha($"  !! a bateria parou: {e}"); _falha++; continua = false; }

        if (continua)
        {
            if (_rotina.Current is double s) _esperarAte = EditorApplication.timeSinceStartup + s;
            return;
        }
        Encerrar();
    }

    static Dictionary<int, int> _progresso;

    static void Encerrar()
    {
        EditorApplication.update -= Tick;
        if (_progresso != null)
            foreach (var kv in _progresso)
                DatabaseManager.Instance.DB.Execute("UPDATE Magias SET Desbloqueado = ? WHERE Id = ?", kv.Value, kv.Key);
        EditorPrefs.DeleteKey(Flag);

        Linha("");
        Linha($"Exceções registradas durante toda a execução: {Excecoes.Count}");
        foreach (var e in Excecoes.Distinct()) Linha("  - " + e);
        Linha($"TOTAL: {_ok} verificações aprovadas, {_falha} reprovadas.");

        string pasta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Verificacao"));
        Directory.CreateDirectory(pasta);
        File.WriteAllText(Path.Combine(pasta, "resultado_verificacao.txt"), Rel.ToString(), new UTF8Encoding(false));
        Debug.Log("<<<VERIFICACAO>>>\n" + Rel);

        if (Application.isBatchMode) EditorApplication.Exit(_falha == 0 && Excecoes.Count == 0 ? 0 : 1);
        else EditorApplication.isPlaying = false;
    }

    static void Linha(string s) => Rel.AppendLine(s);

    static void Conferir(bool ok, string descricao)
    {
        if (ok) _ok++; else _falha++;
        Linha($"  [{(ok ? "OK" : "FALHA")}] {descricao}");
    }

    // ─────────────────────────────────────────────────────────────────────

    static IEnumerator Bateria()
    {
        Linha("VERIFICAÇÃO TÉCNICA DAS MECÂNICAS — QueryQuest");
        Linha($"Data: {DateTime.Now:dd/MM/yyyy HH:mm} | Unity {Application.unityVersion} | cena {Cena}");
        Linha("");

        // Parte do estado de uma partida nova (mesma rotina do NOVO JOGO) e
        // guarda o progresso salvo para devolvê-lo no fim.
        var db = DatabaseManager.Instance.DB;
        _progresso = db.Query<SpellData>("SELECT * FROM Magias").ToDictionary(m => m.Id, m => m.Desbloqueado);
        typeof(FloorManager).GetMethod("ResetSpellUnlocks", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(FloorManager.Instance, null);

        Interpretador();
        Juncao();
        Elementos();
        Recompensas();
        yield return null;

        var custo = CustoEDano();
        while (custo.MoveNext()) yield return custo.Current;

        var turno = Turnos();
        while (turno.MoveNext()) yield return turno.Current;
    }

    // ── 1. Interpretador ─────────────────────────────────────────────────

    static void Interpretador()
    {
        Linha("1. INTERPRETADOR SQL — rejeição de consultas inválidas e aceitação das válidas");
        var interp = new SQLInterpreter(DatabaseManager.Instance.DB);

        var invalidas = new (string cat, string sql)[]
        {
            ("sintático", "SELEC * FROM Magias"),
            ("sintático", "SELECT * Magias"),
            ("sintático", "SELECT * FROM Magias WHERE"),
            ("sintático", "SELECT * FROM Magias WHERE Elemento = 'Fogo' AND"),
            ("sintático", "SELECT * FROM Magias WHERE Elemento = 'Fogo' OR"),
            ("sintático", "SELECT * FROM Magias WHERE AND Elemento = 'Fogo'"),
            ("sintático", "SELECT * FROM Magias WHERE Elemento = 'Fogo' AND AND Nivel = 1"),
            ("semântico", "SELECT Vida FROM Magias"),
            ("semântico", "SELECT * FROM Feiticos"),
            ("semântico", "SELECT * FROM Magias WHERE Cor = 'Azul'"),
        };
        foreach (var (cat, sql) in invalidas)
        {
            var r = interp.Execute(sql);
            Conferir(!r.Success, $"rejeita ({cat}) {sql}");
        }

        var validas = new (string sql, int esperado)[]
        {
            ("SELECT * FROM Magias", 18),
            ("SELECT * FROM Magias WHERE Elemento = 'Fogo'", 4),
            ("SELECT Nome FROM Magias WHERE Elemento = 'Fogo' AND Nivel = 1", 2),
            ("SELECT * FROM Magias WHERE Elemento = 'Fogo' OR Elemento = 'Agua'", 7),
            ("SELECT * FROM Magias WHERE DanoBase >= 70", 4),
            ("SELECT * FROM Magias ORDER BY DanoBase DESC LIMIT 1", 1),
            ("SELECT * FROM Magias WHERE Nome = 'Jato d'Agua'", 1),
        };
        foreach (var (sql, esperado) in validas)
        {
            var r = interp.Execute(sql);
            Conferir(r.Success && r.ResultCount == esperado,
                     $"aceita e devolve {esperado} linha(s): {sql}  (obtido: {(r.Success ? r.ResultCount.ToString() : "erro: " + r.ErrorMessage)})");
        }

        var topo = interp.Execute("SELECT * FROM Magias ORDER BY DanoBase DESC LIMIT 1");
        Conferir(topo.SelectedSpell?.Nome == "Tempestade",
                 $"ORDER BY DanoBase DESC LIMIT 1 seleciona a magia de maior dano (obtido: {topo.SelectedSpell?.Nome})");
        Linha("");
    }

    // ── 2. JOIN da absorção ──────────────────────────────────────────────

    static void Juncao()
    {
        Linha("2. ABSORÇÃO DE FRAGMENTOS — validação do JOIN");
        var validar = typeof(FragmentosUI).GetMethod("ValidarJoin", BindingFlags.NonPublic | BindingFlags.Static);
        var frag = new FragmentoData { FragmentoID = 1, InimigoID = 1, Nome = "Essência Flamejante" };

        (bool ok, string erro) Testar(string sql) => ((bool, string))validar.Invoke(null, new object[] { sql, frag });

        const string ok = "SELECT f.Nome, i.Nome FROM Fragmentos f JOIN Inimigos i ON f.InimigoID = i.Id WHERE f.FragmentoID = 1";
        Conferir(Testar(ok).ok, "aceita: " + ok);
        Conferir(Testar("SELECT * FROM Fragmentos f JOIN Inimigos i ON i.Id = f.InimigoID WHERE f.FragmentoID = 1").ok,
                 "aceita a ligação escrita na ordem inversa (i.Id = f.InimigoID)");

        var recusas = new (string motivo, string sql)[]
        {
            ("sem JOIN", "SELECT * FROM Fragmentos WHERE FragmentoID = 1"),
            ("sem ON", "SELECT * FROM Fragmentos f JOIN Inimigos i WHERE f.FragmentoID = 1"),
            ("chave de ligação errada", "SELECT * FROM Fragmentos f JOIN Inimigos i ON f.FragmentoID = i.Id WHERE f.FragmentoID = 1"),
            ("sem filtrar o fragmento", "SELECT * FROM Fragmentos f JOIN Inimigos i ON f.InimigoID = i.Id"),
            ("comando de escrita", "DELETE FROM Fragmentos"),
            ("dois comandos", "SELECT * FROM Fragmentos f JOIN Inimigos i ON f.InimigoID = i.Id WHERE f.FragmentoID = 1; DROP TABLE Inimigos"),
        };
        foreach (var (motivo, sql) in recusas)
        {
            var r = Testar(sql);
            Conferir(!r.ok, $"recusa ({motivo}) — dica exibida: \"{r.erro}\"");
        }
        Linha("");
    }

    // ── 3. Tabela de elementos ───────────────────────────────────────────

    static void Elementos()
    {
        Linha("3. AFINIDADE ELEMENTAL — 25 combinações atacante x defensor");
        string[] el = { "Fogo", "Agua", "Vento", "Terra", "Raio" };
        int forte = 0, neutro = 0, resiste = 0, outros = 0;
        foreach (var a in el)
            foreach (var d in el)
            {
                float m = ElementalSystem.GetMultiplier(a, d);
                if (Mathf.Approximately(m, 1.5f)) forte++;
                else if (Mathf.Approximately(m, 1.0f)) neutro++;
                else if (Mathf.Approximately(m, 0.75f)) resiste++;
                else outros++;
            }
        Conferir(forte == 5 && resiste == 5 && neutro == 15 && outros == 0,
                 $"multiplicadores: {forte} x1,5 (fraqueza) | {neutro} x1,0 (neutro) | {resiste} x0,75 (resistência) | {outros} outros");
        Linha("");
    }

    // ── 4. Recompensas ───────────────────────────────────────────────────

    static string Chave(RewardItem i) => i.Type switch
    {
        RewardType.Armor => "Armadura",
        RewardType.Staff => "Cajado:" + i.Element,
        RewardType.Page  => "Pagina:" + i.SpellId,
        _ => "?",
    };

    static void Recompensas()
    {
        Linha("4. ESCOLHA DE RECOMPENSAS — sorteio de 3 cartas");
        var db = DatabaseManager.Instance.DB;
        int bloqueadas = db.ExecuteScalar<int>("SELECT COUNT(*) FROM Magias WHERE Desbloqueado = 0");
        Conferir(bloqueadas == 10, $"magias bloqueadas no início da partida: {bloqueadas} (esperado 10)");

        const int telas = 3000;
        int telasOk = 0;
        var vistas = new Dictionary<string, int>();
        for (int t = 0; t < telas; t++)
        {
            var itens = RewardGenerator.Generate(3);
            var chaves = itens.Select(Chave).ToList();
            if (itens.Count == 3 && chaves.Distinct().Count() == 3) telasOk++;
            foreach (var c in chaves) vistas[c] = vistas.TryGetValue(c, out int n) ? n + 1 : 1;
        }
        Conferir(telasOk == telas, $"{telasOk} de {telas} telas com 3 cartas distintas");
        Conferir(vistas.Count == 16,
                 $"cartas diferentes que apareceram: {vistas.Count} (1 armadura + 5 cajados + 10 páginas = 16)");
        Linha($"       frequência mínima/máxima de uma carta em {telas * 3} sorteios: {vistas.Values.Min()} / {vistas.Values.Max()}");

        // A página sai do sorteio quando a magia é desbloqueada
        var meteoro = db.Query<SpellData>("SELECT * FROM Magias WHERE Nome = 'Meteoro'")[0];
        db.Execute("UPDATE Magias SET Desbloqueado = 1 WHERE Id = ?", meteoro.Id);
        int aparicoes = 0;
        for (int t = 0; t < 1000; t++)
            aparicoes += RewardGenerator.Generate(3).Count(i => i.Type == RewardType.Page && i.SpellId == meteoro.Id);
        db.Execute("UPDATE Magias SET Desbloqueado = 0 WHERE Id = ?", meteoro.Id);
        Conferir(aparicoes == 0, $"página de magia já desbloqueada não volta ao sorteio (aparições em 1000 telas: {aparicoes})");
        Linha("");
    }

    // ── 5 e 6. Custo de mana e dano, pelo fluxo real do combate ──────────

    static void Definir(object alvo, string prop, object valor) =>
        alvo.GetType().GetProperty(prop).GetSetMethod(true).Invoke(alvo, new[] { valor });

    static IEnumerator CustoEDano()
    {
        var cm = CombatManager.Instance;
        var mana = ManaSystem.Instance;
        var slots = SlotSystem.Instance;
        var db = DatabaseManager.Instance.DB;

        Linha("5. CUSTO DE MANA — previsão da barra do grimório x mana efetivamente cobrada");
        Linha($"   inimigo: {cm.CurrentEnemy.Nome} (elemento {cm.CurrentEnemy.Elemento}, fraqueza de distância {cm.CurrentEnemy.FraquezaDistancia})");

        // Tsunami (Água, LONGO) só para o teste da fraqueza de distância
        db.Execute("UPDATE Magias SET Desbloqueado = 1 WHERE Nome = 'Tsunami'");

        // (consulta, distância jogador-golem, dano esperado; -1 = só custo)
        var casos = new (string sql, int dist, int dano, string nota)[]
        {
            ("SELECT * FROM Magias WHERE Nome = 'Jato d'Agua'",                                  2, 42,  "Água x Fogo: 28 x 1,5"),
            ("SELECT * FROM Magias WHERE Elemento = 'Vento'",                                      2, 19,  "Vento x Fogo: 25 x 0,75 (resistência)"),
            ("SELECT * FROM Magias WHERE Elemento = 'Terra' AND Nivel = 1",                        1, 40,  "Terra x Fogo: 40 x 1,0 (neutro)"),
            ("SELECT * FROM Magias WHERE Elemento = 'Agua' AND Distancia = 'LONGO'",               3, 168, "Tsunami LONGO: 75 x 1,5 = 112, +50% pela fraqueza LONGO"),
            ("SELECT * FROM Magias WHERE Elemento = 'Terra' AND Nivel <= 1 ORDER BY DanoBase DESC LIMIT 1", 4, 0, "Púa de Pedra (CURTO) a 4 slots: fora da área, erra"),
            ("SELECT * FROM Inimigos",                                                             4, -1,  "análise de 7 colunas"),
            ("SELECT FraquezaElemento, FraquezaDistancia FROM Inimigos",                           4, -1,  "análise de 2 colunas"),
            ("SELECT Elemento FROM Inimigos",                                                      4, -1,  "análise de 1 coluna"),
        };

        var danos = new List<(string sql, int esperado, int obtido, string nota)>();
        foreach (var c in casos)
        {
            // Estado limpo para cada consulta: turno do jogador, mana cheia,
            // magia do turno disponível e golem com vida de sobra.
            for (int k = 0; k < 120 && cm.CurrentState != CombatState.PLAYER_TURN; k++) yield return 0.05;
            Definir(slots, "PlayerSlot", 1);
            Definir(slots, "EnemySlot", 1 + c.dist);
            Definir(cm, "SpellUsedThisTurn", false);
            Definir(cm, "EnemyCurrentHP", 9999);
            mana.ResetMana();

            int previsto = ManaSystem.Prever(c.sql).Custo;
            int manaAntes = mana.CurrentMana, hpAntes = cm.EnemyCurrentHP;

            cm.OpenGrimoire();
            cm.SubmitQuery(c.sql);

            int cobrado = manaAntes - mana.CurrentMana;
            Conferir(cobrado == previsto, $"previsto {previsto,2} | cobrado {cobrado,2} | {c.sql}");
            if (c.dano >= 0) danos.Add((c.sql, c.dano, hpAntes - cm.EnemyCurrentHP, c.nota));
            yield return 0.1;
        }
        db.Execute("UPDATE Magias SET Desbloqueado = 0 WHERE Nome = 'Tsunami'");
        Linha("");

        Linha("6. DANO — elemento, área de alcance e fraqueza de distância, no combate real");
        foreach (var d in danos)
            Conferir(d.esperado == d.obtido, $"dano esperado {d.esperado,3} | causado {d.obtido,3} | {d.nota}");
        Linha("");

        // Devolve a arena ao normal para o teste de turnos
        for (int k = 0; k < 120 && cm.CurrentState != CombatState.PLAYER_TURN; k++) yield return 0.05;
        Definir(slots, "PlayerSlot", 1);
        Definir(slots, "EnemySlot", 6);
        Definir(cm, "EnemyCurrentHP", cm.CurrentEnemy.HP);
    }

    // ── 7. Ciclo de turnos ───────────────────────────────────────────────

    static IEnumerator Turnos()
    {
        Linha("7. CICLO DE TURNOS — ENCERRAR TURNO devolve a vez ao jogador");
        var cm = CombatManager.Instance;
        for (int t = 1; t <= 3; t++)
        {
            cm.OpenGrimoire();          // encerrar com o grimório aberto é o caso que já travou
            cm.EndTurn();
            bool passouPeloGolem = cm.CurrentState == CombatState.ENEMY_TURN;
            double limite = EditorApplication.timeSinceStartup + 6;
            while (cm.CurrentState != CombatState.PLAYER_TURN && EditorApplication.timeSinceStartup < limite)
                yield return 0.05;
            Conferir(passouPeloGolem && cm.CurrentState == CombatState.PLAYER_TURN,
                     $"turno {t}: jogador -> golem -> jogador (estado final: {cm.CurrentState})");
        }
    }
}
