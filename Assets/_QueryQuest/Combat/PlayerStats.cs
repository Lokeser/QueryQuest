// Assets/_QueryQuest/Combat/PlayerStats.cs
// Guarda os status do jogador que persistem durante toda a run roguelike.
// Modificado pelos itens coletados entre andares.

using System.Collections.Generic;
using UnityEngine;

namespace QueryQuest.Combat
{
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        // ─── HP ───────────────────────────────────────────────────────────────
        public int MaxHP { get; private set; } = 100;

        // ─── Armadura: reduz dano recebido (percentual) ──────────────────────
        // Cada armadura coletada adiciona +12% de redução, até o teto de 60%.
        public float DamageReduction { get; private set; } = 0f;
        private const float ARMOR_PER_ITEM = 0.12f;
        private const float ARMOR_CAP = 0.60f;

        // ─── Cajado: bônus de dano por elemento (percentual) ─────────────────
        // Cada cajado de um elemento adiciona +25% de dano daquele elemento.
        private readonly Dictionary<string, float> _elementBonus =
            new(System.StringComparer.OrdinalIgnoreCase);
        private const float STAFF_BONUS = 0.25f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ─────────────────────────────────────────────────────────────────────
        // APLICAÇÃO DE ITENS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Adiciona uma peça de armadura (reduz dano recebido).</summary>
        public void AddArmor()
        {
            DamageReduction = Mathf.Min(ARMOR_CAP, DamageReduction + ARMOR_PER_ITEM);
            Debug.Log($"[PlayerStats] Armadura equipada. Redução de dano: {DamageReduction:P0}");
        }

        /// <summary>Adiciona um cajado elemental (aumenta dano daquele elemento).</summary>
        public void AddStaff(string element)
        {
            if (!_elementBonus.ContainsKey(element))
                _elementBonus[element] = 0f;
            _elementBonus[element] += STAFF_BONUS;
            Debug.Log($"[PlayerStats] Cajado de {element} equipado. Bônus: {_elementBonus[element]:P0}");
        }

        // ─────────────────────────────────────────────────────────────────────
        // CONSULTAS (usadas no cálculo de combate)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Multiplicador de dano para um elemento (1.0 = sem bônus).</summary>
        public float GetElementBonus(string element)
        {
            if (_elementBonus.TryGetValue(element, out float bonus))
                return 1f + bonus;
            return 1f;
        }

        /// <summary>Aplica a redução de armadura a um dano recebido.</summary>
        public int ApplyDamageReduction(int rawDamage)
        {
            int reduced = Mathf.RoundToInt(rawDamage * (1f - DamageReduction));
            return Mathf.Max(1, reduced); // sempre toma pelo menos 1
        }

        // ─────────────────────────────────────────────────────────────────────
        // RESET (nova run)
        // ─────────────────────────────────────────────────────────────────────

        public void ResetStats()
        {
            DamageReduction = 0f;
            _elementBonus.Clear();
            Debug.Log("[PlayerStats] Stats resetados para nova run.");
        }

        public string GetSummary()
        {
            var parts = new List<string>();
            if (DamageReduction > 0) parts.Add($"Armadura: -{DamageReduction:P0} dano");
            foreach (var kvp in _elementBonus)
                parts.Add($"Cajado {kvp.Key}: +{kvp.Value:P0}");
            return parts.Count > 0 ? string.Join(" | ", parts) : "Sem itens";
        }
    }
}
