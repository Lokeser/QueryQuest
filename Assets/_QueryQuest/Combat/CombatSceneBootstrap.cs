// Assets/_QueryQuest/Combat/CombatSceneBootstrap.cs
//
// Substitui o CombatTestBootstrap — inicia o combate na cena real
// e aguarda o input do jogador via HUD (não executa nada automaticamente).
//
// Como usar:
//   1. Adicione este componente no GameManager
//   2. Configure o enemyId no Inspector
//   3. Play — o HUD aparece e o jogador controla tudo

using System.Collections;
using UnityEngine;
using QueryQuest.Database;
using QueryQuest.Models;

namespace QueryQuest.Combat
{
    public class CombatSceneBootstrap : MonoBehaviour
    {
        [Header("Configuração do Combate")]
        [Tooltip("Id do inimigo na tabela Inimigos (1 a 5)")]
        [SerializeField] private int enemyId = 1;

        [Tooltip("Delay antes de iniciar o combate (para a cena carregar)")]
        [SerializeField] private float startDelay = 1.5f;

        private IEnumerator Start()
        {
            // Aguarda o banco estar pronto
            yield return new WaitUntil(() =>
                DatabaseManager.Instance != null &&
                DatabaseManager.Instance.DB != null);

            yield return new WaitForSeconds(startDelay);

            var combat = CombatManager.Instance;
            if (combat == null)
            {
                Debug.LogError("[CombatSceneBootstrap] CombatManager não encontrado!");
                yield break;
            }

            // Busca o inimigo
            var enemy = DatabaseManager.Instance.DB.Find<EnemyData>(enemyId);
            if (enemy == null)
            {
                Debug.LogError($"[CombatSceneBootstrap] Inimigo Id={enemyId} não encontrado.");
                yield break;
            }

            Debug.Log($"[CombatSceneBootstrap] Iniciando combate: {enemy.Nome}");

            // Inicia — a partir daqui o jogador controla via HUD
            combat.StartCombat(enemy);

            // PLAYER_TURN — aguarda o jogador clicar em "Grimório" ou "Fugir"
            // Nenhuma ação automática aqui.
        }
    }
}