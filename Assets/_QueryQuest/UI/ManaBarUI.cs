// Assets/_QueryQuest/UI/ManaBarUI.cs
// Barra de mana exibida na HUD de combate.
//
// Hierarquia esperada:
//
// ManaBar (este script)
// ├── BarBackground   [Image]
// │   └── BarFill      [Image, tipo Filled Horizontal]
// └── ManaText        (TMP) "80 / 100"

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QueryQuest.Combat;

namespace QueryQuest.UI
{
    public class ManaBarUI : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private Image barFill;
        [SerializeField] private TextMeshProUGUI manaText;

        [Header("Cores")]
        [SerializeField] private Color manaFull = new Color(0.35f, 0.55f, 0.95f);
        [SerializeField] private Color manaLow  = new Color(0.85f, 0.35f, 0.55f);

        private void Start()
        {
            if (ManaSystem.Instance != null)
            {
                ManaSystem.Instance.OnManaChanged += UpdateBar;
                UpdateBar(ManaSystem.Instance.CurrentMana, ManaSystem.Instance.MaxMana);
            }
        }

        private void OnDestroy()
        {
            if (ManaSystem.Instance != null)
                ManaSystem.Instance.OnManaChanged -= UpdateBar;
        }

        private void UpdateBar(int current, int max)
        {
            float pct = max > 0 ? (float)current / max : 0f;

            if (barFill != null)
            {
                barFill.fillAmount = pct;
                barFill.color = Color.Lerp(manaLow, manaFull, pct);
            }

            if (manaText != null)
                manaText.text = $"{current} / {max}";
        }
    }
}
