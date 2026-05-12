// Assets/_QueryQuest/Models/QueryResult.cs
using System.Collections.Generic;

namespace QueryQuest.Models
{
    /// <summary>
    /// Retorno padronizado do SQLInterpreter para a UI do Grimório.
    /// </summary>
    public class QueryResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

        // Linhas de resultado (cada linha é um dict coluna->valor)
        public List<Dictionary<string, object>> Rows { get; set; } = new();

        // Feitiço selecionado para lançar (se a query retornou exatamente 1 feitiço)
        public SpellData SelectedSpell { get; set; }

        // Log formatado para exibir no terminal do Grimório
        public string FormattedLog { get; set; }

        public static QueryResult Error(string message) => new()
        {
            Success = false,
            ErrorMessage = message,
            FormattedLog = $"[QUERY STATUS: ERROR]\n[MSG: {message}]"
        };

        public static QueryResult Ok(string log, List<Dictionary<string, object>> rows = null, SpellData spell = null) => new()
        {
            Success = true,
            Rows = rows ?? new(),
            SelectedSpell = spell,
            FormattedLog = log
        };
    }
}
