// Assets/_QueryQuest/UI/FragmentosUI.cs
// Cena de absorção, depois de derrotar um golem.
//
// O jogador ESCREVE o JOIN. Não existe botão que monta a consulta por ele:
// ele escolhe um fragmento, digita a consulta que cruza Fragmentos com
// Inimigos e, se estiver correta, a consulta roda de verdade no SQLite e o
// fragmento é absorvido — liberando a magia de NÍVEL 2 daquele elemento.
//
// Botão DICA dá três degraus de ajuda, terminando no exemplo completo.
//
// Toda a UI é criada por código; abre pela cena de pós-vitória.

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QueryQuest.Combat;
using QueryQuest.Database;
using QueryQuest.Models;

namespace QueryQuest.UI
{
    public class FragmentosUI : MonoBehaviour
    {
        public const int CustoMana = 20;

        private static readonly Color Ink     = new Color(0.24f, 0.15f, 0.06f);
        private static readonly Color Realce  = new Color(0.42f, 0.20f, 0.04f);
        private static readonly Color Erro    = new Color(0.60f, 0.12f, 0.08f);
        private static readonly Color Sucesso = new Color(0.12f, 0.40f, 0.16f);
        private static readonly Color Papel   = new Color(0.99f, 0.96f, 0.87f, 0.95f);

        public static FragmentosUI Instance { get; private set; }

        private RectTransform _lista;
        private TextMeshProUGUI _titulo, _selecionadoTxt, _dicaTxt, _resultadoTxt;
        private TMP_InputField _input;
        private FragmentoData _selecionado;
        private int _nivelDica;

        // ─────────────────────────────────────────────────────────────────────
        // CONSTRUÇÃO
        // ─────────────────────────────────────────────────────────────────────

        public static FragmentosUI Create(Transform canvas)
        {
            var go = new GameObject("FragmentosPanel", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var ui = go.AddComponent<FragmentosUI>();
            ui.Build();
            go.SetActive(false);
            return ui;
        }

        private void Awake()
        {
            Instance = this;
            FragmentDropSystem.OnAbsorbPhaseStarted += AbrirFase;
        }

        private void OnDestroy() => FragmentDropSystem.OnAbsorbPhaseStarted -= AbrirFase;

        private void Build()
        {
            var raiz = (RectTransform)transform;
            raiz.anchorMin = raiz.anchorMax = new Vector2(0.5f, 0.5f);
            raiz.pivot = new Vector2(0.5f, 0.5f);
            raiz.sizeDelta = new Vector2(1020f, 660f);
            raiz.anchoredPosition = Vector2.zero;

            var fundo = gameObject.AddComponent<Image>();
            var moldura = Resources.Load<Sprite>("Sprites/UI/hud_painel");
            if (moldura != null) UiFrame.Apply(fundo, moldura);
            else fundo.color = new Color(0.96f, 0.90f, 0.74f);

            _titulo = Texto(raiz, "Titulo", "O GOLEM DEIXOU SUAS ESSENCIAS", 24f, Realce,
                            0.06f, 0.05f, 0.94f, 0.12f);

            // ── Coluna esquerda: os fragmentos que caíram ──
            Texto(raiz, "LabelLista", "Fragmentos", 17f, Ink, 0.06f, 0.14f, 0.36f, 0.19f);

            _lista = Caixa(raiz, "Lista", 0.06f, 0.19f, 0.36f, 0.62f);
            var layout = _lista.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            _selecionadoTxt = Texto(raiz, "Selecionado", "", 15f, Ink, 0.06f, 0.63f, 0.36f, 0.82f);
            _selecionadoTxt.alignment = TextAlignmentOptions.TopLeft;

            // ── Coluna direita: escreva o JOIN ──
            Texto(raiz, "LabelJoin", "Escreva o JOIN que traz o fragmento e quem o largou:",
                  16f, Realce, 0.39f, 0.14f, 0.94f, 0.20f).alignment = TextAlignmentOptions.Left;

            _input = CriarInput(raiz, 0.39f, 0.20f, 0.94f, 0.44f);

            _dicaTxt = Texto(raiz, "Dica", "", 15f, new Color(0.40f, 0.30f, 0.14f),
                             0.39f, 0.45f, 0.94f, 0.66f);
            _dicaTxt.alignment = TextAlignmentOptions.TopLeft;

            _resultadoTxt = Texto(raiz, "Resultado", "", 15f, Ink, 0.39f, 0.67f, 0.94f, 0.85f);
            _resultadoTxt.alignment = TextAlignmentOptions.TopLeft;

            // ── Botões ──
            Botao(raiz, "CONTINUAR", 0.06f, 0.87f, 0.30f, 0.95f, Continuar);
            Botao(raiz, "DICA",      0.38f, 0.87f, 0.56f, 0.95f, ProximaDica);
            Botao(raiz, $"ABSORVER ({CustoMana} mana)", 0.62f, 0.87f, 0.94f, 0.95f, Absorver);
        }

        // ─────────────────────────────────────────────────────────────────────
        // ABRIR / FECHAR
        // ─────────────────────────────────────────────────────────────────────

        private void AbrirFase(List<FragmentoData> caidos)
        {
            if (_titulo != null)
                _titulo.text = caidos != null && caidos.Count == 1
                    ? $"O GOLEM DEIXOU: {caidos[0].Nome.ToUpper()}"
                    : "O GOLEM DEIXOU SUAS ESSENCIAS";

            _resultadoTxt.text = "Escolha um fragmento e escreva a consulta para absorve-lo.";
            _resultadoTxt.color = Ink;
            Abrir();
        }

        public void Abrir()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Refresh();
        }

