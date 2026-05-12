// Assets/_QueryQuest/Database/Scripts/SQLInterpreter.cs
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using SQLite;
using UnityEngine;
using QueryQuest.Models;

namespace QueryQuest.Database
{
    /// <summary>
    /// Interpreta um subconjunto de SQL digitado pelo jogador.
    /// 
    /// Sintaxe suportada (v1):
    ///   SELECT * FROM Feiticos
    ///   SELECT * FROM Feiticos WHERE Elemento = 'Fogo'
    ///   SELECT * FROM Feiticos WHERE Elemento = 'Fogo' AND Distancia = 'LONGO'
    ///   SELECT * FROM Feiticos WHERE Nivel >= 2
    ///   SELECT * FROM Inimigos WHERE FraquezaElemento = 'Agua'
    /// 
    /// Tabelas disponíveis: Feiticos | Inimigos
    /// </summary>
    public class SQLInterpreter
    {
        private readonly SQLiteConnection _db;

        // Colunas válidas por tabela (para feedback de erro preciso)
        private static readonly Dictionary<string, HashSet<string>> ValidColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Feiticos"] = new(StringComparer.OrdinalIgnoreCase)
                { "Id", "Nome", "Elemento", "Nivel", "Distancia", "DanoBase", "Descricao", "Desbloqueado" },
            ["Inimigos"] = new(StringComparer.OrdinalIgnoreCase)
                { "Id", "Nome", "Elemento", "HP", "Nivel", "FraquezaElemento", "Descricao" }
        };

        private static readonly HashSet<string> ValidTables = new(StringComparer.OrdinalIgnoreCase)
            { "Feiticos", "Inimigos" };

        public SQLInterpreter(SQLiteConnection db)
        {
            _db = db;
        }

        /// <summary>
        /// Ponto de entrada principal. Recebe a string digitada pelo jogador, retorna QueryResult.
        /// </summary>
        public QueryResult Execute(string rawQuery)
        {
            if (string.IsNullOrWhiteSpace(rawQuery))
                return QueryResult.Error("Query vazia. Digite um comando SQL.");

            string query = rawQuery.Trim();

            if (!query.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                return QueryResult.Error("Apenas comandos SELECT são suportados neste grimório.");

            return ParseSelect(query);
        }

        // ─────────────────────────────────────────────────────────────────────
        // PARSE SELECT
        // ─────────────────────────────────────────────────────────────────────

        private QueryResult ParseSelect(string query)
        {
            var match = Regex.Match(query,
                @"SELECT\s+(.+?)\s+FROM\s+(\w+)(?:\s+WHERE\s+(.+))?$",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!match.Success)
                return QueryResult.Error("Sintaxe inválida. Exemplo: SELECT * FROM Feiticos WHERE Elemento = 'Fogo'");

            string colsPart  = match.Groups[1].Value.Trim();
            string tableName = match.Groups[2].Value.Trim();
            string wherePart = match.Groups[3].Success ? match.Groups[3].Value.Trim() : null;

            // Valida tabela
            if (!ValidTables.Contains(tableName))
                return QueryResult.Error($"Tabela '{tableName}' não existe. Tabelas disponíveis: Feiticos, Inimigos.");

            tableName = NormalizeTableName(tableName);

            // Valida colunas solicitadas (se não for *)
            if (colsPart != "*")
            {
                foreach (var col in colsPart.Split(','))
                {
                    string c = col.Trim();
                    if (!ValidColumns[tableName].Contains(c))
                        return QueryResult.Error($"Coluna '{c}' não existe na tabela '{tableName}'.");
                }
            }

            // Valida e parseia WHERE — chama ParseWhereInternal diretamente (sem hack de cast)
            List<WhereCondition> conditions = new();
            if (wherePart != null)
            {
                var (success, error, parsed) = ParseWhereInternal(wherePart, tableName);
                if (!success)
                    return QueryResult.Error(error);

                conditions = parsed;
            }

            return ExecuteQuery(tableName, colsPart, conditions);
        }

        // ─────────────────────────────────────────────────────────────────────
        // PARSE WHERE (AND / OR)
        // ─────────────────────────────────────────────────────────────────────

        private (bool Success, string Error, List<WhereCondition> Conditions) ParseWhereInternal(string wherePart, string tableName)
        {
            var conditions = new List<WhereCondition>();

            // Divide por AND / OR preservando os operadores lógicos
            var tokens     = Regex.Split(wherePart, @"\s+(AND|OR)\s+", RegexOptions.IgnoreCase);
            var logicalOps = Regex.Matches(wherePart, @"\s+(AND|OR)\s+", RegexOptions.IgnoreCase);

            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i].Trim();
                if (string.IsNullOrEmpty(token)) continue;

                // Regex.Split com grupo capturante inclui os separadores como tokens — ignorar
                if (Regex.IsMatch(token, @"^(AND|OR)$", RegexOptions.IgnoreCase)) continue;

                var condMatch = Regex.Match(token,
                    @"(\w+)\s*(=|!=|>=|<=|>|<|LIKE)\s*'?([^']*)'?",
                    RegexOptions.IgnoreCase);

                if (!condMatch.Success)
                    return (false, $"Condição inválida: '{token}'. Exemplo: Elemento = 'Fogo'", null);

                string col   = condMatch.Groups[1].Value.Trim();
                string op    = condMatch.Groups[2].Value.Trim().ToUpper();
                string value = condMatch.Groups[3].Value.Trim();

                if (!ValidColumns[tableName].Contains(col))
                    return (false,
                            $"Coluna '{col}' não existe na tabela '{tableName}'. " +
                            $"Colunas disponíveis: {string.Join(", ", ValidColumns[tableName])}",
                            null);

                // Operador lógico que precede ESTA condição (a partir da segunda)
                // logicalOps[i-1] = operador entre o token anterior e este
                string logicalOp = "AND";
                if (conditions.Count > 0 && logicalOps.Count >= conditions.Count)
                    logicalOp = logicalOps[conditions.Count - 1].Groups[1].Value.ToUpper();

                conditions.Add(new WhereCondition
                {
                    Column    = NormalizeColumnName(tableName, col),
                    Operator  = op,
                    Value     = value,
                    LogicalOp = logicalOp
                });
            }

