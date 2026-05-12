// Assets/_QueryQuest/Combat/TurnController.cs
using System;
using System.Collections;
using UnityEngine;
using QueryQuest.Models;

namespace QueryQuest.Combat
{
    /// <summary>
    /// Gerencia a execução do turno do inimigo (IA simples) e o timing entre turnos.
    /// Usa Coroutines para dar tempo às animações e à UI de reagir antes do próximo estado.
    /// </summary>
    public class TurnController : MonoBehaviour
    {
        [Header("Timing")]
        [Tooltip("Delay em segundos antes do inimigo atacar (tempo para animações).")]
        [SerializeField] private float enemyActionDelay = 1.2f;

        [Tooltip("Delay após aplicar dano antes de voltar ao turno do jogador.")]
        [SerializeField] private float postDamageDelay = 0.8f;

        // ─────────────────────────────────────────────────────────────────────
        // API PÚBLICA
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Executa o turno do inimigo de forma assíncrona.
        /// Chama onAttack(damage) quando o ataque ocorre, depois retorna o controle
        /// ao CombatManager via callback.
        /// </summary>
        public void ExecuteEnemyTurn(EnemyData enemy, Action<int> onAttack)
        {
            StartCoroutine(EnemyTurnRoutine(enemy, onAttack));
        }

        // ─────────────────────────────────────────────────────────────────────
        // IA DO INIMIGO
        // ─────────────────────────────────────────────────────────────────────

        private IEnumerator EnemyTurnRoutine(EnemyData enemy, Action<int> onAttack)
        {
            yield return new WaitForSeconds(enemyActionDelay);

            int damage = CalculateEnemyDamage(enemy);
            onAttack?.Invoke(damage);

            yield return new WaitForSeconds(postDamageDelay);
            // O CombatManager assume o controle após receber o dano via callback
        }

        /// <summary>
        /// IA básica: dano baseado no nível do inimigo + variação aleatória.
        /// Versões futuras podem escolher feitiços da tabela Inimigos.
        /// </summary>
        private int CalculateEnemyDamage(EnemyData enemy)
        {
            // Base: 10 + (5 por nível)
            int baseDamage = 10 + (enemy.Nivel * 5);

            // Variação: ±30%
            float variation = UnityEngine.Random.Range(0.7f, 1.3f);
            int finalDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * variation));

            return finalDamage;
        }
    }
}
