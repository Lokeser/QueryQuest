// Assets/_QueryQuest/UI/FragmentosUI.cs
// Inventário de fragmentos + inspetor por JOIN.
//
// O jogador escolhe um fragmento e marca QUAIS COLUNAS quer revelar. A tela
// monta a query de verdade — com JOIN entre Fragmentos e Inimigos — mostra o
// SQL na tela (é o ponto pedagógico) e executa no SQLite.
//
// ABSORVER: ao inspecionar, o fragmento é consumido e o jogador domina a magia
// de NÍVEL 2 do elemento dele (Golem de Fogo -> magia de Fogo nível 2).
//
// Toda a UI é criada por código; abre pelo botão "FRAGMENTOS" da HUD.

using System.Collections.Generic;
using System.Text;
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

        private static readonly Color Ink   = new Color(0.24f, 0.15f, 0.06f);
        private static readonly Color Panel = new Color(0.10f, 0.06f, 0.03f, 0.92f);

        // Colunas que o jogador pode marcar (rótulo -> expressão SQL)
        private static readonly (string label, string sql)[] Colunas =
        {
            ("Nome",     "f.Nome"),
            ("Tipo",     "f.Tipo"),
            ("Elemento", "f.Elemento"),
            ("Raridade", "f.Raridade"),
            ("ValorXP",  "f.ValorXP"),
            ("Pista",    "f.Pista"),
        };

        private RectTransform _root;
        private RectTransform _lista;
        private TextMeshProUGUI _sqlText;
        private TextMeshProUGUI _resultText;
        private readonly List<Toggle> _toggles = new List<Toggle>();
        private FragmentoData _selecionado;

        public static FragmentosUI Instance { get; private set; }

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

            // Assina aqui (e não no Start): o painel é criado já desativado,
            // e Start não roda em GameObject inativo.
            FragmentDropSystem.OnAbsorbPhaseStarted += AbrirFase;
        }

        private void OnDestroy() => FragmentDropSystem.OnAbsorbPhaseStarted -= AbrirFase;

        private void Build()
        {
            _root = (RectTransform)transform;
            _root.anchorMin = new Vector2(0.5f, 0.5f);
            _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.pivot     = new Vector2(0.5f, 0.5f);
            _root.sizeDelta = new Vector2(940f, 600f);
            _root.anchoredPosition = Vector2.zero;

            var bg = gameObject.AddComponent<Image>();
            var frame = Resources.Load<Sprite>("Sprites/UI/hud_painel");
            if (frame != null) UiFrame.Apply(bg, frame);
            else bg.color = Panel;

            _titulo = MakeText(_root, "Titulo", "ESSENCIA DO GOLEM", 22f, Ink,
                     new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                     new Vector2(0f, -30f), new Vector2(-120f, 34f));

            // ── Coluna esquerda: inventário ──
            _lista = MakeBox(_root, "Lista", new Vector2(0f, 0f), new Vector2(0.42f, 1f),
                             new Vector2(46f, 60f), new Vector2(-10f, -78f));
            var layout = _lista.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            // ── Coluna direita: colunas + SQL + resultado ──
            var direita = MakeBox(_root, "Inspetor", new Vector2(0.42f, 0f), new Vector2(1f, 1f),
                                  new Vector2(10f, 60f), new Vector2(-52f, -78f));

            MakeText(direita, "LabelColunas", "O que voce quer revelar?", 15f, Ink,
                     new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                     new Vector2(0f, 0f), new Vector2(0f, 24f));

            var grid = MakeBox(direita, "Colunas", new Vector2(0f, 1f), new Vector2(1f, 1f),
                               new Vector2(0f, -136f), new Vector2(0f, -26f));
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(160f, 30f);
            gl.spacing = new Vector2(8f, 6f);

            foreach (var col in Colunas) _toggles.Add(MakeToggle(grid, col.label));
            _toggles[0].isOn = true;   // Nome vem marcado

            _sqlText = MakeText(direita, "SQL", "", 13f, new Color(0.30f, 0.22f, 0.10f),
                                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                                new Vector2(0f, -170f), new Vector2(0f, 92f));
            _sqlText.alignment = TextAlignmentOptions.TopLeft;
            _sqlText.fontStyle = FontStyles.Italic;

            _resultText = MakeText(direita, "Resultado", "Escolha um fragmento a esquerda.", 15f, Ink,
                                   new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                                   new Vector2(0f, -30f), new Vector2(0f, -300f));
            _resultText.alignment = TextAlignmentOptions.TopLeft;

            // ── Botões ──
            MakeButton(_root, "BtnInspecionar", $"ABSORVER COM JOIN ({CustoMana} mana)",
                       new Vector2(1f, 0f), new Vector2(1f, 0f),
                       new Vector2(-52f, 16f), new Vector2(300f, 46f), Inspecionar);

            _btnContinuar = MakeButton(_root, "BtnContinuar", "CONTINUAR",
                       new Vector2(0f, 0f), new Vector2(0f, 0f),
                       new Vector2(52f, 16f), new Vector2(220f, 46f), Continuar);
        }

        private Button _btnContinuar;
        private TextMeshProUGUI _titulo;

        private void AbrirFase(List<FragmentoData> caidos)
        {
            if (_titulo != null)
                _titulo.text = caidos != null && caidos.Count == 1
                    ? $"O GOLEM DEIXOU: {caidos[0].Nome.ToUpper()}"
                    : "O GOLEM DEIXOU SUAS ESSENCIAS";

            _resultText.text = "Escolha um fragmento e absorva com o JOIN.\n" +
                               "Cada essencia ensina a magia de nivel 2 do elemento dela.";
            Abrir();
        }

        /// <summary>Encerra a cena de absorção e devolve o controle ao andar.</summary>
        private void Continuar()
        {
            Fechar();
            FragmentDropSystem.Instance?.FinishPhase();
        }

        // ─────────────────────────────────────────────────────────────────────
        // ABRIR / FECHAR
        // ─────────────────────────────────────────────────────────────────────

        public void Abrir()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Refresh();
        }

        public void Fechar() => gameObject.SetActive(false);

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
                MakeText(_lista, "Vazio", "Nenhum fragmento ainda.\nDerrote golens para coletar.",
                         14f, Ink, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                         Vector2.zero, Vector2.zero);
                _selecionado = null;
                AtualizarSQL();
                return;
            }

            foreach (var frag in fragmentos)
            {
                var f = frag;   // captura por valor
                int qtd = InventarioFragmento.Instance.Quantidade(f.FragmentoID);
                string rotulo = $"{f.Nome}  [{f.Elemento}] R{f.Raridade}" + (qtd > 1 ? $" x{qtd}" : "");

                var btn = MakeButton(_lista, $"Frag{f.FragmentoID}", rotulo,
                                     Vector2.zero, Vector2.zero, Vector2.zero,
                                     new Vector2(0f, 34f), () => Selecionar(f));

                var le = btn.gameObject.AddComponent<LayoutElement>();
                le.minHeight = le.preferredHeight = 34f;
            }

            if (_selecionado != null && InventarioFragmento.Instance.Quantidade(_selecionado.FragmentoID) <= 0)
                _selecionado = null;

            AtualizarSQL();
        }

        private void Selecionar(FragmentoData frag)
        {
            _selecionado = frag;
            _resultText.text = $"{frag.Nome}\n<size=90%>{frag.Descricao}</size>";
            AtualizarSQL();
        }

        // ─────────────────────────────────────────────────────────────────────
        // A QUERY (o JOIN)
        // ─────────────────────────────────────────────────────────────────────

        private string MontarQuery(FragmentoData frag)
        {
            var colunas = new List<string>();
            for (int i = 0; i < Colunas.Length; i++)
                if (_toggles[i].isOn) colunas.Add(Colunas[i].sql);

            if (colunas.Count == 0) colunas.Add("f.Nome");
            colunas.Add("i.Nome AS Inimigo");   // o JOIN sempre revela de quem veio

            int id = frag?.FragmentoID ?? 0;
            return $"SELECT {string.Join(", ", colunas)}\n" +
                   $"FROM Fragmentos f\n" +
                   $"JOIN Inimigos i ON f.InimigoID = i.Id\n" +
                   $"WHERE f.FragmentoID = {id}";
        }

        private void AtualizarSQL()
        {
            if (_sqlText != null) _sqlText.text = _selecionado == null ? "" : MontarQuery(_selecionado);
        }

        private void Inspecionar()
        {
            // Trabalha numa cópia local: consumir o fragmento dispara
            // OnInventarioChanged -> Refresh(), que zera _selecionado no meio
            // deste método.
            var frag = _selecionado;
            if (frag == null)
            {
                _resultText.text = "Escolha um fragmento na lista.";
                return;
            }

            var mana = ManaSystem.Instance;
            if (mana != null && !mana.HasMana(CustoMana))
            {
                _resultText.text = $"Mana insuficiente. O JOIN custa {CustoMana}.";
                return;
            }

            var db = DatabaseManager.Instance?.DB;
            if (db == null) return;

            string query = MontarQuery(frag);
            var linhas = db.Query<FragmentoJoinRow>(query.Replace("\n", " "));

            mana?.SpendMana(CustoMana);

            var sb = new StringBuilder();
            sb.AppendLine("<b>Resultado do JOIN</b>");
            if (linhas != null && linhas.Count > 0)
                sb.AppendLine(linhas[0].Describe());

            // ── Absorção: domina a magia de nível 2 do elemento ──
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
            Refresh();
            _resultText.text = sb.ToString();   // depois do Refresh, que reescreve a tela
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

        /// <summary>Linha do resultado do JOIN (as colunas variam conforme os toggles).</summary>
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

        private static RectTransform MakeBox(Transform parent, string name, Vector2 aMin, Vector2 aMax,
                                             Vector2 offMin, Vector2 offMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            return rt;
        }

        private static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size,
                                                Color color, Vector2 aMin, Vector2 aMax, Vector2 pivot,
                                                Vector2 pos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.sizeDelta = sizeDelta; rt.anchoredPosition = pos;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button MakeButton(Transform parent, string name, string label,
                                         Vector2 anchor, Vector2 anchorMax, Vector2 pos,
                                         Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor; rt.anchorMax = anchorMax; rt.pivot = anchor;
            rt.sizeDelta = size; rt.anchoredPosition = pos;

            var img = go.AddComponent<Image>();
            var frame = Resources.Load<Sprite>("Sprites/UI/hud_botao");
            if (frame != null) UiFrame.Apply(img, frame, 0.5f);
            else img.color = new Color(0.35f, 0.22f, 0.10f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var tmp = MakeText(go.transform, "Label", label, 14f, Color.white,
                               Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                               Vector2.zero, Vector2.zero);
            tmp.rectTransform.offsetMin = new Vector2(8f, 2f);
            tmp.rectTransform.offsetMax = new Vector2(-8f, -2f);
            return btn;
        }

        private static Toggle MakeToggle(Transform parent, string label)
        {
            var go = new GameObject($"Toggle{label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var bg = new GameObject("Box", typeof(RectTransform));
            bg.transform.SetParent(go.transform, false);
            var bgRT = (RectTransform)bg.transform;
            bgRT.anchorMin = new Vector2(0f, 0.5f); bgRT.anchorMax = new Vector2(0f, 0.5f);
            bgRT.pivot = new Vector2(0f, 0.5f);
            bgRT.sizeDelta = new Vector2(22f, 22f);
            bgRT.anchoredPosition = new Vector2(2f, 0f);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.94f, 0.88f, 0.72f);

            var check = new GameObject("Check", typeof(RectTransform));
            check.transform.SetParent(bg.transform, false);
            var chRT = (RectTransform)check.transform;
            chRT.anchorMin = Vector2.zero; chRT.anchorMax = Vector2.one;
            chRT.offsetMin = new Vector2(4f, 4f); chRT.offsetMax = new Vector2(-4f, -4f);
            var chImg = check.AddComponent<Image>();
            chImg.color = new Color(0.35f, 0.20f, 0.06f);

            MakeText(go.transform, "Label", label, 14f, Ink,
                     new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                     new Vector2(14f, 0f), new Vector2(-32f, 0f)).alignment = TextAlignmentOptions.Left;

            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = bgImg;
            toggle.graphic = chImg;
            toggle.isOn = false;
            return toggle;
        }
    }
}