            return (true, null, conditions);
        }

        // ─────────────────────────────────────────────────────────────────────
        // EXECUÇÃO NO SQLITE
        // ─────────────────────────────────────────────────────────────────────

        private QueryResult ExecuteQuery(string tableName, string colsPart, List<WhereCondition> conditions)
        {
            try
            {
                var (sqlQuery, args) = BuildSafeQuery(tableName, conditions);

                var rows = new List<Dictionary<string, object>>();
                SpellData selectedSpell = null;

                if (tableName == "Feiticos")
                {
                    var results = _db.Query<SpellData>(sqlQuery, args);
                    foreach (var spell in results)
                        rows.Add(SpellToDict(spell));

                    if (results.Count == 1) selectedSpell = results[0];
                }
                else if (tableName == "Inimigos")
                {
                    var results = _db.Query<EnemyData>(sqlQuery, args);
                    foreach (var enemy in results)
                        rows.Add(EnemyToDict(enemy));
                }

                string log = BuildOutputLog(tableName, colsPart, rows, selectedSpell);
                return QueryResult.Ok(log, rows, selectedSpell);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SQLInterpreter] Erro de execução: {e.Message}");
                return QueryResult.Error($"Erro interno ao executar query: {e.Message}");
            }
        }

        /// <summary>
        /// Monta a query SQL parametrizada a partir da lista de WhereCondition já validada.
        /// Nunca coloca input do jogador diretamente na string SQL.
        /// </summary>
        private (string sql, object[] args) BuildSafeQuery(string tableName, List<WhereCondition> conditions)
        {
            if (conditions == null || conditions.Count == 0)
                return ($"SELECT * FROM {tableName}", Array.Empty<object>());

            var sb   = new StringBuilder($"SELECT * FROM {tableName} WHERE ");
            var args = new List<object>();

            for (int i = 0; i < conditions.Count; i++)
            {
                var c = conditions[i];

                // O primeiro item não tem operador predecessor
                if (i > 0) sb.Append($" {c.LogicalOp} ");

                sb.Append(c.Operator == "LIKE"
                    ? $"{c.Column} LIKE ?"
                    : $"{c.Column} {c.Operator} ?");

                if (int.TryParse(c.Value, out int intVal))
                    args.Add(intVal);
                else
                    args.Add(c.Value);
            }

            return (sb.ToString(), args.ToArray());
        }

        // ─────────────────────────────────────────────────────────────────────
        // FORMATAÇÃO DO LOG DE SAÍDA
        // ─────────────────────────────────────────────────────────────────────

        private string BuildOutputLog(string tableName, string cols, List<Dictionary<string, object>> rows, SpellData spell)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[QUERY STATUS: VALIDATED]");
            sb.AppendLine($"[TABELA: {tableName.ToUpper()} | LINHAS RETORNADAS: {rows.Count}]");
            sb.AppendLine("────────────────────────────────");

            if (rows.Count == 0)
            {
                sb.AppendLine("[OUTPUT: Nenhum registro encontrado.]");
                return sb.ToString();
            }

            foreach (var row in rows)
            {
                if (tableName == "Feiticos")
                    sb.AppendLine($"[OUTPUT: Nome={row["Nome"]} | Elemento={row["Elemento"]} | " +
                                  $"Nível={row["Nivel"]} | Distância={row["Distancia"]} | Dano={row["DanoBase"]}]");
                else
                    sb.AppendLine($"[OUTPUT: Nome={row["Nome"]} | Elemento={row["Elemento"]} | " +
                                  $"HP={row["HP"]} | Fraqueza={row["FraquezaElemento"]}]");
            }

            if (spell != null)
                sb.AppendLine($"\n⚡ Feitiço selecionado: {spell.Nome} — pronto para lançar!");

            return sb.ToString();
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private Dictionary<string, object> SpellToDict(SpellData s) => new()
        {
            ["Id"] = s.Id, ["Nome"] = s.Nome, ["Elemento"] = s.Elemento,
            ["Nivel"] = s.Nivel, ["Distancia"] = s.Distancia, ["DanoBase"] = s.DanoBase,
            ["Descricao"] = s.Descricao, ["Desbloqueado"] = s.Desbloqueado
        };

        private Dictionary<string, object> EnemyToDict(EnemyData e) => new()
        {
            ["Id"] = e.Id, ["Nome"] = e.Nome, ["Elemento"] = e.Elemento,
            ["HP"] = e.HP, ["Nivel"] = e.Nivel, ["FraquezaElemento"] = e.FraquezaElemento,
            ["Descricao"] = e.Descricao
        };

        private static string NormalizeTableName(string t) =>
            t.ToLower() switch { "feiticos" => "Feiticos", "inimigos" => "Inimigos", _ => t };

        private static string NormalizeColumnName(string table, string col)
        {
            foreach (var c in ValidColumns[table])
                if (string.Equals(c, col, StringComparison.OrdinalIgnoreCase))
                    return c;
            return col;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ESTRUTURA AUXILIAR
    // ─────────────────────────────────────────────────────────────────────────

    public class WhereCondition
    {
        public string Column    { get; set; }
        public string Operator  { get; set; }
        public string Value     { get; set; }
        public string LogicalOp { get; set; } // AND | OR — operador que une esta condição à anterior
    }
}
