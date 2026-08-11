// Assets/_QueryQuest/UI/StatusBarsUI.cs
// Barras de status do combate (placeholder):
//   - Canto superior ESQUERDO: HP do jogador + Mana
//   - Canto superior DIREITO:  HP do inimigo
//
// Hierarquia esperada no Canvas:
//
// PlayerStatusPanel (top-left)
// ├── PlayerHPBar
// │   ├── Fill        [Image, Image Type = Filled, Horizontal]
// │   └── HPText      (TMP)
// └── PlayerManaBar
//     ├── Fill        [Image, Filled Horizontal]
//     └── ManaText    (TMP)
//
// EnemyStatusPanel (top-right)
// └── EnemyHPBar
//     ├── Fill        [Image, Filled Horizontal]
//     └── HPText      (TMP)
//     └── NameText    (TMP)  (opcional)

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QueryQuest.Combat;

namespace QueryQuest.UI
{
    public class StatusBarsUI : MonoBehaviour
    {
        [Header("Jogador — HP")]
        [SerializeField] private Image playerHPFill;
        [SerializeField] private TextMeshProUGUI playerHPText;

        [Header("Jogador — Mana")]
        [SerializeField] private Image playerManaFill;
        [SerializeField] private TextMeshProUGUI playerManaText;

        [Header("Inimigo — HP")]
        [SerializeField] private Image enemyHPFill;
        [SerializeField] private TextMeshProUGUI enemyHPText;
        [SerializeField] private TextMeshProUGUI enemyNameText;

        [Header("Cores")]
        [SerializeField] private Color hpFull = new Color(0.35f, 0.80f, 0.40f);
        [SerializeField] private Color hpLow  = new Color(0.85f, 0.30f, 0.30f);
        [SerializeField] private Color manaColor = new Color(0.35f, 0.55f, 0.95f);
        [SerializeField] private Color enemyHPColor = new Color(0.80f, 0.30f, 0.30f);

        /// <summary>
        /// As barras passaram a ser ARTE (verde/azul/vermelha vindas da HUD), então
        /// o tint por código precisa sair do caminho — senão pinta por cima do
        /// sprite. Quem encolhe a barra continua sendo o fillAmount.
        /// </summary>
        public void UseArtSkin()
        {
            hpFull = hpLow = manaColor = enemyHPColor = Color.white;

            // Os números sobre as barras de arte: em preto — o branco da cena
            // some sobre o verde/azul/vermelho claros do pergaminho.
            if (playerHPText != null)   playerHPText.color = Color.black;
            if (playerManaText != null) playerManaText.color = Color.black;
            if (enemyHPText != null)    enemyHPText.color = Color.black;

            RefreshHealth();
            if (ManaSystem.Instance != null)
                RefreshMana(ManaSystem.Instance.CurrentMana, ManaSystem.Instance.MaxMana);
        }

        // ─────────────────────────────────────────────────────────────────────
        // UNITY
        // ─────────────────────────────────────────────────────────────────────

        private void Start()
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.OnHealthChanged += RefreshHealth;
                RefreshHealth();
            }

            if (ManaSystem.Instance != null)
            {
                ManaSystem.Instance.OnManaChanged += RefreshMana;
                RefreshMana(ManaSystem.Instance.CurrentMana, ManaSystem.Instance.MaxMana);
            }
        }

        private void OnDestroy()
        {
            if (CombatManager.Instance != null)
                CombatManager.Instance.OnHealthChanged -= RefreshHealth;

            if (ManaSystem.Instance != null)
                ManaSystem.Instance.OnManaChanged -= RefreshMana;
        }

        // ─────────────────────────────────────────────────────────────────────
        // ATUALIZAÇÃO
        // ─────────────────────────────────────────────────────────────────────

        private void RefreshHealth()
        {
            var cm = CombatManager.Instance;
            if (cm == null) return;

            // HP do jogador
            float playerPct = cm.PlayerMaxHP > 0 ? (float)cm.PlayerCurrentHP / cm.PlayerMaxHP : 0f;
            if (playerHPFill != null)
            {
                playerHPFill.fillAmount = playerPct;
                playerHPFill.color = Color.Lerp(hpLow, hpFull, playerPct);
            }
            if (playerHPText != null)
                playerHPText.text = $"HP {cm.PlayerCurrentHP} / {cm.PlayerMaxHP}";

            // HP do inimigo
            if (cm.CurrentEnemy != null)
            {
                float enemyPct = cm.CurrentEnemy.HP > 0 ? (float)cm.EnemyCurrentHP / cm.CurrentEnemy.HP : 0f;
                if (enemyHPFill != null)
                {
                    enemyHPFill.fillAmount = enemyPct;
                    enemyHPFill.color = enemyHPColor;
                }
                if (enemyHPText != null)
                    enemyHPText.text = $"HP {cm.EnemyCurrentHP} / {cm.CurrentEnemy.HP}";
                if (enemyNameText != null)
                    enemyNameText.text = cm.CurrentEnemy.Nome;
            }
        }

        private void RefreshMana(int current, int max)
        {
            float pct = max > 0 ? (float)current / max : 0f;
            if (playerManaFill != null)
            {
                playerManaFill.fillAmount = pct;
                playerManaFill.color = manaColor;
            }
            if (playerManaText != null)
                playerManaText.text = $"MP {current} / {max}";
        }
    }
}
