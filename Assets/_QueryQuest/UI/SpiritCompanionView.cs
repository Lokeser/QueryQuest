// Assets/_QueryQuest/UI/SpiritCompanionView.cs
// A lua alada — o espírito que acompanha o herói.
// Flutua ATRÁS e ACIMA do jogador (acompanha quando ele troca de slot) e,
// no início do combate, abre um balão com a dica sobre a fraqueza do golem.
//
// Criado por código pelo ArenaSceneView; nada para configurar no Inspector.

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using QueryQuest.Combat;

namespace QueryQuest.UI
{
    public class SpiritCompanionView : MonoBehaviour
    {
        [Header("Posição relativa ao herói")]
        [Tooltip("Deslocamento horizontal: negativo = atrás do herói (ele encara a direita).")]
        [SerializeField] private float offsetX = -95f;
        [Tooltip("Altura acima da base do herói, como fração da altura dele.")]
        [SerializeField] private float heightFactor = 0.95f;

        [Header("Visual")]
        [SerializeField] private float spiritHeight = 105f;
        [SerializeField] private float bobAmplitude = 9f;
        [SerializeField] private float bobPeriod    = 2.4f;
        [SerializeField] private float animFps      = 12f;

        [Header("Balão")]
        [SerializeField] private Vector2 bubbleSize = new Vector2(430f, 132f);
        [SerializeField] private float bubbleDuration = 4f;

        private RectTransform _spiritRT;
        private Image _spiritImg;
        private Sprite[] _frames;

        private RectTransform _bubbleRT;
        private TextMeshProUGUI _bubbleText;
        private Coroutine _hideRoutine;

        private Vector2 _anchor;   // posição-base (sem o bob), em coords locais do pai
        private float _bobTimer;
        private int _frame;
        private float _frameTimer;

        // ─────────────────────────────────────────────────────────────────────
        // CONSTRUÇÃO
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _frames = SpriteSheetLoader.Load("Sprites/Spirit/lua_sheet");
            BuildSpirit();
            BuildBubble();
        }

        private void Start()
        {
            if (SpiritCompanion.Instance != null)
                SpiritCompanion.Instance.OnSpiritSpeak += Speak;
        }

        private void OnDestroy()
        {
            if (SpiritCompanion.Instance != null)
                SpiritCompanion.Instance.OnSpiritSpeak -= Speak;
        }

        private void BuildSpirit()
        {
            var go = new GameObject("SpiritMoon");
            go.transform.SetParent(transform, false);

            _spiritRT = go.AddComponent<RectTransform>();
            _spiritRT.anchorMin = new Vector2(0.5f, 0.5f);
            _spiritRT.anchorMax = new Vector2(0.5f, 0.5f);
            _spiritRT.pivot     = new Vector2(0.5f, 0.5f);

            float aspect = 1f;
            if (_frames.Length > 0)
            {
                var r = _frames[0].rect;
                aspect = r.width / r.height;
            }
            _spiritRT.sizeDelta = new Vector2(spiritHeight * aspect, spiritHeight);

            // Orientação original do sheet (de frente, sem espelhar)
            _spiritRT.localScale = Vector3.one;

            _spiritImg = go.AddComponent<Image>();
            _spiritImg.raycastTarget = true;    // clicável: pede a próxima dica
            _spiritImg.preserveAspect = true;
            if (_frames.Length > 0) _spiritImg.sprite = _frames[0];

            go.AddComponent<SpiritClickTarget>();
        }

        private void BuildBubble()
        {
            var go = new GameObject("SpiritBubble");
            go.transform.SetParent(transform, false);

            _bubbleRT = go.AddComponent<RectTransform>();
            _bubbleRT.anchorMin = new Vector2(0.5f, 0.5f);
            _bubbleRT.anchorMax = new Vector2(0.5f, 0.5f);
            _bubbleRT.pivot     = new Vector2(0.5f, 0f);   // cresce para cima, a partir da lua
            _bubbleRT.sizeDelta = bubbleSize;

            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            var frame = Resources.Load<Sprite>("Sprites/UI/hud_balao");
            if (frame != null) UiFrame.Apply(img, frame);
            else img.color = new Color(0.96f, 0.91f, 0.76f, 0.95f);

            // Texto dentro do balão (tinta escura sobre pergaminho)
            var textGO = new GameObject("BubbleText");
            textGO.transform.SetParent(go.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(34f, 22f);
            textRT.offsetMax = new Vector2(-34f, -22f);

            _bubbleText = textGO.AddComponent<TextMeshProUGUI>();
            _bubbleText.fontSize = 15f;
            _bubbleText.color = new Color(0.24f, 0.15f, 0.06f);
            _bubbleText.alignment = TextAlignmentOptions.Center;
            _bubbleText.textWrappingMode = TextWrappingModes.Normal;
            _bubbleText.raycastTarget = false;

            go.SetActive(false);
        }

        // ─────────────────────────────────────────────────────────────────────
        // POSIÇÃO E ANIMAÇÃO
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Chamado pelo ArenaSceneView: 'playerBase' é a posição dos pés do herói.</summary>
        public void FollowPlayer(Vector2 playerBase, float playerHeight)
        {
            _anchor = new Vector2(playerBase.x + offsetX, playerBase.y + playerHeight * heightFactor);
        }

        private void Update()
        {
            if (_spiritRT == null) return;

            // Flutuação suave
            _bobTimer += Time.deltaTime;
            float bob = Mathf.Sin(_bobTimer / Mathf.Max(0.1f, bobPeriod) * Mathf.PI * 2f) * bobAmplitude;
            _spiritRT.anchoredPosition = _anchor + new Vector2(0f, bob);

            if (_bubbleRT != null && _bubbleRT.gameObject.activeSelf)
            {
                _bubbleRT.anchoredPosition = _anchor + new Vector2(
                    _spiritRT.sizeDelta.x * 0.5f + 10f,
                    _spiritRT.sizeDelta.y * 0.5f + 6f + bob * 0.4f);
            }

            // Frames da lua
            if (_frames.Length > 1)
            {
                _frameTimer += Time.deltaTime;
                if (_frameTimer >= 1f / Mathf.Max(1f, animFps))
                {
                    _frameTimer = 0f;
                    _frame = (_frame + 1) % _frames.Length;
                    _spiritImg.sprite = _frames[_frame];
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // BALÃO DE FALA
        // ─────────────────────────────────────────────────────────────────────

        public void Speak(string message)
        {
            if (_bubbleText == null) return;

            _bubbleText.text = message;
            _bubbleRT.gameObject.SetActive(true);

            if (_hideRoutine != null) StopCoroutine(_hideRoutine);
            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(bubbleDuration);
            if (_bubbleRT != null) _bubbleRT.gameObject.SetActive(false);
        }
    }

    /// <summary>Clicar na lua pede a próxima dica (as 3 da partida, em ordem).</summary>
    public class SpiritClickTarget : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            if (SpiritCompanion.Instance != null) SpiritCompanion.Instance.NextHint();
        }
    }
}
