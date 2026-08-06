// Assets/_QueryQuest/UI/GrimoireSkin.cs
// Veste o grimório com a paleta da HUD SEM usar as artes da HUD.
//
// As molduras da HUD são banners largos, com ornamento desenhado nas pontas —
// esticadas num painel alto e estreito elas deformam. Aqui as superfícies são
// geradas em runtime: retângulos de cantos arredondados com borda dourada,
// em 9-slice, que escalam para qualquer tamanho sem perder o traço.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QueryQuest.UI
{
    public static class GrimoireSkin
    {
        // ─────────────────────────────────────────────────────────────────────
        // PALETA (tirada das artes da HUD)
        // ─────────────────────────────────────────────────────────────────────

        public static readonly Color Madeira      = new Color(0.176f, 0.098f, 0.043f);
        public static readonly Color MadeiraClara = new Color(0.271f, 0.157f, 0.071f);
        public static readonly Color Ouro         = new Color(0.855f, 0.686f, 0.220f);
        public static readonly Color OuroEscuro   = new Color(0.549f, 0.400f, 0.106f);
        public static readonly Color Pergaminho   = new Color(0.965f, 0.902f, 0.749f);
        public static readonly Color PergaminhoEscuro = new Color(0.878f, 0.796f, 0.616f);
        public static readonly Color Tinta        = new Color(0.239f, 0.149f, 0.059f);
        public static readonly Color TintaFraca   = new Color(0.435f, 0.337f, 0.208f);

        // ─────────────────────────────────────────────────────────────────────
        // SUPERFÍCIES GERADAS EM RUNTIME
        // ─────────────────────────────────────────────────────────────────────

        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Retângulo de cantos arredondados com borda, já fatiado em 9 partes.
        /// O miolo é 1px, então esticar não deforma nada além do preenchimento.
        /// </summary>
        public static Sprite Painel(Color fundo, Color borda, int raio = 12, int espessura = 3)
        {
            string chave = $"{fundo}|{borda}|{raio}|{espessura}";
            if (_cache.TryGetValue(chave, out var pronto) && pronto != null) return pronto;

            int n = raio * 2 + 4;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    // Distância com sinal até a borda do retângulo arredondado
                    float cx = Mathf.Clamp(x + 0.5f, raio, n - raio);
                    float cy = Mathf.Clamp(y + 0.5f, raio, n - raio);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) - raio;

                    Color c;
                    if (d >= 0f)                    c = new Color(borda.r, borda.g, borda.b, 0f);
                    else if (d >= -espessura)       c = borda;
                    else                            c = fundo;

                    // suaviza 1px na silhueta e na transição borda→fundo
                    float alpha = Mathf.Clamp01(0.5f - d);
                    if (d < -espessura && d > -espessura - 1f)
                        c = Color.Lerp(borda, fundo, -d - espessura);

                    c.a *= alpha;
                    px[y * n + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();

            float b = raio + 1;
            var sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0,
                                       SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            sprite.name = "grimorio_painel";
            _cache[chave] = sprite;
            return sprite;
        }

        /// <summary>Aplica uma superfície gerada a um Image, em 9-slice.</summary>
        public static void Vestir(Image img, Color fundo, Color borda, int raio = 12, int espessura = 3)
        {
            if (img == null) return;
            img.sprite = Painel(fundo, borda, raio, espessura);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
            img.color = Color.white;
        }

        // ─────────────────────────────────────────────────────────────────────
        // APLICAÇÃO NO PAINEL DO GRIMÓRIO
        // ─────────────────────────────────────────────────────────────────────

        public static void Aplicar(Transform painel)
        {
            if (painel == null) return;

            // Fundo geral: madeira escura com filete dourado
            var fundo = painel.GetComponent<Image>();
            if (fundo == null) fundo = painel.gameObject.AddComponent<Image>();
            Vestir(fundo, Madeira, Ouro, 16, 4);

            VestirCabecalho(painel.Find("Header"));
            VestirAbas(painel.Find("TabBar"));
            VestirConteudo(painel.Find("ContentArea"));
            VestirRodape(painel.Find("StatusBar"));
        }

        private static void VestirCabecalho(Transform header)
        {
            if (header == null) return;

            var img = header.GetComponent<Image>();
            if (img != null) Vestir(img, MadeiraClara, OuroEscuro, 10, 2);

            foreach (var tmp in header.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                tmp.color = Ouro;
                tmp.fontStyle |= FontStyles.Bold;
            }
        }

        private static void VestirRodape(Transform barra)
        {
            if (barra == null) return;

            var img = barra.GetComponent<Image>();
            if (img != null) Vestir(img, MadeiraClara, OuroEscuro, 8, 2);

            foreach (var tmp in barra.GetComponentsInChildren<TextMeshProUGUI>(true))
                tmp.color = new Color(0.78f, 0.66f, 0.42f);
        }

        private static void VestirConteudo(Transform area)
        {
            if (area == null) return;

            var img = area.GetComponent<Image>();
            if (img != null) Vestir(img, Pergaminho, OuroEscuro, 12, 3);

            for (int i = 0; i < area.childCount; i++)
            {
                var painel = area.GetChild(i);
                var pImg = painel.GetComponent<Image>();
                // Os painéis internos ficam transparentes: quem dá o pergaminho
                // é a ContentArea. Dois pergaminhos empilhados só engrossam a borda.
                if (pImg != null) pImg.color = new Color(1f, 1f, 1f, 0f);

                foreach (var tmp in painel.GetComponentsInChildren<TextMeshProUGUI>(true))
                    if (Luminancia(tmp.color) > 0.55f) tmp.color = Tinta;

                foreach (var btn in painel.GetComponentsInChildren<Button>(true))
                    VestirBotao(btn);

                foreach (var input in painel.GetComponentsInChildren<TMP_InputField>(true))
                {
                    var bg = input.GetComponent<Image>();
                    if (bg != null) Vestir(bg, PergaminhoEscuro, OuroEscuro, 8, 2);
                    if (input.textComponent != null) input.textComponent.color = Tinta;
                    if (input.placeholder is TextMeshProUGUI ph) ph.color = TintaFraca;
                }

                foreach (var scroll in painel.GetComponentsInChildren<Scrollbar>(true))
                {
                    var barra = scroll.GetComponent<Image>();
                    if (barra != null) Vestir(barra, PergaminhoEscuro, OuroEscuro, 6, 1);
                    if (scroll.targetGraphic is Image alca) Vestir(alca, OuroEscuro, Ouro, 6, 1);
                }
            }
        }

        public static void VestirBotao(Button btn)
        {
            if (btn == null) return;

            var img = btn.GetComponent<Image>();
            if (img != null) Vestir(img, MadeiraClara, Ouro, 8, 2);
            btn.targetGraphic = img;

            var cores = btn.colors;
            cores.normalColor      = Color.white;
            cores.highlightedColor = new Color(1f, 0.94f, 0.78f);
            cores.pressedColor     = new Color(0.75f, 0.70f, 0.60f);
            cores.selectedColor    = Color.white;
            cores.disabledColor    = new Color(0.55f, 0.52f, 0.48f);
            cores.fadeDuration     = 0.08f;
            btn.colors = cores;

            foreach (var tmp in btn.GetComponentsInChildren<TextMeshProUGUI>(true))
                tmp.color = Pergaminho;
        }

        // ─────────────────────────────────────────────────────────────────────
        // ABAS
        // ─────────────────────────────────────────────────────────────────────

        private static readonly List<(Button botao, GameObject painel)> _abas =
            new List<(Button, GameObject)>();

        private static void VestirAbas(Transform barra)
        {
            if (barra == null) return;

            var img = barra.GetComponent<Image>();
            if (img != null) img.color = new Color(1f, 1f, 1f, 0f);

            _abas.Clear();
            foreach (var btn in barra.GetComponentsInChildren<Button>(true))
            {
                var alvo = btn;
                btn.onClick.AddListener(RepintarAbas);

                var cores = btn.colors;
                cores.normalColor = cores.selectedColor = Color.white;
                cores.highlightedColor = new Color(1f, 0.96f, 0.85f);
                cores.pressedColor = new Color(0.82f, 0.78f, 0.70f);
                cores.disabledColor = Color.white;
                btn.colors = cores;

                foreach (var tmp in btn.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    tmp.fontStyle |= FontStyles.Bold;
                    tmp.textWrappingMode = TextWrappingModes.NoWrap;
                }
            }
        }

        /// <summary>Registra o par aba↔painel para saber qual pintar como ativa.</summary>
        public static void RegistrarAba(Button botao, GameObject painel)
        {
            if (botao == null || painel == null) return;
            _abas.Add((botao, painel));
        }

        /// <summary>A aba do painel visível fica em pergaminho; as demais, em madeira.</summary>
        public static void RepintarAbas()
        {
            foreach (var (botao, painel) in _abas)
            {
                if (botao == null || painel == null) continue;
                bool ativa = painel.activeSelf;

                var img = botao.GetComponent<Image>();
                if (img != null)
                {
                    if (ativa) Vestir(img, Pergaminho, Ouro, 10, 3);
                    else       Vestir(img, MadeiraClara, OuroEscuro, 10, 2);
                }

                foreach (var tmp in botao.GetComponentsInChildren<TextMeshProUGUI>(true))
                    tmp.color = ativa ? Tinta : new Color(0.72f, 0.62f, 0.42f);
            }
        }

        private static float Luminancia(Color c) => c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
    }
}
