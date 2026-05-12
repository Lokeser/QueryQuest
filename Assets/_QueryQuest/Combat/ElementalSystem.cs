// Assets/_QueryQuest/Combat/ElementalSystem.cs
using System.Collections.Generic;
using UnityEngine;

namespace QueryQuest.Combat
{
    /// <summary>
    /// Ciclo elemental: Água ➔ Fogo ➔ Vento ➔ Terra ➔ Raio ➔ Água
    /// (cada elemento é forte contra o anterior no ciclo)
    /// </summary>
    public static class ElementalSystem
    {
        // Chave: elemento atacante | Valor: elemento que ele é FORTE contra
        private static readonly Dictionary<string, string> StrongAgainst = new(System.StringComparer.OrdinalIgnoreCase)
        {
            ["Agua"]  = "Fogo",
            ["Fogo"]  = "Vento",
            ["Vento"] = "Terra",
            ["Terra"] = "Raio",
            ["Raio"]  = "Agua",
        };

        private const float SuperEffectiveMultiplier = 1.5f;
        private const float NeutralMultiplier         = 1.0f;
        private const float ResistantMultiplier       = 0.75f;

        /// <summary>
        /// Retorna o multiplicador elemental do ataque.
        /// </summary>
        public static float GetMultiplier(string attackElement, string defenderElement)
        {
            if (!StrongAgainst.TryGetValue(attackElement, out string strongTarget))
            {
                Debug.LogWarning($"[ElementalSystem] Elemento desconhecido: '{attackElement}'");
                return NeutralMultiplier;
            }

            // Atacante é FORTE contra o defensor
            if (string.Equals(strongTarget, defenderElement, System.StringComparison.OrdinalIgnoreCase))
                return SuperEffectiveMultiplier;

            // Defensor é forte contra o atacante (resistência)
            if (StrongAgainst.TryGetValue(defenderElement, out string defenderStrong) &&
                string.Equals(defenderStrong, attackElement, System.StringComparison.OrdinalIgnoreCase))
                return ResistantMultiplier;

            return NeutralMultiplier;
        }

        /// <summary>
        /// Retorna o elemento fraco de um defensor (o que é forte contra ele).
        /// </summary>
        public static string GetWeaknessOf(string defenderElement)
        {
            foreach (var kvp in StrongAgainst)
                if (string.Equals(kvp.Value, defenderElement, System.StringComparison.OrdinalIgnoreCase))
                    return kvp.Key;
            return "Nenhum";
        }

        public static string GetEffectivenessLabel(float multiplier) => multiplier switch
        {
            > 1f => "SUPER EFETIVO!",
            < 1f => "Pouco efetivo...",
            _    => "Normal."
        };
    }
}
