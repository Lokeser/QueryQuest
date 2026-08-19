// Assets/_QueryQuest/UI/TutorialPopup.cs
// Explicação rápida do jogo, em páginas curtas. Abre sozinha no primeiro
// combate de um jogo novo e pode ser reaberta pelo botão AJUDA, no topo do
// grimório. Criada por código pelo HudSkin.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QueryQuest.Combat;

namespace QueryQuest.UI
{
    public class TutorialPopup : MonoBehaviour
    {
        public static TutorialPopup Instance { get; private set; }

        private static readonly Color Ink   = new Color(0.24f, 0.15f, 0.06f);
        private static readonly Color Realce = new Color(0.42f, 0.20f, 0.04f);

        /// <summary>
        /// Limites do pergaminho dentro da moldura hud_painel, com uma folga.
        /// Medidos na arte: o bege vai de 0,147 a 0,906 na horizontal.
        /// </summary>
        private const float SafeX0 = 0.168f;
        private const float SafeX1 = 0.886f;

        private struct Pagina
        {
            public string Titulo;
            public string Corpo;
        }

        // Só o essencial para dar o primeiro turno. O resto o jogador descobre
        // jogando, ou consulta na aba TABELAS do grimório — repetir tudo aqui
        // vira parede de texto que ninguém lê.
        private static readonly Pagina[] Paginas =
        {
            new Pagina
            {
                Titulo = "VOCE NAO ESCOLHE MAGIAS. VOCE AS CONSULTA.",
                Corpo  = "Seu grimório é um banco de dados. Para atacar, escreva uma consulta SQL na aba <b>Query</b>: a magia que ela devolver é a que você lança.\n\n" +
                         "<color=#6B3410><b>SELECT * FROM Magias WHERE Elemento = 'Fogo'</b></color>\n\n" +
                         "Quanto mais específica a consulta, <b>mais barata</b> fica a magia. WHERE, AND, ORDER BY e LIMIT descontam mana.",
            },
            new Pagina
            {
                Titulo = "DESCUBRA A FRAQUEZA, ESCOLHA A DISTANCIA",
                Corpo  = "Cada golem tem um elemento que o fere e uma distância em que sofre dano extra. Nada disso aparece na tela: está na tabela <b>Inimigos</b>.\n\n" +
                         "<color=#6B3410><b>SELECT FraquezaElemento FROM Inimigos</b></color>\n\n" +
                         "A arena tem <b>6 slots</b>. Por turno você lança <b>uma</b> magia e faz <b>um</b> movimento; o turno só passa quando você clicar em <b>ENCERRAR TURNO</b>.",
            },
            new Pagina
            {
                Titulo = "PERDIDO? ABRA O GRIMORIO.",
                Corpo  = "A aba <b>TABELAS</b> explica cada coluna do banco e marca as que decidem a luta. A lua também solta dicas — clique nela.\n\n" +
                         "Ao derrotar um golem você absorve o fragmento dele escrevendo um <b>JOIN</b>, e ganha a magia de nível 2 daquele elemento.\n\n" +
                         "<b>Para reler isto:</b> botão <b>AJUDA</b>, no topo do grimório.",
            },
        };

        private int _pagina;
        private TextMeshProUGUI _titulo, _corpo, _contador;
        private Button _btnAnterior, _btnProximo;

        // ─────────────────────────────────────────────────────────────────────
        // CONSTRUÇÃO
        // ─────────────────────────────────────────────────────────────────────

