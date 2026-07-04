// Assets/_QueryQuest/Combat/ManaSystem.cs
// Sistema de mana arcana. Cada combate tem mana fixa.
// O CUSTO de lançar uma magia depende da ESPECIFICIDADE da query:
//   - Query genérica (SELECT * FROM Magias)      → custo ALTO (desperdício)
//   - Query específica (WHERE + AND + ORDER BY)  → custo BAIXO (eficiência)
//
// Lore: descrever a magia com precisão canaliza a energia com eficiência.
// Pedagogia: recompensa o domínio de SQL com economia de recurso.

using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace QueryQuest.Combat
{
    public class ManaSystem : MonoBehaviour
    {
        public static ManaSystem Instance { get; private set; }

        [Header("Configuração")]
        [SerializeField] private int maxMana = 100;

        public int MaxMana => maxMana;
        public int CurrentMana { get; private set; }

        // Custo base de uma magia sem nenhum refinamento (SELECT * FROM Magias)
        private const int BASE_COST = 45;

        // Descontos por tipo de cláusula (especificar = economizar)
        private const int DISCOUNT_WHERE    = 10; // + (usar WHERE)
        private const int DISCOUNT_AND      = 8;  // ++ por cada AND adicional
        private const int DISCOUNT_ORDERBY  = 12; // +++ (ORDER BY)
        private const int DISCOUNT_LIMIT    = 6;  // refinamento com LIMIT
        private const int DISCOUNT_OPERATOR = 5;  // operadores de comparação (>, <, >=, etc)

        private const int MIN_COST = 8; // custo mínimo (nunca é grátis)

        public event Action<int, int> OnManaChanged; // current, max

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ─────────────────────────────────────────────────────────────────────
        // CONTROLE DE MANA
        // ─────────────────────────────────────────────────────────────────────

        public void ResetMana()
        {
            CurrentMana = maxMana;
            OnManaChanged?.Invoke(CurrentMana, maxMana);
        }

        public bool HasMana(int cost) => CurrentMana >= cost;

        public void SpendMana(int cost)
        {
            CurrentMana = Mathf.Max(0, CurrentMana - cost);
            OnManaChanged?.Invoke(CurrentMana, maxMana);
        }

        // ─────────────────────────────────────────────────────────────────────
        // CÁLCULO DE CUSTO POR ESPECIFICIDADE
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Calcula o custo de mana de uma query. Quanto mais específica
        /// (mais cláusulas SQL), menor o custo.
        /// </summary>
        public int CalculateCost(string rawQuery)
        {
            if (string.IsNullOrWhiteSpace(rawQuery)) return BASE_COST;

            string q = rawQuery.ToUpper();
            int cost = BASE_COST;
            var breakdown = new System.Text.StringBuilder();

            // WHERE presente → desconto
            if (Regex.IsMatch(q, @"\bWHERE\b"))
            {
                cost -= DISCOUNT_WHERE;

                // Cada AND adicional → mais desconto
                int ands = Regex.Matches(q, @"\bAND\b").Count;
                cost -= ands * DISCOUNT_AND;

                // OR conta como refinamento leve também
                int ors = Regex.Matches(q, @"\bOR\b").Count;
                cost -= ors * DISCOUNT_AND;

                // Operadores de comparação (>, <, >=, <=, !=)
                int operators = Regex.Matches(q, @"(>=|<=|!=|>|<)").Count;
                cost -= operators * DISCOUNT_OPERATOR;
            }

            // ORDER BY → desconto forte (cláusula avançada)
            if (Regex.IsMatch(q, @"\bORDER\s+BY\b"))
                cost -= DISCOUNT_ORDERBY;

            // LIMIT → refinamento
            if (Regex.IsMatch(q, @"\bLIMIT\b"))
                cost -= DISCOUNT_LIMIT;

            return Mathf.Max(MIN_COST, cost);
        }

        /// <summary>Gera um texto explicando o custo (para o log educacional).</summary>
        public string ExplainCost(string rawQuery)
        {
            int cost = CalculateCost(rawQuery);
            string q = rawQuery.ToUpper();

            var tags = new System.Collections.Generic.List<string>();
            if (Regex.IsMatch(q, @"\bWHERE\b"))      tags.Add("WHERE");
            int ands = Regex.Matches(q, @"\bAND\b").Count;
            if (ands > 0)                             tags.Add($"{ands}x AND");
            if (Regex.IsMatch(q, @"\bORDER\s+BY\b")) tags.Add("ORDER BY");
            if (Regex.IsMatch(q, @"\bLIMIT\b"))      tags.Add("LIMIT");

            string refinements = tags.Count > 0
                ? $"Refinamentos: {string.Join(" + ", tags)}"
                : "Sem refinamentos (query genérica = custo máximo!)";

            return $"Custo: {cost} mana. {refinements}";
        }
    }
}
