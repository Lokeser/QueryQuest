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
        // Golem de Fogo(1, fácil) → Agua(2) → Terra(3) → Raio(4) → Primordial(5, boss)
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

            // O menu decide onde a run começa (1 = jogo novo, N = "Continuar")
            StartFloor(Mathf.Clamp(GameSession.AndarInicial, 1, totalFloors));
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

            // Guarda o progresso para o "Continuar" do menu (só na run normal —
            // no Modo Infinito o andar não representa mais o avanço da campanha)
            if (Loop == 0 && floor <= totalFloors) GameSession.SalvarAndar(floor);

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
        /// (cada um tem HP e elemento diferentes, em ordem crescente); no Modo
        /// Infinito, cada volta deixa todos eles mais fortes.
        /// </summary>
        private EnemyData LoadScaledEnemy(int enemyId, int floor)
        {
            var db = DatabaseManager.Instance.DB;
            var enemy = db.Find<EnemyData>(enemyId);
            if (enemy == null || Loop <= 0) return enemy;

            // Find devolve uma cópia nova a cada chamada, então dá para reforçar
            // o inimigo sem alterar o banco.
            enemy.HP = Mathf.RoundToInt(enemy.HP * (1f + 0.6f * Loop));
            enemy.Nivel += Loop;              // o golpe dele é 10 + Nivel * 5
            enemy.Nome = $"{enemy.Nome} +{Loop}";
            return enemy;
        }

        // ─────────────────────────────────────────────────────────────────────
        // MODO INFINITO
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Voltas concluídas no Modo Infinito (0 = run normal).</summary>
        public int Loop { get; private set; }

        /// <summary>
        /// Recomeça do primeiro golem, todos mais fortes, MANTENDO a build:
        /// magias desbloqueadas, itens e fragmentos continuam com o jogador.
        /// </summary>
        public void StartInfiniteMode()
        {
            Loop++;
            Debug.Log($"[FloorManager] MODO INFINITO — volta {Loop}. Build mantida.");
            CombatManager.Instance?.LogExternal(
                $"[MODO INFINITO] Volta {Loop}! Os golens voltam mais fortes — sua build continua com você.");
            StartFloor(1);
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

            // Venceu o andar: primeiro a cena de absorção do fragmento, e só
            // depois a recompensa (ou a vitória final).
            bool ultimoAndar = CurrentFloor >= totalFloors;

            System.Action continuar = () =>
            {
                if (ultimoAndar)
                {
                    Debug.Log("[FloorManager] Último andar vencido! VITÓRIA FINAL!");
                    OnRunWon?.Invoke();
                }
                else
                {
                    Debug.Log($"[FloorManager] Andar {CurrentFloor} vencido! Mostrando recompensas.");
                    OnRewardReady?.Invoke();
                }
            };

            if (FragmentDropSystem.Instance != null)
                FragmentDropSystem.Instance.BeginAbsorbPhase(CombatManager.Instance?.CurrentEnemy, continuar);
            else
                continuar();
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
            Loop = 0;
            PlayerStats.Instance?.ResetStats();
            InventarioFragmento.Instance?.Resetar();
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
                // Re-bloqueia só o que a run desbloqueou: as magias ELEMENTAIS de
                // nível 2 ou 3, que vêm dos fragmentos. As magias neutras são
                // utilitárias (Analise, Inspecionar Fragmento) e o jogador nasce
                // com elas — um reset por nível puro as trancava junto.
                db.Execute("UPDATE Magias SET Desbloqueado = 0 WHERE Nivel >= 2 AND Elemento <> 'Neutro'");
                db.Execute("UPDATE Magias SET Desbloqueado = 1 WHERE Nivel = 1 OR Elemento = 'Neutro'");
                Debug.Log("[FloorManager] Magias resetadas para o estado inicial.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FloorManager] Erro ao resetar magias: {e.Message}");
            }
        }
    }
}
