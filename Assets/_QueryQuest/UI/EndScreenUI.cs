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
            if (resultTitle != null)    resultTitle.text = "VITÓRIA!";
            if (resultSubtitle != null) resultSubtitle.text =
                "Você superou todos os 5 andares e dominou a arte do SQL arcano!";
            if (background != null)     background.color = victoryColor;
            ShowPanel();
        }

        private void ShowDefeat()
        {
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