        public void Fechar() => gameObject.SetActive(false);

        private void Continuar()
        {
            Fechar();
            FragmentDropSystem.Instance?.FinishPhase();
        }

        private void OnEnable()
        {
            if (InventarioFragmento.Instance != null)
                InventarioFragmento.Instance.OnInventarioChanged += Refresh;
        }

        private void OnDisable()
        {
            if (InventarioFragmento.Instance != null)
                InventarioFragmento.Instance.OnInventarioChanged -= Refresh;
        }

        private void Refresh()
        {
            foreach (Transform child in _lista) Destroy(child.gameObject);

            var fragmentos = InventarioFragmento.Instance?.ObterTodos() ?? new List<FragmentoData>();
            if (fragmentos.Count == 0)
            {
                var vazio = Texto(_lista, "Vazio", "Nenhum fragmento.", 14f, Ink, 0f, 0f, 1f, 1f);
                vazio.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
                _selecionado = null;
                AtualizarSelecionado();
                return;
            }

            foreach (var frag in fragmentos)
            {
                var f = frag;
                int qtd = InventarioFragmento.Instance.Quantidade(f.FragmentoID);
                string rotulo = $"{f.Nome}  R{f.Raridade}" + (qtd > 1 ? $" x{qtd}" : "");

                var btn = Botao(_lista, rotulo, 0f, 0f, 1f, 1f, () => Selecionar(f), 14f);
                var le = btn.gameObject.AddComponent<LayoutElement>();
                le.minHeight = le.preferredHeight = 34f;
            }

            if (_selecionado != null && InventarioFragmento.Instance.Quantidade(_selecionado.FragmentoID) <= 0)
                _selecionado = null;

            AtualizarSelecionado();
        }

        private void Selecionar(FragmentoData frag)
        {
            _selecionado = frag;
            _nivelDica = 0;
            _dicaTxt.text = "";
            AtualizarSelecionado();
        }

        private void AtualizarSelecionado()
        {
            if (_selecionadoTxt == null) return;

            if (_selecionado == null)
            {
                _selecionadoTxt.text = "<i>Nenhum fragmento selecionado.</i>";
                return;
            }

            _selecionadoTxt.text =
                $"<b>{_selecionado.Nome}</b>\n" +
                $"Elemento: {_selecionado.Elemento}\n" +
                $"FragmentoID: <b>{_selecionado.FragmentoID}</b>\n" +
                $"<size=90%><i>{_selecionado.Descricao}</i></size>";
        }

        // ─────────────────────────────────────────────────────────────────────
        // DICAS (três degraus, do conceito ao exemplo)
        // ─────────────────────────────────────────────────────────────────────

