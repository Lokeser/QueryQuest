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
    ///   SELECT * FROM Magias
    ///   SELECT * FROM Magias WHERE Elemento = 'Fogo'
    ///   SELECT * FROM Magias WHERE Elemento = 'Fogo' AND Distancia = 'LONGO'
    ///   SELECT * FROM Magias WHERE Nivel >= 2
    ///   SELECT * FROM Inimigos WHERE FraquezaElemento = 'Agua'
    /// 
    /// Tabelas disponíveis: Magias | Inimigos
    /// </summary>
    public class SQLInterpreter
    {
        private readonly SQLiteConnection _db;

        // Colunas válidas por tabela (para feedback de erro preciso)
        private static readonly Dictionary<string, HashSet<string>> ValidColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Magias"] = new(StringComparer.OrdinalIgnoreCase)
                { "Id", "Nome", "Elemento", "Nivel", "Distancia", "DanoBase", "Descricao", "Desbloqueado" },
            ["Inimigos"] = new(StringComparer.OrdinalIgnoreCase)
                { "Id", "Nome", "Elemento", "HP", "Nivel", "FraquezaElemento", "AtaqueDistancia", "FraquezaDistancia", "Descricao" }
        };

        private static readonly HashSet<string> ValidTables = new(StringComparer.OrdinalIgnoreCase)
            { "Magias", "Inimigos" };

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
            // Regex agora captura WHERE, ORDER BY e LIMIT opcionais
            var match = Regex.Match(query,
                @"SELECT\s+(.+?)\s+FROM\s+(\w+)(?:\s+WHERE\s+(.+?))?(?:\s+ORDER\s+BY\s+(\w+)(?:\s+(ASC|DESC))?)?(?:\s+LIMIT\s+(\d+))?$",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!match.Success)
                return QueryResult.Error("Sintaxe inválida. Exemplo: SELECT * FROM Magias WHERE Elemento = 'Fogo'");

            string colsPart  = match.Groups[1].Value.Trim();
            string tableName = match.Groups[2].Value.Trim();
            string wherePart = match.Groups[3].Success ? match.Groups[3].Value.Trim() : null;
            string orderCol  = match.Groups[4].Success ? match.Groups[4].Value.Trim() : null;
            string orderDir  = match.Groups[5].Success ? match.Groups[5].Value.Trim().ToUpper() : "ASC";
            string limitPart = match.Groups[6].Success ? match.Groups[6].Value.Trim() : null;

            // Valida tabela
            if (!ValidTables.Contains(tableName))
                return QueryResult.Error($"Tabela '{tableName}' não existe. Tabelas disponíveis: Magias, Inimigos.");

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

            // Valida ORDER BY
            if (orderCol != null && !ValidColumns[tableName].Contains(orderCol))
                return QueryResult.Error($"Coluna '{orderCol}' (ORDER BY) não existe na tabela '{tableName}'.");

            // Valida e parseia WHERE
            List<WhereCondition> conditions = new();
            if (wherePart != null)
            {
                var (success, error, parsed) = ParseWhereInternal(wherePart, tableName);
                if (!success)
                    return QueryResult.Error(error);

                conditions = parsed;
            }

            int? limit = null;
            if (limitPart != null && int.TryParse(limitPart, out int lim))
                limit = lim;

            return ExecuteQuery(tableName, colsPart, conditions, orderCol, orderDir, limit);
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

                // O valor é TUDO que vem depois do operador (âncora no fim): assim
                // nada é ignorado em silêncio e nomes com apóstrofo, como
                // 'Jato d'Agua', não são cortados no meio.
                var condMatch = Regex.Match(token,
                    @"^(\w+)\s*(=|!=|>=|<=|>|<|LIKE)\s*(.+)$",
                    RegexOptions.IgnoreCase);

                if (!condMatch.Success)
                    return (false, $"Condição inválida: '{token}'. Exemplo: Elemento = 'Fogo'", null);

                string col = condMatch.Groups[1].Value.Trim();
                string op  = condMatch.Groups[2].Value.Trim().ToUpper();

                var (okValue, valueError, value) = ParseValue(condMatch.Groups[3].Value.Trim(), token);
                if (!okValue) return (false, valueError, null);

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

        /// <summary>
        /// Lê o valor de uma condição. Entre aspas, vale tudo que estiver entre a
        /// PRIMEIRA e a ÚLTIMA aspa — é o que faz 'Jato d'Agua' funcionar mesmo
        /// sem o jogador escapar o apóstrofo (o padrão SQL '' também é aceito).
        /// </summary>
        private (bool Success, string Error, string Value) ParseValue(string raw, string token)
        {
            if (string.IsNullOrEmpty(raw))
                return (false, $"Falta o valor em: '{token}'. Exemplo: Elemento = 'Fogo'", null);

            if (raw[0] == '\'')
            {
                if (raw.Length < 2 || raw[raw.Length - 1] != '\'')
                    return (false, $"Aspas não fechadas em: '{token}'.", null);

                return (true, null, raw.Substring(1, raw.Length - 2).Replace("''", "'"));
            }

            // Sem aspas: número ou palavra solta (ex.: Nivel = 2, Desbloqueado = 1)
            if (raw.IndexOf('\'') >= 0)
                return (false, $"Aspas fora de lugar em: '{token}'. Exemplo: Nome = 'Rajada'", null);

            return (true, null, raw);
        }

        // ─────────────────────────────────────────────────────────────────────
        // EXECUÇÃO NO SQLITE
        // ─────────────────────────────────────────────────────────────────────

        private QueryResult ExecuteQuery(string tableName, string colsPart, List<WhereCondition> conditions,
                                         string orderCol = null, string orderDir = "ASC", int? limit = null)
        {
            try
            {
                var (sqlQuery, args) = BuildSafeQuery(tableName, conditions, orderCol, orderDir, limit);

                var rows = new List<Dictionary<string, object>>();
                SpellData selectedSpell = null;

                if (tableName == "Magias")
                {
                    var results = _db.Query<SpellData>(sqlQuery, args);
                    foreach (var spell in results)
                        rows.Add(SpellToDict(spell));

                    // 1 resultado = magia escolhida com precisão.
                    // Múltiplos resultados = pega a PRIMEIRA (custa muita mana).
                    if (results.Count >= 1) selectedSpell = results[0];
                }
                else if (tableName == "Inimigos")
                {
                    var results = _db.Query<EnemyData>(sqlQuery, args);
                    foreach (var enemy in results)
                        rows.Add(EnemyToDict(enemy));
                }

                string log = BuildOutputLog(tableName, colsPart, rows, selectedSpell);
                var qr = QueryResult.Ok(log, rows, selectedSpell);
                qr.ResultCount = rows.Count;
                return qr;
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
        private (string sql, object[] args) BuildSafeQuery(string tableName, List<WhereCondition> conditions,
                                                           string orderCol = null, string orderDir = "ASC", int? limit = null)
        {
            var sb   = new StringBuilder($"SELECT * FROM {tableName}");
            var args = new List<object>();

            // WHERE
            if (conditions != null && conditions.Count > 0)
            {
                sb.Append(" WHERE ");
                for (int i = 0; i < conditions.Count; i++)
                {
                    var c = conditions[i];
                    if (i > 0) sb.Append($" {c.LogicalOp} ");

                    sb.Append(c.Operator == "LIKE"
                        ? $"{c.Column} LIKE ?"
                        : $"{c.Column} {c.Operator} ?");

                    if (int.TryParse(c.Value, out int intVal))
                        args.Add(intVal);
                    else
                        args.Add(c.Value);
                }
            }

            // ORDER BY (coluna já validada; direção só pode ser ASC/DESC)
            if (!string.IsNullOrEmpty(orderCol))
            {
                string dir = orderDir == "DESC" ? "DESC" : "ASC";
                sb.Append($" ORDER BY {orderCol} {dir}");
            }

            // LIMIT
            if (limit.HasValue && limit.Value > 0)
                sb.Append($" LIMIT {limit.Value}");

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
                if (tableName == "Magias")
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
            t.ToLower() switch { "magias" => "Magias", "inimigos" => "Inimigos", _ => t };

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
