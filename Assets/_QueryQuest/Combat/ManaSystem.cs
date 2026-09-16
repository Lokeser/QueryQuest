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

        // Custo base de uma magia sem nenhum refinamento (SELECT * FROM Magias).
        // Público porque a barra de custo do grimório usa isto como TETO: a barra
        // nasce cheia neste valor e esvazia conforme a consulta fica específica.
        public const int BASE_COST = 45;

        // ── Análise (FROM Inimigos) ──────────────────────────────────────────
        // Caminho de custo diferente: em vez de especificidade, paga-se por
        // coluna revelada. Fica aqui, e não no CombatManager, para que a barra de
        // custo e a cobrança de verdade não possam divergir.
        public const int CUSTO_POR_COLUNA = 8;
        public const int COLUNAS_DO_INIMIGO = 7; // Nome, Nivel, Elemento, HP, Fraqueza, AtaqueDistancia, FraquezaDistancia

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
        public int CalculateCost(string rawQuery) => CustoDaMagia(rawQuery);

        /// <summary>
        /// Mesma conta, sem depender da instância — a barra do grimório precisa
        /// prever o custo mesmo fora de combate.
        /// </summary>
        public static int CustoDaMagia(string rawQuery)
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

        // ─────────────────────────────────────────────────────────────────────
        // PREVISÃO (a barra de custo do grimório)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Custo previsto de uma consulta, com o teto para desenhar a barra.</summary>
        public readonly struct Previsao
        {
            public readonly int Custo;
            public readonly int Teto;
            public readonly bool EhAnalise;

            public Previsao(int custo, int teto, bool ehAnalise)
            {
                Custo = custo;
                Teto = Mathf.Max(1, teto);
                EhAnalise = ehAnalise;
            }

            /// <summary>Quanto da barra fica cheia: 1 = custo máximo, perto de 0 = consulta afiada.</summary>
            public float Fracao => Mathf.Clamp01((float)Custo / Teto);
        }

        /// <summary>Consulta à tabela Inimigos = análise, e a análise cobra por coluna.</summary>
        public static bool EhAnalise(string rawQuery) =>
            !string.IsNullOrEmpty(rawQuery) &&
            Regex.IsMatch(rawQuery, @"FROM\s+Inimigos", RegexOptions.IgnoreCase);

        /// <summary>Quantas colunas o SELECT pede ("*" = todas as do inimigo).</summary>
        public static int ColunasPedidas(string rawQuery)
        {
            var m = Regex.Match(rawQuery ?? "", @"SELECT\s+(.+?)\s+FROM", RegexOptions.IgnoreCase);
            string cols = m.Success ? m.Groups[1].Value.Trim() : "*";
            return cols == "*" ? COLUNAS_DO_INIMIGO : cols.Split(',').Length;
        }

        /// <summary>
        /// O que a consulta vai custar se for executada agora. Cobre os dois
        /// caminhos do combate: magia (custo por especificidade) e análise
        /// (custo por coluna revelada).
        /// </summary>
        public static Previsao Prever(string rawQuery)
        {
            if (EhAnalise(rawQuery))
                return new Previsao(ColunasPedidas(rawQuery) * CUSTO_POR_COLUNA,
                                    COLUNAS_DO_INIMIGO * CUSTO_POR_COLUNA, true);

            return new Previsao(CustoDaMagia(rawQuery), BASE_COST, false);
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
