using UnityEngine;
using System.Collections;
using QueryQuest.Combat;
using QueryQuest.Database;

public class StartCombat : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f);
        var enemy = DatabaseManager.Instance.DB
            .Find<QueryQuest.Models.EnemyData>(1);
        CombatManager.Instance.StartCombat(enemy);
        Debug.Log("[StartCombat] Combate iniciado!");
    }
}