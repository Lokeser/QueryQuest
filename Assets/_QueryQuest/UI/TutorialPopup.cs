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

        private struct Pagina
        {
            public string Titulo;
            public string Corpo;
        }

        // A explicação. Curta por página, para o jogador ler sem cansar.
        private static readonly Pagina[] Paginas =
        {
            new Pagina
            {
                Titulo = "VOCE NAO ESCOLHE MAGIAS. VOCE AS CONSULTA.",
                Corpo  = "Seu grimório é um banco de dados. Para atacar, escreva uma consulta SQL de verdade na aba <b>Query</b> — a magia que a consulta devolver é a que você lança.\n\n" +
                         "<color=#6B3410><b>SELECT * FROM Magias WHERE Elemento = 'Fogo'</b></color>\n\n" +
                         "Se a consulta devolver várias magias, você lança a primeira da lista.",
            },
            new Pagina
            {
                Titulo = "CONSULTA PRECISA, MAGIA BARATA",
                Corpo  = "O custo de mana <b>cai</b> quanto mais específica for a sua consulta. Filtrar com <b>WHERE</b>, <b>AND</b>, <b>ORDER BY</b> e <b>LIMIT</b> deixa o feitiço mais barato.\n\n" +
                         "Um <color=#6B3410><b>SELECT *</b></color> sem filtro funciona, mas é o jeito mais caro de lutar: você paga por toda a lista para lançar uma magia só.",
            },
            new Pagina
            {
                Titulo = "A POSICAO DECIDE SE VOCE ACERTA",
                Corpo  = "A arena tem <b>6 slots</b>. Cada magia tem um alcance a partir de onde você está:\n\n" +
                         "<b>CURTO</b> — atinge o slot seguinte\n" +
                         "<b>MEDIO</b> — os dois seguintes\n" +
                         "<b>LONGO</b> — os três seguintes\n\n" +
                         "Se o golem não estiver dentro dessa área, a magia passa longe. E cada golem sofre <b>+50% de dano</b> na distância em que é vulnerável.",
            },
            new Pagina
            {
                Titulo = "UM TURNO = UMA MAGIA + UM MOVIMENTO",
                Corpo  = "Por turno você pode lançar <b>uma</b> magia e fazer <b>um</b> movimento, na ordem que preferir.\n\n" +
                         "O turno não passa sozinho: quando terminar, clique em <b>ENCERRAR TURNO</b>. Use isso a seu favor — ataque primeiro e depois recue, ou aproxime-se e só então lance.",
            },
            new Pagina
            {
                Titulo = "A LUA SABE DE ALGUMA COISA",
                Corpo  = "No começo de cada luta a lua solta uma dica sobre a fraqueza do golem. <b>Clique nela</b> para ouvir as outras.\n\n" +
                         "Para confirmar, use a magia <b>Analise</b> consultando a tabela de inimigos:\n\n" +
                         "<color=#6B3410><b>SELECT FraquezaElemento FROM Inimigos</b></color>\n\n" +
                         "Ela cobra mana <b>por coluna</b> consultada — pergunte só o que importa.",
            },
            new Pagina
            {
                Titulo = "DERROTOU O GOLEM? USE JOIN.",
                Corpo  = "Cada golem derrotado deixa cair <b>fragmentos</b> da própria essência. Para absorver, você cruza duas tabelas com <b>JOIN</b>:\n\n" +
                         "<color=#6B3410><b>FROM Fragmentos f JOIN Inimigos i ON f.InimigoID = i.Id</b></color>\n\n" +
                         "Ninguém escreve por você: na tela de absorção você <b>digita a consulta</b>, e ela roda de verdade no banco. Se travar, o botão <b>DICA</b> ajuda em três degraus.\n\n" +
                         "Absorver um fragmento ensina a você a <b>magia de nível 2</b> daquele elemento. É assim que sua build cresce ao longo dos 5 andares.",
            },
            new Pagina
            {
                Titulo = "PRONTO. O RESTO E COM VOCE.",
                Corpo  = "Cinco andares, um golem em cada. Vença todos e o <b>Modo Infinito</b> abre: tudo recomeça mais forte, com a build que você montou.\n\n" +
                         "<b>Quer reler isto?</b> É só clicar em <b>AJUDA</b>, no canto superior do grimório — está sempre lá.",
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
            raiz.sizeDelta = new Vector2(920f, 580f);
            raiz.anchoredPosition = Vector2.zero;

            var fundo = gameObject.AddComponent<Image>();
            var moldura = Resources.Load<Sprite>("Sprites/UI/hud_painel");
            if (moldura != null) UiFrame.Apply(fundo, moldura);
            else fundo.color = new Color(0.96f, 0.90f, 0.74f);

            _titulo = Texto(raiz, "Titulo", "", 26f, Realce, 0.08f, 0.09f, 0.92f, 0.22f);

            _corpo = Texto(raiz, "Corpo", "", 19f, Ink, 0.09f, 0.24f, 0.91f, 0.76f);
            _corpo.alignment = TextAlignmentOptions.TopLeft;
            _corpo.lineSpacing = 6f;

            _contador = Texto(raiz, "Contador", "", 16f, new Color(0.45f, 0.34f, 0.20f),
                              0.40f, 0.80f, 0.60f, 0.88f);

            _btnAnterior = Botao(raiz, "ANTERIOR", 0.08f, 0.80f, 0.30f, 0.91f, () => Ir(-1));
            _btnProximo  = Botao(raiz, "PROXIMO",  0.70f, 0.80f, 0.92f, 0.91f, () => Ir(+1));
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

            var label = Texto(go.transform, "Label", rotulo, 17f, Color.white, 0f, 0f, 1f, 1f);
            label.rectTransform.offsetMin = new Vector2(8f, 4f);
            label.rectTransform.offsetMax = new Vector2(-8f, -4f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return btn;
        }
    }
}
