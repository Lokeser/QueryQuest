// Assets/_QueryQuest/UI/MagiasUI.cs
// Aba MAGIAS do grimório, refeita por código.
//
// Cada magia vira um cartão com a faixa do elemento, o nível, alcance, dano e
// estado de desbloqueio — e, embaixo, a consulta pronta que lança justamente
// aquela magia. É o elo que faltava: o aluno vê a linha da tabela e o SELECT
// que a devolve, lado a lado.

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QueryQuest.Database;
using QueryQuest.Models;

namespace QueryQuest.UI
{
    public class MagiasUI : MonoBehaviour
    {
        private RectTransform _conteudo;
        private TextMeshProUGUI _resumo;
        private bool _montado;

        public static MagiasUI Instalar(Transform painel)
        {
            if (painel == null) return null;

            var pronto = painel.GetComponent<MagiasUI>();
            if (pronto != null) return pronto;

            for (int i = painel.childCount - 1; i >= 0; i--)
                Object.Destroy(painel.GetChild(i).gameObject);

            var antigo = painel.GetComponent<MagiaListUI>();
            if (antigo != null) antigo.enabled = false;

            var ui = painel.gameObject.AddComponent<MagiasUI>();
            ui.Build();
            return ui;
        }

        private void Build()
        {
            if (_montado) return;
            _montado = true;

            // Resumo fixo no topo: quantas magias o jogador domina
            var topoGO = new GameObject("Resumo", typeof(RectTransform));
            topoGO.transform.SetParent(transform, false);
            var topoRT = (RectTransform)topoGO.transform;
            topoRT.anchorMin = new Vector2(0f, 1f);
            topoRT.anchorMax = new Vector2(1f, 1f);
            topoRT.pivot = new Vector2(0.5f, 1f);
            topoRT.offsetMin = new Vector2(18f, 0f);
            topoRT.offsetMax = new Vector2(-18f, -14f);
            topoRT.sizeDelta = new Vector2(topoRT.sizeDelta.x, 30f);

            _resumo = topoGO.AddComponent<TextMeshProUGUI>();
            _resumo.fontSize = 15f;
            _resumo.color = GrimoireSkin.Tinta;
            _resumo.alignment = TextAlignmentOptions.Left;
            _resumo.textWrappingMode = TextWrappingModes.NoWrap;
            _resumo.raycastTarget = false;

            var scrollGO = new GameObject("MagiasScroll", typeof(RectTransform));
            scrollGO.transform.SetParent(transform, false);
            var sRT = (RectTransform)scrollGO.transform;
            sRT.anchorMin = Vector2.zero;
            sRT.anchorMax = Vector2.one;
            sRT.offsetMin = new Vector2(18f, 18f);
            sRT.offsetMax = new Vector2(-18f, -48f);

            var scroll = scrollGO.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 34f;

            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollGO.transform, false);
            var vRT = (RectTransform)viewport.transform;
            vRT.anchorMin = Vector2.zero; vRT.anchorMax = Vector2.one;
            vRT.offsetMin = Vector2.zero; vRT.offsetMax = Vector2.zero;
            viewport.AddComponent<RectMask2D>();
            // Imagem invisível que capta a roda do mouse: os cartões são todos
            // raycastTarget=false, e sem um Graphic sob o cursor o ScrollRect
            // nunca recebe o evento de rolagem.
            var captura = viewport.AddComponent<Image>();
            captura.color = new Color(0f, 0f, 0f, 0f);
            captura.raycastTarget = true;
            scroll.viewport = vRT;

            var conteudo = new GameObject("Conteudo", typeof(RectTransform));
            conteudo.transform.SetParent(viewport.transform, false);
            _conteudo = (RectTransform)conteudo.transform;
            _conteudo.anchorMin = new Vector2(0f, 1f);
            _conteudo.anchorMax = new Vector2(1f, 1f);
            _conteudo.pivot = new Vector2(0.5f, 1f);
            _conteudo.offsetMin = Vector2.zero;
            _conteudo.offsetMax = Vector2.zero;
            scroll.content = _conteudo;

            var layout = conteudo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(2, 12, 2, 12);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            conteudo.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            Refresh();
        }

        private void OnEnable()
        {
            if (_montado) Refresh();
        }

        // ─────────────────────────────────────────────────────────────────────
        // CONTEÚDO
        // ─────────────────────────────────────────────────────────────────────

