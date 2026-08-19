// Assets/_QueryQuest/UI/TabelasUI.cs
// Aba TABELAS do grimório: a documentação do banco que o aluno consulta durante
// a partida. Substitui as antigas abas Arsenal e Docs.
//
// A primeira coisa visível é a caixa "O QUE VOCÊ PRECISA AGORA", com as três
// informações que decidem a luta. Só depois vêm as tabelas coluna a coluna,
// com as colunas decisivas em destaque.
//
// As colunas NÃO são digitadas aqui: vêm do esquema real do banco
// (ver SchemaGuia), então a documentação não tem como divergir do jogo.

using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QueryQuest.UI
{
    public class TabelasUI : MonoBehaviour
    {
        private const string HexDestaque = "#8A4B10";
        private const string HexTipo     = "#7A6A4A";
        private const string HexSQL      = "#6B3410";

        private RectTransform _conteudo;
        private bool _montado;

        // ─────────────────────────────────────────────────────────────────────
        // CONSTRUÇÃO
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Monta a aba dentro do painel dado, limpando o que houver nele.</summary>
        public static TabelasUI Instalar(Transform painel)
        {
            if (painel == null) return null;

            var existente = painel.GetComponent<TabelasUI>();
            if (existente != null) return existente;

            // O conteúdo antigo (tabela crua do Arsenal) sai de cena
            for (int i = painel.childCount - 1; i >= 0; i--)
                Object.Destroy(painel.GetChild(i).gameObject);

            var arsenal = painel.GetComponent<ArsenalUI>();
            if (arsenal != null) arsenal.enabled = false;

            var ui = painel.gameObject.AddComponent<TabelasUI>();
            ui.Build();
            return ui;
        }

        private void Build()
        {
            if (_montado) return;
            _montado = true;

            // ── Área rolável ──
            var scrollGO = new GameObject("TabelasScroll", typeof(RectTransform));
            scrollGO.transform.SetParent(transform, false);
            Esticar(scrollGO.transform, 18f, 18f, 18f, 18f);

            var scroll = scrollGO.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 34f;

            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollGO.transform, false);
            Esticar(viewport.transform, 0f, 0f, 0f, 0f);
            viewport.AddComponent<RectMask2D>();
            // O ScrollRect só recebe a roda do mouse se houver um Graphic com
            // raycastTarget sob o cursor. Como todos os cartões e textos são
            // raycastTarget=false, é esta imagem invisível que capta o evento —
            // sem ela a rolagem simplesmente não responde.
            var captura = viewport.AddComponent<Image>();
            captura.color = new Color(0f, 0f, 0f, 0f);
            captura.raycastTarget = true;
            scroll.viewport = (RectTransform)viewport.transform;

            var conteudo = new GameObject("Conteudo", typeof(RectTransform));
            conteudo.transform.SetParent(viewport.transform, false);
            _conteudo = (RectTransform)conteudo.transform;
            _conteudo.anchorMin = new Vector2(0f, 1f);
            _conteudo.anchorMax = new Vector2(1f, 1f);
            _conteudo.pivot = new Vector2(0.5f, 1f);
            _conteudo.offsetMin = new Vector2(0f, 0f);
            _conteudo.offsetMax = new Vector2(0f, 0f);
            scroll.content = _conteudo;

            var layout = conteudo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(4, 14, 2, 12);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            conteudo.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            Preencher();
        }

        private void OnEnable()
        {
            // O banco pode não estar pronto quando o grimório é montado
            if (_montado && _conteudo != null && _conteudo.childCount <= 1) Preencher();
        }

        // ─────────────────────────────────────────────────────────────────────
        // CONTEÚDO
        // ─────────────────────────────────────────────────────────────────────

        private void Preencher()
        {
            if (_conteudo == null) return;
            for (int i = _conteudo.childCount - 1; i >= 0; i--)
                Destroy(_conteudo.GetChild(i).gameObject);

            Destaque();

            Tabela("Magias", "O que VOCÊ pode lançar. Toda magia que você conjura sai desta tabela.",
                   "SELECT * FROM Magias WHERE Elemento = 'Fogo' AND Distancia = 'LONGO'");

            Tabela("Inimigos", "O que você está enfrentando. As fraquezas do golem estão aqui — " +
                   "e não aparecem na tela até você perguntar.",
                   "SELECT Nome, FraquezaElemento, FraquezaDistancia FROM Inimigos");

            Tabela("Fragmentos", "O que cai do golem derrotado. Liga-se a Inimigos pela coluna InimigoID.",
                   "SELECT f.Nome, i.Nome FROM Fragmentos f JOIN Inimigos i ON f.InimigoID = i.Id");

            Rodape();

            // Sem isto a altura do conteúdo só é recalculada no frame seguinte,
            // e a rolagem nasce travada.
            LayoutRebuilder.ForceRebuildLayoutImmediate(_conteudo);
        }

        /// <summary>
        /// A caixa que abre a aba: as colunas das duas tabelas que o jogador
        /// consulta o tempo todo, iguais às que ficam ao lado das barras de vida.
        /// Vêm do esquema real do banco, com as decisivas em destaque.
        /// </summary>
        private void Destaque()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<size=118%><b><color={HexDestaque}>O QUE VOCÊ PRECISA AGORA</color></b></size>");
            sb.AppendLine();
            // O dourado da HUD sumiria sobre o pergaminho: aqui o destaque é escuro.
            sb.AppendLine(SchemaGuia.LinhaDeColunas("Magias", HexDestaque));
            sb.AppendLine();
            sb.Append(SchemaGuia.LinhaDeColunas("Inimigos", HexDestaque));

            Cartao(sb.ToString(), GrimoireSkin.PergaminhoEscuro, GrimoireSkin.Ouro, 15.5f);
        }

        private void Tabela(string nome, string paraQueServe, string exemplo)
        {
            var colunas = SchemaGuia.Colunas(nome);

            var sb = new StringBuilder();
            sb.AppendLine($"<size=124%><b><color={HexDestaque}>{nome}</color></b></size>");
            sb.AppendLine($"<i>{paraQueServe}</i>");
            sb.AppendLine();

            if (colunas.Count == 0)
            {
                sb.AppendLine("<i>(banco indisponível — reabra o grimório)</i>");
            }
            else
            {
                foreach (var c in colunas)
                {
                    string rotulo = c.Destaque
                        ? $"<b><color={HexDestaque}>{c.Nome}</color></b>"
                        : $"<b>{c.Nome}</b>";
                    string tipo = string.IsNullOrEmpty(c.Tipo) ? "" : $" <color={HexTipo}><size=88%>{c.Tipo}</size></color>";
                    // ">" e ASCII: o losango U+25C6 nao existe na fonte e saia como
                    // quadrado vazio. O ponto U+2022 renderiza normalmente.
                    string marca = c.Destaque ? "> " : "•  ";
                    sb.AppendLine($"{marca}{rotulo}{tipo}");
                    if (!string.IsNullOrEmpty(c.Explicacao))
                        sb.AppendLine($"    <size=92%>{c.Explicacao}</size>");
                }
            }

            sb.AppendLine();
            sb.Append($"<b>Exemplo:</b>  <color={HexSQL}><b>{exemplo}</b></color>");

            Cartao(sb.ToString(), GrimoireSkin.Pergaminho, GrimoireSkin.OuroEscuro, 15f);
        }

        private void Rodape()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<b><color={HexDestaque}>LEMBRETES DE SINTAXE</color></b>");
            sb.AppendLine("• Texto vai entre aspas simples: <b>WHERE Elemento = 'Fogo'</b>");
            sb.AppendLine("• Número vai sem aspas: <b>WHERE DanoBase &gt; 40</b>");
            sb.AppendLine("• Quanto mais filtro, <b>mais barata</b> fica a magia: WHERE, AND, ORDER BY e LIMIT descontam mana.");
            sb.Append("• As colunas marcadas com <b>&gt;</b> mudam o resultado do combate.");

            Cartao(sb.ToString(), GrimoireSkin.PergaminhoEscuro, GrimoireSkin.OuroEscuro, 14.5f);
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private void Cartao(string texto, Color fundo, Color borda, float fonte)
        {
            var go = new GameObject("Cartao", typeof(RectTransform));
            go.transform.SetParent(_conteudo, false);

            var img = go.AddComponent<Image>();
            GrimoireSkin.Vestir(img, fundo, borda, 10, 2);
            img.raycastTarget = false;

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 12, 12);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            // Sem ContentSizeFitter aqui: o cartão está DENTRO de um
            // VerticalLayoutGroup que já controla a altura dele. Os dois juntos
            // brigam pelo mesmo valor, e o resultado é a altura do conteúdo sair
            // errada — foi o que travava a rolagem no meio da tabela Magias.

            var txtGO = new GameObject("Texto", typeof(RectTransform));
            txtGO.transform.SetParent(go.transform, false);

            var tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text = texto;
            tmp.fontSize = fonte;
            tmp.color = GrimoireSkin.Tinta;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.richText = true;
            tmp.raycastTarget = false;
            tmp.lineSpacing = 2f;
        }

        private static void Esticar(Transform t, float l, float b, float r, float top)
        {
            var rt = (RectTransform)t;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -top);
        }
    }
}
