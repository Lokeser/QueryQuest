// Assets/_QueryQuest/UI/RewardScreenUI.cs
// Tela de recompensa exibida após vencer um andar.
// Mostra 3 itens; o jogador escolhe 1, que é aplicado, e avança de andar.
//
// Hierarquia esperada:
//
// RewardScreen (este script)  [Image = fundo escurecido tela cheia, começa inativo]
// ├── TitleText               (TMP) "ESCOLHA UMA RECOMPENSA"
// └── CardsRow                [HorizontalLayoutGroup]
//     ├── Card1               [Button + Image]
//     │   ├── CardTitle       (TMP)
//     │   └── CardDesc        (TMP)
//     ├── Card2 (igual)
//     └── Card3 (igual)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QueryQuest.Combat;

namespace QueryQuest.UI
{
    public class RewardScreenUI : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private GameObject panel;       // o painel inteiro (liga/desliga)
        [SerializeField] private Button[] cardButtons;   // 3 botões
        [SerializeField] private TextMeshProUGUI[] cardTitles; // 3 títulos
        [SerializeField] private TextMeshProUGUI[] cardDescs;  // 3 descrições

        [Header("Cores por tipo")]
        [SerializeField] private Color armorColor = new Color(0.30f, 0.45f, 0.70f);
        [SerializeField] private Color staffColor = new Color(0.70f, 0.40f, 0.30f);
        [SerializeField] private Color pageColor  = new Color(0.45f, 0.35f, 0.65f);

        private List<RewardItem> _currentRewards;

        private void Awake()
        {
            // Conecta os botões
            for (int i = 0; i < cardButtons.Length; i++)
            {
                int index = i; // captura local
                cardButtons[i]?.onClick.AddListener(() => OnCardChosen(index));
            }
        }

        private void Start()
        {
            if (FloorManager.Instance != null)
                FloorManager.Instance.OnRewardReady += ShowRewards;

            HidePanel();
        }

        private void OnDestroy()
        {
            if (FloorManager.Instance != null)
                FloorManager.Instance.OnRewardReady -= ShowRewards;
        }

        // ─────────────────────────────────────────────────────────────────────
        // EXIBIÇÃO
        // ─────────────────────────────────────────────────────────────────────

        public void ShowRewards()
        {
            _currentRewards = RewardGenerator.Generate(3);

            for (int i = 0; i < cardButtons.Length && i < _currentRewards.Count; i++)
            {
                var item = _currentRewards[i];

                if (cardTitles[i] != null) cardTitles[i].text = item.Title;
                if (cardDescs[i]  != null) cardDescs[i].text  = item.Description;

                // Cor do card por tipo
                var img = cardButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = item.Type switch
                    {
                        RewardType.Armor => armorColor,
                        RewardType.Staff => staffColor,
                        RewardType.Page  => pageColor,
                        _ => Color.gray
                    };
            }

            ShowPanel();
        }

        private void OnCardChosen(int index)
        {
            if (_currentRewards == null || index >= _currentRewards.Count) return;

            var chosen = _currentRewards[index];
            Debug.Log($"[RewardScreen] Jogador escolheu: {chosen.Title}");

            RewardGenerator.ApplyReward(chosen);
            HidePanel();

            // Avança para o próximo andar
            FloorManager.Instance?.AdvanceToNextFloor();
        }

        // ─────────────────────────────────────────────────────────────────────
        // LIGA/DESLIGA
        // ─────────────────────────────────────────────────────────────────────

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
