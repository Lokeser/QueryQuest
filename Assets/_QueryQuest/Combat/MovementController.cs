// Assets/_QueryQuest/Combat/MovementController.cs
// Adicione ao GameManager.
// Expõe métodos para os botões de movimento da ArenaHUD chamarem.

using UnityEngine;
using QueryQuest.Combat;

namespace QueryQuest.Combat
{
    public class MovementController : MonoBehaviour
    {
        public static MovementController Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>Move o jogador +1 slot (em direção ao inimigo).</summary>
        public void MoveForward()
        {
            if (!CanMove()) return;
            var result = SlotSystem.Instance.MovePlayer(+1);
            LogMoveResult(result, "frente");
        }

        /// <summary>Move o jogador -1 slot (se afasta do inimigo).</summary>
        public void MoveBack()
        {
            if (!CanMove()) return;
            var result = SlotSystem.Instance.MovePlayer(-1);
            LogMoveResult(result, "trás");
        }

        /// <summary>Jogador fica parado (consome ação de movimento).</summary>
        public void Stay()
        {
            if (!CanMove()) return;
            SlotSystem.Instance.StayPlayer();
            Debug.Log("[MovementController] Jogador permaneceu no lugar.");
        }

        private bool CanMove()
        {
            if (CombatManager.Instance == null) return false;
            if (CombatManager.Instance.CurrentState != CombatState.PLAYER_TURN)
            {
                Debug.Log("[MovementController] Movimento só disponível no turno do jogador.");
                return false;
            }
            if (SlotSystem.Instance == null) return false;
            if (!SlotSystem.Instance.HasMovementAction)
            {
                Debug.Log("[MovementController] Ação de movimento já usada neste turno.");
                return false;
            }
            return true;
        }

        private void LogMoveResult(MoveResult result, string direction)
        {
            switch (result)
            {
                case MoveResult.Success:
                    Debug.Log($"[MovementController] Moveu para {direction}. {SlotSystem.Instance.GetStatusString()}");
                    break;
                case MoveResult.NoActionLeft:
                    Debug.Log("[MovementController] Sem ação de movimento disponível.");
                    break;
                case MoveResult.OutOfBounds:
                    Debug.Log($"[MovementController] Não pode mover para {direction} — fora dos limites.");
                    break;
            }
        }
    }
}