        public void Refresh()
        {
            if (_conteudo == null) return;
            for (int i = _conteudo.childCount - 1; i >= 0; i--)
                Destroy(_conteudo.GetChild(i).gameObject);

            var db = DatabaseManager.Instance?.DB;
            if (db == null)
            {
                if (_resumo != null) _resumo.text = "<i>banco indisponível</i>";
                return;
            }

            List<SpellData> magias;
            try { magias = db.Query<SpellData>("SELECT * FROM Magias"); }
            catch { return; }
            if (magias == null) return;

            // Desbloqueadas primeiro; depois por elemento e nível
            magias.Sort((a, b) =>
            {
                if (a.Desbloqueado != b.Desbloqueado) return b.Desbloqueado.CompareTo(a.Desbloqueado);
                int e = string.Compare(a.Elemento, b.Elemento, System.StringComparison.Ordinal);
                return e != 0 ? e : a.Nivel.CompareTo(b.Nivel);
            });

            int liberadas = 0;
            foreach (var m in magias) if (m.Desbloqueado == 1) liberadas++;

            if (_resumo != null)
                _resumo.text = $"<b>{liberadas}</b> de <b>{magias.Count}</b> magias desbloqueadas   " +
                               $"<size=88%><color=#7A6A4A>· as bloqueadas aparecem na consulta, mas não são lançadas</color></size>";

            string elementoAtual = null;
            foreach (var m in magias)
            {
                if (m.Elemento != elementoAtual)
                {
                    elementoAtual = m.Elemento;
                    Separador(elementoAtual);
                }
                Cartao(m);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_conteudo);
        }

        private void Separador(string elemento)
        {
            var go = new GameObject($"Sep{elemento}", typeof(RectTransform));
            go.transform.SetParent(_conteudo, false);
            go.AddComponent<LayoutElement>().preferredHeight = 24f;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{(elemento ?? "?").ToUpperInvariant()}</b>";
            tmp.fontSize = 14f;
            tmp.color = Escurecer(ElementPalette.For(elemento), 0.55f);
            tmp.alignment = TextAlignmentOptions.BottomLeft;
            tmp.raycastTarget = false;
        }

        private void Cartao(SpellData m)
        {
            bool liberada = m.Desbloqueado == 1;
            Color cor = ElementPalette.For(m.Elemento);

            var go = new GameObject($"Magia_{m.Nome}", typeof(RectTransform));
            go.transform.SetParent(_conteudo, false);

            var img = go.AddComponent<Image>();
            GrimoireSkin.Vestir(img,
                liberada ? GrimoireSkin.Pergaminho : new Color(0.82f, 0.77f, 0.66f),
                liberada ? Escurecer(cor, 0.75f) : new Color(0.62f, 0.57f, 0.48f), 10, 2);
            img.raycastTarget = false;

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, 14, 10, 10);
            layout.spacing = 12f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            // Faixa do elemento, à esquerda
            var faixaGO = new GameObject("Faixa", typeof(RectTransform));
            faixaGO.transform.SetParent(go.transform, false);
            var faixaImg = faixaGO.AddComponent<Image>();
            GrimoireSkin.Vestir(faixaImg, liberada ? cor : Dessaturar(cor), Escurecer(cor, 0.6f), 6, 1);
            faixaImg.raycastTarget = false;
            var le = faixaGO.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = 9f;
            le.flexibleHeight = 1f;

            // Texto
            var txtGO = new GameObject("Texto", typeof(RectTransform));
            txtGO.transform.SetParent(go.transform, false);
            txtGO.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text = Descrever(m, liberada, cor);
            tmp.fontSize = 14f;
            tmp.color = liberada ? GrimoireSkin.Tinta : new Color(0.42f, 0.38f, 0.32f);
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            tmp.lineSpacing = 2f;
        }

        private static string Descrever(SpellData m, bool liberada, Color cor)
        {
            string hex = ColorUtility.ToHtmlStringRGB(Escurecer(cor, 0.62f));
            var sb = new StringBuilder();

            sb.Append($"<size=118%><b><color=#{hex}>{m.Nome}</color></b></size>");
            sb.Append(liberada
                ? "   <size=86%><color=#2E6B2E><b>DISPONÍVEL</b></color></size>"
                : "   <size=86%><color=#8A3020><b>BLOQUEADA</b></color></size>");
            sb.AppendLine();

            sb.AppendLine($"<size=92%>Nv. <b>{m.Nivel}</b>  ·  alcance <b>{m.Distancia}</b>  ·  " +
                          $"dano base <b>{m.DanoBase}</b>  ·  elemento <b>{m.Elemento}</b></size>");

            if (!string.IsNullOrEmpty(m.Descricao))
                sb.Append($"<size=88%><i>{m.Descricao}</i></size>");

            if (!liberada)
                sb.Append("\n<size=86%><i>Absorva um fragmento de " + m.Elemento + " para desbloquear.</i></size>");

            return sb.ToString();
        }

        private static Color Escurecer(Color c, float f) => new Color(c.r * f, c.g * f, c.b * f, c.a);

        private static Color Dessaturar(Color c)
        {
            float l = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
            return Color.Lerp(c, new Color(l, l, l), 0.65f);
        }
    }
}
