// Assets/_QueryQuest/UI/ArenaHUD.cs
// Hierarquia esperada no Canvas (abaixo do CombatHUD, acima do GrimoirePanel):
//
// ArenaSection
// ├── ArenaUI            (ArenaUI.cs) — os 6 slots visuais
// │   └── SlotsContainer [HorizontalLayoutGroup spacing=8]
// └── MovementBar        [HorizontalLayoutGroup]
//     ├── BtnBack        [Button] "< Recuar"
//     ├── BtnStay        [Button] "Manter"
//     └── BtnForward     [Button] "Avançar >"

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QueryQuest.Combat;

namespace QueryQuest.UI
{
    public class ArenaHUD : MonoBehaviour
    {
        [Header("Botões de Movimento")]
        [SerializeField] private Button btnBack;
        [SerializeField] private Button btnStay;
        [SerializeField] private Button btnForward;

        [Header("Info de Slots")]
        [SerializeField] private TextMeshProUGUI slotInfoText;

        [Header("Cores dos botões")]
        [SerializeField] private Color btnActiveColor   = new Color(0.27f, 0.25f, 0.45f);
        [SerializeField] private Color btnInactiveColor = new Color(0.12f, 0.12f, 0.18f);

        private void Awake()
        {
            btnBack?.onClick.AddListener(OnBackClicked);
            btnStay?.onClick.AddListener(OnStayClicked);
            btnForward?.onClick.AddListener(OnForwardClicked);
        }

        private void Start()
        {
            if (CombatManager.Instance != null)
                CombatManager.Instance.OnStateChanged += OnStateChanged;

            if (SlotSystem.Instance != null)
                SlotSystem.Instance.OnPositionsChanged += OnPositionsChanged;

            RefreshButtons(CombatManager.Instance?.CurrentState ?? CombatState.IDLE);
        }

        private void OnDestroy()
        {
            if (CombatManager.Instance != null)
                CombatManager.Instance.OnStateChanged -= OnStateChanged;

            if (SlotSystem.Instance != null)
                SlotSystem.Instance.OnPositionsChanged -= OnPositionsChanged;
        }

        // ─────────────────────────────────────────────────────────────────────
        // EVENTOS
        // ─────────────────────────────────────────────────────────────────────

        private void OnStateChanged(CombatState state)
        {
            RefreshButtons(state);
        }

        private void OnPositionsChanged(int playerSlot, int enemySlot)
        {
            if (slotInfoText != null)
                slotInfoText.text = SlotSystem.Instance?.GetStatusString() ?? "";
        }

        // ─────────────────────────────────────────────────────────────────────
        // BOTÕES
        // ─────────────────────────────────────────────────────────────────────

        private void OnBackClicked()
        {
            MovementController.Instance?.MoveBack();
            RefreshButtons(CombatManager.Instance?.CurrentState ?? CombatState.IDLE);
        }

        private void OnStayClicked()
        {
            MovementController.Instance?.Stay();
            RefreshButtons(CombatManager.Instance?.CurrentState ?? CombatState.IDLE);
        }

        private void OnForwardClicked()
        {
            MovementController.Instance?.MoveForward();
            RefreshButtons(CombatManager.Instance?.CurrentState ?? CombatState.IDLE);
        }

        // ─────────────────────────────────────────────────────────────────────
        // VISUAL
        // ─────────────────────────────────────────────────────────────────────

        private void RefreshButtons(CombatState state)
        {
            bool isPlayerTurn = state == CombatState.PLAYER_TURN;
            bool hasMovement  = SlotSystem.Instance?.HasMovementAction ?? false;
            bool canMove      = isPlayerTurn && hasMovement;

            SetButton(btnBack,    canMove);
            SetButton(btnStay,    canMove);
            SetButton(btnForward, canMove);
        }

        private void SetButton(Button btn, bool active)
        {
            if (btn == null) return;
            btn.interactable = active;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = active ? btnActiveColor : btnInactiveColor;
        }
    }
}
