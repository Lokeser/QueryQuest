// Assets/_QueryQuest/Combat/FloorManager.cs
// Gerencia a progressão roguelike de 5 andares.
// Cada andar = 1 inimigo (em ordem crescente de dificuldade).
// Após vencer: tela de recompensa → próximo andar (com cura total).
// Após 5 andares: vitória final.

using System.Collections;
using System.Linq;
using UnityEngine;
using QueryQuest.Database;
using QueryQuest.Models;

namespace QueryQuest.Combat
{
    public class FloorManager : MonoBehaviour
    {
        public static FloorManager Instance { get; private set; }

        [Header("Configuração")]
        [SerializeField] private int totalFloors = 5;
        [SerializeField] private float startDelay = 1.5f;

        public int CurrentFloor { get; private set; } = 0; // 1 a 5
        public int TotalFloors => totalFloors;

        // IDs dos inimigos em ordem de dificuldade (do banco)
        // Alfa(1, fácil) → Beta(2) → Nulo(3) → Overflow(4) → Primordial(5, boss)
        private readonly int[] _floorEnemyIds = { 1, 2, 3, 4, 5 };

        // Eventos para a UI
        public event System.Action<int> OnFloorStarted;       // número do andar
        public event System.Action OnRunWon;                  // venceu todos os andares
        public event System.Action OnRunLost;                 // morreu
        public event System.Action OnRewardReady;             // hora de mostrar recompensas

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private IEnumerator Start()
        {
            // Aguarda os sistemas iniciarem
            yield return new WaitUntil(() =>
                DatabaseManager.Instance != null &&
                DatabaseManager.Instance.DB != null &&
                CombatManager.Instance != null);

            yield return new WaitForSeconds(startDelay);

            // Inscreve no fim de combate
            CombatManager.Instance.OnCombatEnded += OnCombatEnded;

            // Reseta stats da run
            PlayerStats.Instance?.ResetStats();

            StartFloor(1);
        }

        private void OnDestroy()
        {
            if (CombatManager.Instance != null)
                CombatManager.Instance.OnCombatEnded -= OnCombatEnded;
        }

        // ─────────────────────────────────────────────────────────────────────
        // CONTROLE DE ANDARES
        // ─────────────────────────────────────────────────────────────────────

        public void StartFloor(int floor)
        {
            CurrentFloor = floor;

            if (floor > totalFloors)
            {
                Debug.Log("[FloorManager] Todos os andares vencidos! VITÓRIA FINAL!");
                OnRunWon?.Invoke();
                return;
            }

            int enemyId = _floorEnemyIds[floor - 1];
            var enemy = LoadScaledEnemy(enemyId, floor);

            if (enemy == null)
            {
                Debug.LogError($"[FloorManager] Inimigo do andar {floor} não encontrado!");
                return;
            }

            Debug.Log($"[FloorManager] === ANDAR {floor}/{totalFloors} === {enemy.Nome} (HP {enemy.HP})");
            OnFloorStarted?.Invoke(floor);

            // Cura total entre andares
            CombatManager.Instance.StartCombat(enemy);
        }

        /// <summary>
        /// Carrega o inimigo do banco. A dificuldade já vem do próprio inimigo
        /// (cada um tem HP e elemento diferentes, em ordem crescente).
        /// </summary>
        private EnemyData LoadScaledEnemy(int enemyId, int floor)
        {
            var db = DatabaseManager.Instance.DB;
            var enemy = db.Find<EnemyData>(enemyId);
            return enemy;
        }

        // ─────────────────────────────────────────────────────────────────────
        // FIM DE COMBATE
        // ─────────────────────────────────────────────────────────────────────

        private void OnCombatEnded(bool playerWon)
        {
            if (!playerWon)
            {
                Debug.Log("[FloorManager] Jogador foi derrotado. Fim da run.");
                OnRunLost?.Invoke();
                return;
            }

            // Venceu o andar
            if (CurrentFloor >= totalFloors)
            {
                Debug.Log("[FloorManager] Último andar vencido! VITÓRIA FINAL!");
                OnRunWon?.Invoke();
            }
            else
            {
                Debug.Log($"[FloorManager] Andar {CurrentFloor} vencido! Mostrando recompensas.");
                OnRewardReady?.Invoke();
            }
        }

        /// <summary>
        /// Chamado pela RewardScreenUI após o jogador escolher um item.
        /// Avança para o próximo andar.
        /// </summary>
        public void AdvanceToNextFloor()
        {
            StartFloor(CurrentFloor + 1);
        }

        /// <summary>Reinicia a run do zero (após derrota ou vitória).</summary>
        public void RestartRun()
        {
            PlayerStats.Instance?.ResetStats();
            // Re-bloqueia todas as magias exceto as iniciais
            ResetSpellUnlocks();
            StartFloor(1);
        }

        private void ResetSpellUnlocks()
        {
            var db = DatabaseManager.Instance?.DB;
            if (db == null) return;
            try
            {
                // Mantém desbloqueadas apenas as magias iniciais (Desbloqueado padrão = 1 no seed)
                // Re-bloqueia as que foram desbloqueadas por páginas durante a run.
                db.Execute("UPDATE Magias SET Desbloqueado = 0 WHERE Nivel >= 2");
                db.Execute("UPDATE Magias SET Desbloqueado = 1 WHERE Nivel = 1");
                Debug.Log("[FloorManager] Magias resetadas para o estado inicial.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FloorManager] Erro ao resetar magias: {e.Message}");
            }
        }
    }
}
