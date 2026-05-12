// Assets/_QueryQuest/Combat/CombatTestBootstrap.cs
// ARQUIVO TEMPORÁRIO DE TESTE — remova antes do build final
//
// Como usar:
//   1. Adicione este componente no mesmo GameObject que o CombatManager
//   2. Play a cena
//   3. No Console, veja o combate executar automaticamente
//   4. Ajuste testQuery para testar diferentes queries
//
using System.Collections;
using UnityEngine;
using QueryQuest.Database;

namespace QueryQuest.Combat
{
    public class CombatTestBootstrap : MonoBehaviour
    {
        [Header("Teste")]
        [SerializeField] private int enemyId = 1; // Id do inimigo na tabela Inimigos

        [TextArea(2, 4)]
        [SerializeField] private string testQuery = "SELECT * FROM Feiticos WHERE Elemento = 'Fogo' AND Distancia = 'MEDIO'";

        private IEnumerator Start()
        {
            // Espera o DatabaseManager inicializar
            yield return new WaitUntil(() => DatabaseManager.Instance != null && DatabaseManager.Instance.DB != null);
            yield return null; // um frame extra

            var combat = CombatManager.Instance;
            if (combat == null)
            {
                Debug.LogError("[TestBootstrap] CombatManager não encontrado!");
                yield break;
            }

            // Inscreve no log de combate
            combat.OnCombatLog     += msg => Debug.Log($"[COMBAT LOG] {msg}");
            combat.OnStateChanged  += state => Debug.Log($"[STATE] {state}");
            combat.OnDamageApplied += (dmg, isPlayer) =>
                Debug.Log($"[DAMAGE] {(isPlayer ? "Jogador atacou" : "Inimigo atacou")}: {dmg.FinalDamage} dmg");
            combat.OnCombatEnded   += won =>
                Debug.Log($"[RESULT] {(won ? "VITÓRIA" : "DERROTA")}");

            // Busca o inimigo no banco
            var db = DatabaseManager.Instance.DB;
            var enemy = db.Find<QueryQuest.Models.EnemyData>(enemyId);
            if (enemy == null)
            {
                Debug.LogError($"[TestBootstrap] Inimigo com Id={enemyId} não encontrado.");
                yield break;
            }

            Debug.Log($"[TestBootstrap] Iniciando combate contra: {enemy.Nome}");

            // Inicia combate
            combat.StartCombat(enemy);

            // Aguarda turno do jogador
            yield return new WaitUntil(() => combat.CurrentState == CombatState.PLAYER_TURN);
            yield return new WaitForSeconds(0.5f);

            // Abre o grimório
            Debug.Log($"[TestBootstrap] Abrindo grimório e executando: {testQuery}");
            combat.OpenGrimoire();

            yield return new WaitForSeconds(0.2f);

            // Submete a query de teste
            var result = combat.SubmitQuery(testQuery);
            Debug.Log($"[TestBootstrap] Query success={result.Success} | spell={result.SelectedSpell?.Nome ?? "nenhum"}");
        }
    }
}
