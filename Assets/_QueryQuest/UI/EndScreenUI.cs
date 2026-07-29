// Assets/_QueryQuest/UI/EndScreenUI.cs
// Telas de fim de run: Vitória Final (venceu 5 andares) e Game Over (morreu).
//
// Hierarquia esperada:
//
// EndScreen (este script)  [Image fundo tela cheia, começa inativo]
// ├── ResultTitle          (TMP) — "VITÓRIA!" ou "DERROTA"
// ├── ResultSubtitle       (TMP) — mensagem
// └── RestartButton        [Button] "Jogar Novamente"

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QueryQuest.Combat;

namespace QueryQuest.UI
{
    public class EndScreenUI : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI resultTitle;
        [SerializeField] private TextMeshProUGUI resultSubtitle;
        [SerializeField] private Button restartButton;
        [SerializeField] private Image background;

        [Header("Cores")]
        [SerializeField] private Color victoryColor = new Color(0.20f, 0.50f, 0.30f, 0.95f);
        [SerializeField] private Color defeatColor  = new Color(0.50f, 0.15f, 0.15f, 0.95f);

        private void Awake()
        {
            restartButton?.onClick.AddListener(OnRestartClicked);
        }

        private void Start()
        {
            if (FloorManager.Instance != null)
            {
                FloorManager.Instance.OnRunWon  += ShowVictory;
                FloorManager.Instance.OnRunLost += ShowDefeat;
            }
            HidePanel();
        }

        private void OnDestroy()
        {
            if (FloorManager.Instance != null)
            {
                FloorManager.Instance.OnRunWon  -= ShowVictory;
                FloorManager.Instance.OnRunLost -= ShowDefeat;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // EXIBIÇÃO
        // ─────────────────────────────────────────────────────────────────────

        private void ShowVictory()
        {
            int loop = FloorManager.Instance != null ? FloorManager.Instance.Loop : 0;

            if (resultTitle != null)    resultTitle.text = loop > 0 ? $"VITÓRIA! (volta {loop})" : "VITÓRIA!";
            if (resultSubtitle != null) resultSubtitle.text =
                "Você superou todos os 5 andares e dominou a arte do SQL arcano!\n" +
                "No Modo Infinito os golens voltam mais fortes — e você mantém sua build.";
            if (background != null)     background.color = victoryColor;

            EnsureInfiniteButton();
            if (_infiniteButton != null) _infiniteButton.gameObject.SetActive(true);

            ShowPanel();
        }

        // ─────────────────────────────────────────────────────────────────────
        // MODO INFINITO (só aparece na vitória)
        // ─────────────────────────────────────────────────────────────────────

        private Button _infiniteButton;

        private void EnsureInfiniteButton()
        {
            if (_infiniteButton != null || restartButton == null) return;

            // Nasce ao lado do botão de reiniciar, herdando o mesmo visual
            var go = Instantiate(restartButton.gameObject, restartButton.transform.parent);
            go.name = "InfiniteButton";

            var rt = (RectTransform)go.transform;
            var baseRT = (RectTransform)restartButton.transform;
            rt.anchorMin = baseRT.anchorMin;
            rt.anchorMax = baseRT.anchorMax;
            rt.pivot     = baseRT.pivot;
            rt.sizeDelta = baseRT.sizeDelta;
            rt.anchoredPosition = baseRT.anchoredPosition + new Vector2(0f, -(baseRT.sizeDelta.y + 16f));

            foreach (var tmp in go.GetComponentsInChildren<TextMeshProUGUI>(true))
                tmp.text = "MODO INFINITO";

            _infiniteButton = go.GetComponent<Button>();
            _infiniteButton.onClick.RemoveAllListeners();
            _infiniteButton.onClick.AddListener(OnInfiniteClicked);
        }

        private void OnInfiniteClicked()
        {
            HidePanel();
            FloorManager.Instance?.StartInfiniteMode();
        }

        private void ShowDefeat()
        {
            if (_infiniteButton != null) _infiniteButton.gameObject.SetActive(false);

            if (resultTitle != null)    resultTitle.text = "DERROTA";
            if (resultSubtitle != null) resultSubtitle.text =
                $"Você caiu no andar {FloorManager.Instance?.CurrentFloor}. " +
                "Refine suas queries e tente novamente!";
            if (background != null)     background.color = defeatColor;
            ShowPanel();
        }

        private void OnRestartClicked()
        {
            HidePanel();
            FloorManager.Instance?.RestartRun();
        }

        private void ShowPanel()
        {
            if (panel != null) panel.SetActive(true);
            else gameObject.SetActive(true);
        }

        private void HidePanel()
        {
            if (panel != null) panel.SetActive(false);
            else gameObject.SetActive(false);
        }
    }
}