        private void ProximaDica()
        {
            if (_selecionado == null)
            {
                _dicaTxt.text = "Escolha um fragmento primeiro.";
                return;
            }

            _nivelDica = Mathf.Min(_nivelDica + 1, 3);
            int id = _selecionado.FragmentoID;

            switch (_nivelDica)
            {
                case 1:
                    _dicaTxt.text =
                        "<b>Dica 1/3</b> — A informacao esta em DUAS tabelas: <b>Fragmentos</b> " +
                        "(o item que caiu) e <b>Inimigos</b> (quem largou). Uma consulta que le " +
                        "as duas de uma vez usa <b>JOIN</b>.";
                    break;

                case 2:
                    _dicaTxt.text =
                        "<b>Dica 2/3</b> — A ponte entre elas e a coluna <b>InimigoID</b> de " +
                        "Fragmentos, que aponta para o <b>Id</b> de Inimigos. E o ON diz isso:\n" +
                        "<i>... JOIN Inimigos i ON f.InimigoID = i.Id</i>\n" +
                        $"Nao esqueca de filtrar so este fragmento: <b>WHERE f.FragmentoID = {id}</b>";
                    break;

                default:
                    _dicaTxt.text =
                        "<b>Dica 3/3</b> — Exemplo completo:\n" +
                        "<color=#6B3410>SELECT f.Nome, i.Nome AS Inimigo\n" +
                        "FROM Fragmentos f\n" +
                        $"JOIN Inimigos i ON f.InimigoID = i.Id\n" +
                        $"WHERE f.FragmentoID = {id}</color>";
                    break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // ABSORVER (valida o JOIN escrito pelo jogador)
        // ─────────────────────────────────────────────────────────────────────

        private void Absorver()
        {
            var frag = _selecionado;
            if (frag == null)
            {
                Falha("Escolha um fragmento na lista.");
                return;
            }

            string consulta = _input != null ? _input.text : "";
            var (ok, erro) = ValidarJoin(consulta, frag);
            if (!ok)
            {
                Falha(erro);
                return;
            }

            var mana = ManaSystem.Instance;
            if (mana != null && !mana.HasMana(CustoMana))
            {
                Falha($"Mana insuficiente. Absorver custa {CustoMana}.");
                return;
            }

            var db = DatabaseManager.Instance?.DB;
            if (db == null) { Falha("Banco indisponivel."); return; }

            List<FragmentoJoinRow> linhas;
            try
            {
                linhas = db.Query<FragmentoJoinRow>(consulta.Replace("\n", " ").Replace("\r", " "));
            }
            catch (System.Exception e)
            {
                Falha($"O SQLite recusou a consulta: {e.Message}");
                return;
            }

            if (linhas == null || linhas.Count == 0)
            {
                Falha("A consulta rodou, mas nao devolveu nenhuma linha. " +
                      "Confira o ON e o WHERE.");
                return;
            }

            mana?.SpendMana(CustoMana);

            var sb = new StringBuilder();
            sb.AppendLine("<b>JOIN aceito!</b>");
            sb.AppendLine(linhas[0].Describe().TrimEnd());

            string magia = DesbloquearMagiaNivel2(frag.Elemento);
            InventarioFragmento.Instance?.Remover(frag.FragmentoID);

            if (magia != null)
            {
                sb.AppendLine($"\n<b>Essencia absorvida!</b> Voce dominou <b>{magia}</b>.");
                CombatManager.Instance?.LogExternal(
                    $"[JOIN] {frag.Nome} absorvido. Magia desbloqueada: {magia}.");
            }
            else
            {
                sb.AppendLine($"\n<b>Essencia absorvida</b>, mas voce ja dominava a magia de {frag.Elemento} nivel 2.");
                CombatManager.Instance?.LogExternal($"[JOIN] {frag.Nome} absorvido.");
            }

            _selecionado = null;
            _input.text = "";
            _dicaTxt.text = "";
            Refresh();

            _resultadoTxt.color = Sucesso;
            _resultadoTxt.text = sb.ToString();
        }

        private void Falha(string msg)
        {
            _resultadoTxt.color = Erro;
            _resultadoTxt.text = msg;
        }

        /// <summary>
        /// Confere se a consulta é de fato um JOIN entre as duas tabelas, filtrando
        /// o fragmento escolhido. Aceita variações de alias, ordem e espaçamento —
        /// o que importa é o jogador ter entendido a ligação.
        /// </summary>
        private static (bool ok, string erro) ValidarJoin(string consulta, FragmentoData frag)
        {
            if (string.IsNullOrWhiteSpace(consulta))
                return (false, "Escreva a consulta no campo acima. Sem ideia? Use o botao DICA.");

            string s = Regex.Replace(consulta, @"\s+", " ").Trim().ToUpperInvariant();

            // Só leitura: nada de encadear comandos ou alterar o banco
            if (s.Contains(";"))
                return (false, "Uma consulta so, sem ponto e virgula.");
            if (Regex.IsMatch(s, @"\b(DROP|DELETE|UPDATE|INSERT|ALTER|CREATE|PRAGMA)\b"))
                return (false, "Aqui so entra consulta de leitura (SELECT).");

            if (!s.StartsWith("SELECT"))
                return (false, "A consulta precisa comecar com SELECT.");
            if (!Regex.IsMatch(s, @"\bFROM\s+FRAGMENTOS\b"))
                return (false, "Comece pela tabela do item: FROM Fragmentos");
            if (!Regex.IsMatch(s, @"\bJOIN\s+INIMIGOS\b"))
                return (false, "Falta trazer a outra tabela: JOIN Inimigos");
            if (!Regex.IsMatch(s, @"\bON\b"))
                return (false, "O JOIN precisa do ON dizendo como as tabelas se ligam.");
            if (!Regex.IsMatch(s, @"ON\s+[\w.]*INIMIGOID\s*=\s*[\w.]*\bID\b") &&
                !Regex.IsMatch(s, @"ON\s+[\w.]*\bID\b\s*=\s*[\w.]*INIMIGOID"))
                return (false, "A ligacao certa e InimigoID (de Fragmentos) = Id (de Inimigos).");
            if (!Regex.IsMatch(s, $@"\bFRAGMENTOID\s*=\s*{frag.FragmentoID}\b"))
                return (false, $"Filtre o fragmento escolhido: WHERE f.FragmentoID = {frag.FragmentoID}");

            return (true, null);
        }

        /// <summary>Desbloqueia a magia nível 2 do elemento. Retorna o nome, ou null se já estava.</summary>
        private static string DesbloquearMagiaNivel2(string elemento)
        {
            var db = DatabaseManager.Instance?.DB;
            if (db == null || string.IsNullOrEmpty(elemento)) return null;

            var magias = db.Query<SpellData>(
                "SELECT * FROM Magias WHERE Elemento = ? AND Nivel = 2", elemento);
            if (magias == null || magias.Count == 0) return null;

            var magia = magias[0];
            if (magia.Desbloqueado == 1) return null;

            db.Execute("UPDATE Magias SET Desbloqueado = 1 WHERE Id = ?", magia.Id);
            return magia.Nome;
        }

        /// <summary>Linha do resultado (as colunas variam conforme o SELECT do jogador).</summary>
        private class FragmentoJoinRow
        {
            public string Nome { get; set; }
            public string Tipo { get; set; }
            public string Elemento { get; set; }
            public int Raridade { get; set; }
            public int ValorXP { get; set; }
            public string Pista { get; set; }
            public string Inimigo { get; set; }

            public string Describe()
            {
                var sb = new StringBuilder();
                if (!string.IsNullOrEmpty(Nome))     sb.AppendLine($"Nome: {Nome}");
                if (!string.IsNullOrEmpty(Tipo))     sb.AppendLine($"Tipo: {Tipo}");
                if (!string.IsNullOrEmpty(Elemento)) sb.AppendLine($"Elemento: {Elemento}");
                if (Raridade > 0)                    sb.AppendLine($"Raridade: {Raridade}");
                if (ValorXP > 0)                     sb.AppendLine($"ValorXP: {ValorXP}");
                if (!string.IsNullOrEmpty(Pista))    sb.AppendLine($"Pista: {Pista}");
                if (!string.IsNullOrEmpty(Inimigo))  sb.AppendLine($"Inimigo: {Inimigo}");
                return sb.ToString();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS DE UI
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Posiciona por fração do painel (y medido a partir do topo).</summary>
        private static void Fracao(Transform t, float x0, float yTop0, float x1, float yTop1)
        {
            var rt = (RectTransform)t;
            rt.anchorMin = new Vector2(x0, 1f - yTop1);
            rt.anchorMax = new Vector2(x1, 1f - yTop0);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Esticar(Transform t, float l, float b, float r, float top)
        {
            var rt = (RectTransform)t;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -top);
        }

        private static RectTransform Caixa(Transform pai, string nome,
                                           float x0, float y0, float x1, float y1)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(pai, false);
            Fracao(go.transform, x0, y0, x1, y1);
            return (RectTransform)go.transform;
        }

        private static TextMeshProUGUI Texto(Transform pai, string nome, string texto, float tamanho,
                                             Color cor, float x0, float y0, float x1, float y1)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(pai, false);
            Fracao(go.transform, x0, y0, x1, y1);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = texto;
            tmp.fontSize = tamanho;
            tmp.color = cor;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static TMP_InputField CriarInput(Transform pai, float x0, float y0, float x1, float y1)
        {
            var go = new GameObject("InputJoin", typeof(RectTransform));
            go.transform.SetParent(pai, false);
            Fracao(go.transform, x0, y0, x1, y1);

            var bg = go.AddComponent<Image>();
            bg.color = Papel;

            var input = go.AddComponent<TMP_InputField>();
            input.lineType = TMP_InputField.LineType.MultiLineNewline;
            input.targetGraphic = bg;

            // O TMP_InputField precisa de viewport + texto + placeholder montados à mão
            var area = new GameObject("Text Area", typeof(RectTransform));
            area.transform.SetParent(go.transform, false);
            Esticar(area.transform, 12f, 8f, 12f, 8f);
            area.AddComponent<RectMask2D>();

            var textoGO = new GameObject("Text", typeof(RectTransform));
            textoGO.transform.SetParent(area.transform, false);
            Esticar(textoGO.transform, 0f, 0f, 0f, 0f);
            var texto = textoGO.AddComponent<TextMeshProUGUI>();
            texto.fontSize = 16f;
            texto.color = Ink;
            texto.alignment = TextAlignmentOptions.TopLeft;
            texto.textWrappingMode = TextWrappingModes.Normal;
            texto.richText = false;

            var phGO = new GameObject("Placeholder", typeof(RectTransform));
            phGO.transform.SetParent(area.transform, false);
            Esticar(phGO.transform, 0f, 0f, 0f, 0f);
            var ph = phGO.AddComponent<TextMeshProUGUI>();
            ph.text = "SELECT ... FROM Fragmentos f JOIN Inimigos i ON ... WHERE ...";
            ph.fontSize = 15f;
            ph.color = new Color(0.55f, 0.45f, 0.30f);
            ph.fontStyle = FontStyles.Italic;
            ph.alignment = TextAlignmentOptions.TopLeft;

            input.textViewport = (RectTransform)area.transform;
            input.textComponent = texto;
            input.placeholder = ph;
            input.text = "";
            return input;
        }

        private static Button Botao(Transform pai, string rotulo,
                                    float x0, float y0, float x1, float y1,
                                    UnityEngine.Events.UnityAction aoClicar, float tamanhoFonte = 15f)
        {
            var go = new GameObject($"Btn{rotulo}", typeof(RectTransform));
            go.transform.SetParent(pai, false);
            Fracao(go.transform, x0, y0, x1, y1);

            var img = go.AddComponent<Image>();
            var placa = Resources.Load<Sprite>("Sprites/UI/hud_botao");
            if (placa != null) UiFrame.Apply(img, placa, 0.5f);
            else img.color = new Color(0.35f, 0.22f, 0.10f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(aoClicar);

            var cores = btn.colors;
            cores.normalColor = cores.selectedColor = cores.highlightedColor = Color.white;
            cores.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            cores.disabledColor = new Color(0.62f, 0.58f, 0.52f);
            btn.colors = cores;

            var label = Texto(go.transform, "Label", rotulo, tamanhoFonte, Color.white, 0f, 0f, 1f, 1f);
            label.rectTransform.offsetMin = new Vector2(8f, 3f);
            label.rectTransform.offsetMax = new Vector2(-8f, -3f);
            return btn;
        }
    }
}
