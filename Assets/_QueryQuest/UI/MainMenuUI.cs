// Assets/_QueryQuest/UI/MainMenuUI.cs
// Tela inicial do jogo. A cena MainMenu tem só um GameObject com este script —
// Canvas, EventSystem, fundo, logo e botões são todos criados aqui, seguindo a
// convenção do projeto de montar a UI por código.
//
// A logo e os botões ficam sobre o painel de couro da arte (lado esquerdo,
// que ocupa os primeiros ~32% da largura).

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using QueryQuest.Combat;

namespace QueryQuest.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        private const string CenaCombate = "SampleScene";

        private static readonly Color Ink       = new Color(0.24f, 0.15f, 0.06f);
        private static readonly Color Pergaminho = new Color(0.96f, 0.90f, 0.74f);

        // Tudo em fração da tela: a arte de fundo é esticada e o couro fica à esquerda
        private static readonly float[] LogoRect = { 0.030f, 0.045f, 0.300f, 0.320f };
        private const float BotaoX0 = 0.055f, BotaoX1 = 0.275f;
        private const float BotaoY0 = 0.400f, BotaoAltura = 0.086f, BotaoEspaco = 0.026f;

        private RectTransform _raiz;
        private RectTransform _painelConfig;
        private Button _btnContinuar;

        // Config
        private readonly List<Vector2Int> _resolucoes = new List<Vector2Int>();
        private int _resIndex;
        private bool _telaCheia;
        private TextMeshProUGUI _labelRes, _labelModo;

        // ─────────────────────────────────────────────────────────────────────
        // CONSTRUÇÃO
        // ─────────────────────────────────────────────────────────────────────

        private void Start()
        {
            GarantirEventSystem();
            _raiz = CriarCanvas();

            CriarFundo();
            CriarLogo();
            CriarBotoes();
            CriarPainelConfig();

            CarregarPreferencias();
        }

        private static void GarantirEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        private RectTransform CriarCanvas()
        {
            var go = new GameObject("MenuCanvas", typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return (RectTransform)go.transform;
        }

        private void CriarFundo()
        {
            var go = new GameObject("Fundo", typeof(RectTransform));
            go.transform.SetParent(_raiz, false);
            Esticar(go.transform, 0f, 0f, 0f, 0f);

            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            var sprite = Resources.Load<Sprite>("Sprites/UI/menu_fundo");
            if (sprite != null) img.sprite = sprite;
            else img.color = new Color(0.10f, 0.07f, 0.05f);
        }

        private void CriarLogo()
        {
            var go = new GameObject("Logo", typeof(RectTransform));
            go.transform.SetParent(_raiz, false);
            Fracao(go.transform, LogoRect[0], LogoRect[1], LogoRect[2], LogoRect[3]);

            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            var sprite = Resources.Load<Sprite>("Sprites/UI/logo");
            if (sprite != null) img.sprite = sprite;
            else img.color = new Color(0f, 0f, 0f, 0f);
        }

        private void CriarBotoes()
        {
            (string texto, System.Action acao)[] itens =
            {
                ("NOVO JOGO", NovoJogo),
                ("CONTINUAR", Continuar),
                ("CONFIG",    () => MostrarConfig(true)),
                ("SAIR",      Sair),
            };

            for (int i = 0; i < itens.Length; i++)
            {
                float y0 = BotaoY0 + i * (BotaoAltura + BotaoEspaco);
                var btn = CriarBotao(_raiz, itens[i].texto, BotaoX0, y0, BotaoX1, y0 + BotaoAltura, itens[i].acao);
                if (itens[i].texto == "CONTINUAR") _btnContinuar = btn;
            }

            // Sem andar salvo não há o que continuar
            if (_btnContinuar != null && !GameSession.TemProgresso)
            {
                _btnContinuar.interactable = false;
                foreach (var t in _btnContinuar.GetComponentsInChildren<TextMeshProUGUI>(true))
                    t.color = new Color(0.72f, 0.66f, 0.58f);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // AÇÕES
        // ─────────────────────────────────────────────────────────────────────

        private void NovoJogo()
        {
            GameSession.MostrarTutorial = true;
            GameSession.AndarInicial = 1;
            GameSession.LimparProgresso();
            SceneManager.LoadScene(CenaCombate);
        }

        private void Continuar()
        {
            GameSession.MostrarTutorial = false;
            GameSession.AndarInicial = GameSession.AndarSalvo;
            SceneManager.LoadScene(CenaCombate);
        }

        private void Sair()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ─────────────────────────────────────────────────────────────────────
        // CONFIG (resolução e tela cheia)
        // ─────────────────────────────────────────────────────────────────────

        private void CriarPainelConfig()
        {
            var go = new GameObject("PainelConfig", typeof(RectTransform));
            go.transform.SetParent(_raiz, false);
            _painelConfig = (RectTransform)go.transform;
            Fracao(_painelConfig, 0.34f, 0.24f, 0.94f, 0.76f);

            var img = go.AddComponent<Image>();
            var moldura = Resources.Load<Sprite>("Sprites/UI/hud_painel");
            if (moldura != null) UiFrame.Apply(img, moldura);
            else img.color = new Color(0.12f, 0.08f, 0.04f, 0.96f);

            Texto(_painelConfig, "Titulo", "CONFIGURACOES", 30f, Ink,
                  0.05f, 0.06f, 0.95f, 0.18f);

            MontarResolucoes();

            // Resolução
            Texto(_painelConfig, "LabelRes", "Resolução", 20f, Ink, 0.10f, 0.26f, 0.45f, 0.38f)
                .alignment = TextAlignmentOptions.Left;
            CriarBotao(_painelConfig, "<", 0.46f, 0.26f, 0.55f, 0.38f, () => TrocarResolucao(-1));
            _labelRes = Texto(_painelConfig, "ValorRes", "", 20f, Ink, 0.56f, 0.26f, 0.80f, 0.38f);
            CriarBotao(_painelConfig, ">", 0.81f, 0.26f, 0.90f, 0.38f, () => TrocarResolucao(+1));

            // Tela cheia / janela
            Texto(_painelConfig, "LabelModo", "Modo", 20f, Ink, 0.10f, 0.44f, 0.45f, 0.56f)
                .alignment = TextAlignmentOptions.Left;
            _labelModo = Texto(_painelConfig, "ValorModo", "", 20f, Ink, 0.56f, 0.44f, 0.80f, 0.56f);
            CriarBotao(_painelConfig, "TROCAR", 0.46f, 0.60f, 0.90f, 0.72f, AlternarModo);

            // Aplicar / Voltar
            CriarBotao(_painelConfig, "APLICAR", 0.10f, 0.78f, 0.47f, 0.91f, Aplicar);
            CriarBotao(_painelConfig, "VOLTAR",  0.53f, 0.78f, 0.90f, 0.91f, () => MostrarConfig(false));

            go.SetActive(false);
        }

        private void MontarResolucoes()
        {
            var vistas = new HashSet<long>();
            foreach (var r in Screen.resolutions)
            {
                long chave = ((long)r.width << 32) | (uint)r.height;
                if (vistas.Add(chave)) _resolucoes.Add(new Vector2Int(r.width, r.height));
            }

            // Fallback: em alguns ambientes Screen.resolutions vem vazio
            if (_resolucoes.Count == 0)
                _resolucoes.AddRange(new[]
                {
                    new Vector2Int(1280, 720), new Vector2Int(1600, 900),
                    new Vector2Int(1920, 1080), new Vector2Int(2560, 1440),
                });

            _resolucoes.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        }

        private void CarregarPreferencias()
        {
            int larg = PlayerPrefs.GetInt("QQ_ResW", Screen.width);
            int alt  = PlayerPrefs.GetInt("QQ_ResH", Screen.height);
            _telaCheia = PlayerPrefs.GetInt("QQ_TelaCheia", Screen.fullScreen ? 1 : 0) == 1;

            _resIndex = _resolucoes.FindIndex(r => r.x == larg && r.y == alt);
            if (_resIndex < 0) _resIndex = Mathf.Max(0, _resolucoes.Count - 1);

            AtualizarLabelsConfig();
        }

        private void TrocarResolucao(int passo)
        {
            if (_resolucoes.Count == 0) return;
            _resIndex = (_resIndex + passo + _resolucoes.Count) % _resolucoes.Count;
            AtualizarLabelsConfig();
        }

        private void AlternarModo()
        {
            _telaCheia = !_telaCheia;
            AtualizarLabelsConfig();
        }

        private void AtualizarLabelsConfig()
        {
            if (_labelRes != null && _resolucoes.Count > 0)
                _labelRes.text = $"{_resolucoes[_resIndex].x} x {_resolucoes[_resIndex].y}";
            if (_labelModo != null)
                _labelModo.text = _telaCheia ? "Tela cheia" : "Janela";
        }

        private void Aplicar()
        {
            if (_resolucoes.Count == 0) return;

            var r = _resolucoes[_resIndex];
            Screen.SetResolution(r.x, r.y, _telaCheia ? FullScreenMode.FullScreenWindow
                                                      : FullScreenMode.Windowed);

            PlayerPrefs.SetInt("QQ_ResW", r.x);
            PlayerPrefs.SetInt("QQ_ResH", r.y);
            PlayerPrefs.SetInt("QQ_TelaCheia", _telaCheia ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void MostrarConfig(bool mostrar)
        {
            if (_painelConfig != null) _painelConfig.gameObject.SetActive(mostrar);
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS DE UI
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Posiciona por fração da tela (y medido a partir do TOPO).</summary>
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
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button CriarBotao(Transform pai, string rotulo,
                                         float x0, float y0, float x1, float y1,
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

            // A placa já é escura: o tint só acende/apaga, não pinta por cima
            var cores = btn.colors;
            cores.normalColor = cores.selectedColor = Color.white;
            cores.highlightedColor = new Color(1f, 0.96f, 0.85f);
            cores.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            cores.disabledColor = new Color(0.60f, 0.56f, 0.50f);
            btn.colors = cores;

            var label = Texto(go.transform, "Label", rotulo, 24f, Color.white, 0f, 0f, 1f, 1f);
            label.rectTransform.offsetMin = new Vector2(10f, 6f);
            label.rectTransform.offsetMax = new Vector2(-10f, -6f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return btn;
        }
    }
}