        public static TutorialPopup Create(Transform canvas)
        {
            var go = new GameObject("TutorialPopup", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var ui = go.AddComponent<TutorialPopup>();
            ui.Build();
            go.SetActive(false);
            return ui;
        }

        private void Awake() => Instance = this;

        private void Build()
        {
            var raiz = (RectTransform)transform;
            raiz.anchorMin = raiz.anchorMax = new Vector2(0.5f, 0.5f);
            raiz.pivot = new Vector2(0.5f, 0.5f);
            raiz.sizeDelta = new Vector2(1080f, 660f);
            raiz.anchoredPosition = Vector2.zero;

            var fundo = gameObject.AddComponent<Image>();
            var moldura = Resources.Load<Sprite>("Sprites/UI/hud_painel");
            if (moldura != null) UiFrame.Apply(fundo, moldura);
            else fundo.color = new Color(0.96f, 0.90f, 0.74f);

            // Tudo aqui dentro respeita a ÁREA BEGE da moldura, e não o retângulo
            // do painel. A hud_painel é 9-slice com borda de 279x221px sobre uma
            // arte de 996x790: o pergaminho começa em x 0,147 e termina em 0,906,
            // e verticalmente vai de 0,115 a 0,812. Usar as bordas do painel como
            // referência jogava o título por baixo do entalhe dourado.
            // Os tamanhos de fonte são os de ANTES do TextStyler, que aplica +20%.
            _titulo = Texto(raiz, "Titulo", "", 20f, Realce, SafeX0, 0.140f, SafeX1, 0.245f);
            _titulo.enableAutoSizing = true;
            _titulo.fontSizeMin = 12f;
            _titulo.fontSizeMax = 20f;

            _corpo = Texto(raiz, "Corpo", "", 17f, Ink, SafeX0 + 0.02f, 0.280f, SafeX1 - 0.018f, 0.655f);
            _corpo.alignment = TextAlignmentOptions.TopLeft;
            _corpo.lineSpacing = 6f;
            _corpo.enableAutoSizing = true;
            _corpo.fontSizeMin = 11f;
            _corpo.fontSizeMax = 17f;

            _contador = Texto(raiz, "Contador", "", 13f, new Color(0.45f, 0.34f, 0.20f),
                              0.430f, 0.680f, 0.570f, 0.750f);

            _btnAnterior = Botao(raiz, "ANTERIOR", SafeX0 + 0.005f, 0.668f, 0.375f, 0.778f, () => Ir(-1));
            _btnProximo  = Botao(raiz, "PROXIMO",  0.680f, 0.668f, SafeX1 - 0.005f, 0.778f, () => Ir(+1));
        }

        // ─────────────────────────────────────────────────────────────────────
        // NAVEGAÇÃO
        // ─────────────────────────────────────────────────────────────────────

        public void Abrir()
        {
            _pagina = 0;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Mostrar();
        }

        public void Fechar() => gameObject.SetActive(false);

        private void Ir(int passo)
        {
            int destino = _pagina + passo;
            if (destino < 0) return;
            if (destino >= Paginas.Length) { Fechar(); return; }   // "COMECAR" na última

            _pagina = destino;
            Mostrar();
        }

        private void Mostrar()
        {
            _titulo.text = Paginas[_pagina].Titulo;
            _corpo.text  = Paginas[_pagina].Corpo;
            _contador.text = $"{_pagina + 1} / {Paginas.Length}";

            _btnAnterior.interactable = _pagina > 0;

            bool ultima = _pagina == Paginas.Length - 1;
            foreach (var t in _btnProximo.GetComponentsInChildren<TextMeshProUGUI>(true))
                t.text = ultima ? "COMECAR" : "PROXIMO";
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private static void Fracao(Transform t, float x0, float yTop0, float x1, float yTop1)
        {
            var rt = (RectTransform)t;
            rt.anchorMin = new Vector2(x0, 1f - yTop1);
            rt.anchorMax = new Vector2(x1, 1f - yTop0);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
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

        private static Button Botao(Transform pai, string rotulo, float x0, float y0, float x1, float y1,
                                    System.Action aoClicar)
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
            btn.onClick.AddListener(() => aoClicar());

            var cores = btn.colors;
            cores.normalColor = cores.selectedColor = cores.highlightedColor = Color.white;
            cores.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            cores.disabledColor = new Color(0.62f, 0.58f, 0.52f);
            btn.colors = cores;

            var label = Texto(go.transform, "Label", rotulo, 13f, Color.white, 0f, 0f, 1f, 1f);
            label.rectTransform.offsetMin = new Vector2(8f, 4f);
            label.rectTransform.offsetMax = new Vector2(-8f, -4f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = true;
            label.fontSizeMin = 8f;
            label.fontSizeMax = 13f;
            return btn;
        }
    }
}
