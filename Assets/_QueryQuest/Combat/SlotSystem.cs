// Assets/_QueryQuest/Combat/SlotSystem.cs
using System;
using UnityEngine;

namespace QueryQuest.Combat
{
    /// <summary>
    /// Gerencia as posições do jogador e inimigo nos 6 slots da arena.
    /// Slots numerados de 1 a 6: jogador começa no 1, inimigo no 6.
    /// 
    /// Regras:
    /// - Jogador e inimigo NÃO podem ocupar o mesmo slot (param adjacentes)
    /// - Magia CURTA atinge playerSlot + 1
    /// - Magia MEDIA atinge playerSlot + 2
    /// - Magia LONGA atinge playerSlot + 3
    /// - Acerta apenas se o inimigo estiver EXATAMENTE no slot atingido
    /// - Inimigo se aproxima 1 slot por turno (direção ao player)
    /// </summary>
    public class SlotSystem : MonoBehaviour
    {
        public static SlotSystem Instance { get; private set; }

        public const int SLOT_MIN    = 1;
        public const int SLOT_MAX    = 6;
        public const int PLAYER_START = 1;
        public const int ENEMY_START  = 6;

        public int PlayerSlot { get; private set; } = PLAYER_START;
        public int EnemySlot  { get; private set; } = ENEMY_START;

        // Ação de movimento disponível neste turno
        public bool HasMovementAction { get; private set; } = true;

        // Eventos para a UI reagir
        public event Action<int, int> OnPositionsChanged; // playerSlot, enemySlot
        public event Action<System.Collections.Generic.List<int>> OnSpellRangeHighlight; // slots atingidos (vazio = limpar)

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ─────────────────────────────────────────────────────────────────────
        // RESET
        // ─────────────────────────────────────────────────────────────────────

        public void ResetPositions()
        {
            PlayerSlot = PLAYER_START;
            EnemySlot  = ENEMY_START;
            HasMovementAction = true;
            NotifyPositionChange();
        }

        public void ResetMovementAction()
        {
            HasMovementAction = true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // MOVIMENTO DO JOGADOR
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Move o jogador +1 (em direção ao inimigo) ou -1 (se afasta).</summary>
        public MoveResult MovePlayer(int direction)
        {
            if (!HasMovementAction)
                return MoveResult.NoActionLeft;

            int target = PlayerSlot + direction;

            if (target < SLOT_MIN || target > SLOT_MAX)
                return MoveResult.OutOfBounds;

            if (target == EnemySlot)
                return MoveResult.Blocked;

            PlayerSlot = target;
            HasMovementAction = false;

            Debug.Log($"[SlotSystem] Jogador moveu para slot {PlayerSlot}");
            NotifyPositionChange();
            return MoveResult.Success;
        }

        /// <summary>Jogador escolhe não se mover (consome a ação de movimento).</summary>
        public void StayPlayer()
        {
            HasMovementAction = false;
            Debug.Log($"[SlotSystem] Jogador ficou no slot {PlayerSlot}");
        }

        // ─────────────────────────────────────────────────────────────────────
        // MOVIMENTO DO INIMIGO (IA)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Inimigo se aproxima 1 slot em direção ao jogador.
        /// Chamado automaticamente no turno do inimigo.
        /// </summary>
        public void MoveEnemyTowardsPlayer()
        {
            int direction = EnemySlot > PlayerSlot ? -1 : 1;
            int target = EnemySlot + direction;

            // Nunca entra no slot do jogador — para no adjacente
            if (EnemySlot == PlayerSlot || target == PlayerSlot)
            {
                Debug.Log("[SlotSystem] Inimigo já está encostado no jogador — não avança.");
                return;
            }

            EnemySlot = target;

            Debug.Log($"[SlotSystem] Inimigo moveu para slot {EnemySlot}");
            NotifyPositionChange();
        }

        // ─────────────────────────────────────────────────────────────────────
        // CÁLCULO DE ALCANCE
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Retorna TODOS os slots atingidos pelo feitiço (área de efeito).
        /// CURTO = [slot+1] | MEDIO = [slot+1, slot+2] | LONGO = [slot+1, slot+2, slot+3].
        /// O slot do próprio jogador nunca conta.
        /// </summary>
        public System.Collections.Generic.List<int> GetTargetSlots(string distancia)
        {
            int reach = distancia.ToUpper() switch
            {
                "CURTO" => 1,
                "MEDIO" => 2,
                "LONGO" => 3,
                _       => 1
            };

            int direction = EnemySlot >= PlayerSlot ? 1 : -1;
            var slots = new System.Collections.Generic.List<int>();

            for (int step = 1; step <= reach; step++)
            {
                int s = PlayerSlot + (step * direction);
                if (s >= SLOT_MIN && s <= SLOT_MAX)
                    slots.Add(s);
            }
            return slots;
        }

        /// <summary>
        /// Retorna o slot mais distante atingido (para compatibilidade / centro do efeito).
        /// </summary>
        public int GetTargetSlot(string distancia)
        {
            var slots = GetTargetSlots(distancia);
            return slots.Count > 0 ? slots[slots.Count - 1] : PlayerSlot;
        }

        /// <summary>
        /// Verifica se o feitiço acerta o inimigo.
        /// Acerta se o inimigo estiver em QUALQUER slot dentro da área do feitiço.
        /// </summary>
        public bool SpellHitsEnemy(string distancia)
        {
            return GetTargetSlots(distancia).Contains(EnemySlot);
        }

        /// <summary>Distância em slots entre jogador e inimigo.</summary>
        public int GetDistance() => Mathf.Abs(EnemySlot - PlayerSlot);

        // ─────────────────────────────────────────────────────────────────────
        // HIGHLIGHT DE ALCANCE (para a UI)
        // ─────────────────────────────────────────────────────────────────────

        public void ShowSpellRange(string distancia)
        {
            OnSpellRangeHighlight?.Invoke(GetTargetSlots(distancia));
        }

        /// <summary>Mostra os slots atingidos diretamente (para o flash de 2s).</summary>
        public void ShowSpellRangeSlots(System.Collections.Generic.List<int> slots)
        {
            OnSpellRangeHighlight?.Invoke(slots);
        }

        public void HideSpellRange()
        {
            OnSpellRangeHighlight?.Invoke(new System.Collections.Generic.List<int>());
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private void NotifyPositionChange()
        {
            OnPositionsChanged?.Invoke(PlayerSlot, EnemySlot);
        }

        public string GetStatusString()
            => $"Jogador: Slot {PlayerSlot} | Inimigo: Slot {EnemySlot} | Distância: {GetDistance()} slot(s)";
    }

    public enum MoveResult
    {
        Success,
        NoActionLeft,
        OutOfBounds,
        Blocked,       // slot ocupado pelo inimigo
    }
}
