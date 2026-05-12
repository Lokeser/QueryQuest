// Assets/_QueryQuest/Combat/DamageCalculator.cs
using System.Collections.Generic;
using QueryQuest.Models;
using UnityEngine;

namespace QueryQuest.Combat
{
    /// <summary>
    /// Fórmula: Dano = (PoderBase × MultiplicadorElemento) × EficienciaDistancia
    /// </summary>
    public static class DamageCalculator
    {
        // Eficiência de distância: feitiço funciona melhor na distância ideal
        private static readonly Dictionary<string, Dictionary<string, float>> DistanceEfficiency = new()
        {
            // Feitiço CURTO
            ["CURTO"] = new() { ["CURTO"] = 1.0f, ["MEDIO"] = 0.6f, ["LONGO"] = 0.3f },
            // Feitiço MÉDIO
            ["MEDIO"] = new() { ["CURTO"] = 0.6f, ["MEDIO"] = 1.0f, ["LONGO"] = 0.6f },
            // Feitiço LONGO
            ["LONGO"] = new() { ["CURTO"] = 0.3f, ["MEDIO"] = 0.6f, ["LONGO"] = 1.0f },
        };

        /// <summary>
        /// Calcula o dano final.
        /// </summary>
        /// <param name="spell">Feitiço lançado pelo jogador.</param>
        /// <param name="enemy">Inimigo alvo.</param>
        /// <param name="currentDistance">Distância atual do combate (CURTO/MEDIO/LONGO).</param>
        /// <returns>DamageResult com dano e breakdown para exibição.</returns>
        public static DamageResult Calculate(SpellData spell, EnemyData enemy, string currentDistance)
        {
            float elementalMult  = ElementalSystem.GetMultiplier(spell.Elemento, enemy.Elemento);
            float distEfficiency = GetDistanceEfficiency(spell.Distancia, currentDistance);

            float rawDamage   = spell.DanoBase * elementalMult * distEfficiency;
            int   finalDamage = Mathf.Max(1, Mathf.RoundToInt(rawDamage));

            return new DamageResult
            {
                FinalDamage       = finalDamage,
                ElementalMult     = elementalMult,
                DistEfficiency    = distEfficiency,
                Effectiveness     = ElementalSystem.GetEffectivenessLabel(elementalMult),
                Breakdown         = $"{spell.DanoBase} × {elementalMult:F1}(elm) × {distEfficiency:F1}(dist) = {finalDamage}"
            };
        }

        private static float GetDistanceEfficiency(string spellDistance, string combatDistance)
        {
            string spellDist  = spellDistance.ToUpper();
            string combatDist = combatDistance.ToUpper();

            if (DistanceEfficiency.TryGetValue(spellDist, out var inner) &&
                inner.TryGetValue(combatDist, out float eff))
                return eff;

            Debug.LogWarning($"[DamageCalc] Distância inválida: feitiço='{spellDist}' combate='{combatDist}'");
            return 1.0f;
        }
    }

    public class DamageResult
    {
        public int    FinalDamage    { get; set; }
        public float  ElementalMult  { get; set; }
        public float  DistEfficiency { get; set; }
        public string Effectiveness  { get; set; }
        public string Breakdown      { get; set; } // Para exibir no log do grimório
    }
}
