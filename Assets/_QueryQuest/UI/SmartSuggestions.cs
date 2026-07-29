// Assets/_QueryQuest/UI/SmartSuggestions.cs
// "Lista Inteligente" — autocomplete contextual para o campo de query SQL.
// Analisa o que o jogador digitou e sugere a próxima palavra:
//   - Depois de SELECT  → colunas (ou *)
//   - Depois de FROM    → tabelas
//   - Depois de WHERE   → colunas
//   - Depois de coluna= → (sem sugestão de valores livres, ex: Nome)
//
// Hierarquia esperada:
//
// PanelQuery
// └── SuggestionsBar  (este script)  [HorizontalLayoutGroup ou VerticalLayoutGroup]
//     └── (botões de sugestão criados por código)
//
// Conecte o QueryInputField no campo abaixo.

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QueryQuest.Database;
using QueryQuest.Models;

namespace QueryQuest.UI
{
    public class SmartSuggestions : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private TMP_InputField queryInput;
        [SerializeField] private Transform suggestionsContainer;

        [Header("Aparência dos botões")]
        [SerializeField] private Color buttonColor = new Color(0.16f, 0.16f, 0.26f);
        [SerializeField] private Color textColor   = new Color(0.72f, 0.68f, 0.92f);
        [SerializeField] private float fontSize    = 13f;
        [SerializeField] private int maxSuggestions = 8;

        // Esquema do banco (mesmas colunas do SQLInterpreter)
        private static readonly string[] Tables = { "Magias", "Inimigos" };

        private static readonly Dictionary<string, string[]> Columns = new()
        {
            ["Magias"]   = new[] { "Nome", "Elemento", "Nivel", "Distancia", "DanoBase", "Desbloqueado" },
            ["Inimigos"] = new[] { "Nome", "Elemento", "HP", "Nivel", "FraquezaElemento", "AtaqueDistancia", "FraquezaDistancia" },
        };

        // Valores comuns para sugestão após "="
        private static readonly Dictionary<string, string[]> ColumnValues = new()
        {
            ["Elemento"]        = new[] { "'Fogo'", "'Agua'", "'Vento'", "'Terra'", "'Raio'" },
            ["Distancia"]       = new[] { "'CURTO'", "'MEDIO'", "'LONGO'" },
            ["AtaqueDistancia"] = new[] { "'CURTO'", "'MEDIO'", "'LONGO'" },
            ["FraquezaElemento"]= new[] { "'Fogo'", "'Agua'", "'Vento'", "'Terra'", "'Raio'" },
            ["Desbloqueado"]    = new[] { "0", "1" },
            ["Nivel"]           = new[] { "1", "2", "3" },
        };

        private readonly List<GameObject> _buttons = new();

        // ─────────────────────────────────────────────────────────────────────
        // UNITY
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (queryInput != null)
                queryInput.onValueChanged.AddListener(OnInputChanged);
        }

        private void OnDestroy()
        {
            if (queryInput != null)
                queryInput.onValueChanged.RemoveListener(OnInputChanged);
        }

        private void Start()
        {
            RefreshSuggestions(queryInput != null ? queryInput.text : "");
        }

        // ─────────────────────────────────────────────────────────────────────
        // LÓGICA DE SUGESTÃO
        // ─────────────────────────────────────────────────────────────────────

        private void OnInputChanged(string text)
        {
            RefreshSuggestions(text);
        }

        private void RefreshSuggestions(string text)
        {
            ClearButtons();

            var suggestions = ComputeSuggestions(text ?? "");
            foreach (var s in suggestions.Take(maxSuggestions))
                CreateButton(s);
        }

        /// <summary>Decide o que sugerir baseado no texto atual.</summary>
        private List<string> ComputeSuggestions(string text)
        {
            string upper = text.ToUpper();
            string trimmed = text.TrimEnd();
            string upperTrim = trimmed.ToUpper();

            // Vazio ou começando → sugere SELECT
            if (string.IsNullOrWhiteSpace(text))
                return new List<string> { "SELECT" };

            // Detecta a tabela mencionada (para saber quais colunas sugerir)
            string activeTable = DetectTable(upper);

            // Terminou de digitar "SELECT " → sugere colunas + *
            if (Regex.IsMatch(upperTrim, @"\bSELECT$"))
            {
                var cols = new List<string> { "*" };
                // Sem tabela ainda: oferece colunas de todas as tabelas
                cols.AddRange(AllColumns());
                return cols;
            }

            // Está no meio do SELECT (antes do FROM) → sugere mais colunas ou FROM
            if (upper.Contains("SELECT") && !upper.Contains("FROM"))
            {
                // Se acabou de digitar uma vírgula, sugere colunas
                if (trimmed.EndsWith(","))
                    return AllColumns().ToList();

                // Senão, oferece FROM para avançar
                return new List<string> { "FROM", "," };
            }

            // Terminou de digitar "FROM " → sugere tabelas
            if (Regex.IsMatch(upperTrim, @"\bFROM$"))
                return Tables.ToList();

            // Tem FROM + tabela mas não tem WHERE → sugere WHERE
            if (upper.Contains("FROM") && !upper.Contains("WHERE"))
            {
                if (activeTable != null)
                    return new List<string> { "WHERE" };
                return Tables.ToList(); // ainda não escolheu tabela válida
            }

            // Terminou de digitar "WHERE " → sugere colunas da tabela ativa
            if (Regex.IsMatch(upperTrim, @"\bWHERE$"))
            {
                if (activeTable != null && Columns.ContainsKey(activeTable))
                    return Columns[activeTable].ToList();
                return AllColumns().ToList();
            }

            // Depois de WHERE, detecta se acabou de digitar uma coluna → sugere operador
            if (upper.Contains("WHERE"))
            {
                // Pega a última "palavra" após WHERE
                var whereMatch = Regex.Match(text, @"WHERE\s+(.+)$", RegexOptions.IgnoreCase);
                if (whereMatch.Success)
                {
                    string afterWhere = whereMatch.Groups[1].Value;

                    // Terminou com uma coluna conhecida → sugere operadores
                    string lastCol = GetLastColumn(afterWhere, activeTable);
                    if (lastCol != null && Regex.IsMatch(afterWhere.TrimEnd(), @"\w$"))
                    {
                        // Se a última palavra é exatamente uma coluna, sugere operadores
                        string lastWord = afterWhere.TrimEnd().Split(' ', ',').Last();
                        if (activeTable != null && Columns.ContainsKey(activeTable) &&
                            Columns[activeTable].Any(c => c.Equals(lastWord, System.StringComparison.OrdinalIgnoreCase)))
                        {
                            return new List<string> { "=", ">", "<", ">=", "<=", "!=" };
                        }
                    }

                    // Terminou com operador → sugere valores da coluna
                    var opMatch = Regex.Match(afterWhere, @"(\w+)\s*(=|>|<|>=|<=|!=)\s*$");
                    if (opMatch.Success)
                    {
                        string col = NormalizeCol(opMatch.Groups[1].Value, activeTable);

                        // Nome é string: sugere os nomes reais do banco, JÁ entre aspas
                        if (col == "Nome")
                            return GetNameSuggestions(activeTable);

                        if (ColumnValues.TryGetValue(col, out var vals))
                            return vals.ToList();
                    }

                    // Depois de um valor completo → sugere AND/OR/ORDER BY/LIMIT
                    if (Regex.IsMatch(afterWhere.TrimEnd(), @"('.*'|\d+)$"))
                        return new List<string> { "AND", "OR", "ORDER BY", "LIMIT" };
                }
            }

            // Depois de ORDER BY → sugere colunas, depois ASC/DESC
            if (Regex.IsMatch(upperTrim, @"ORDER\s+BY$"))
            {
                if (activeTable != null && Columns.ContainsKey(activeTable))
                    return Columns[activeTable].ToList();
                return AllColumns().ToList();
            }
            if (Regex.IsMatch(upper, @"ORDER\s+BY\s+\w+$"))
                return new List<string> { "DESC", "ASC", "LIMIT" };
            if (Regex.IsMatch(upper, @"(DESC|ASC)$"))
                return new List<string> { "LIMIT" };

            return new List<string>();
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS DE ESQUEMA
        // ─────────────────────────────────────────────────────────────────────

        // Cache dos nomes (a lista de magias/inimigos não muda durante o combate)
        private List<string> _cachedSpellNames;
        private List<string> _cachedEnemyNames;

        /// <summary>Nomes reais da tabela ativa, entre aspas simples (Nome é string!).</summary>
        private List<string> GetNameSuggestions(string activeTable)
        {
            var db = DatabaseManager.Instance != null ? DatabaseManager.Instance.DB : null;
            if (db == null) return new List<string>();

            if (activeTable == "Inimigos")
            {
                if (_cachedEnemyNames == null)
                    _cachedEnemyNames = db.Table<EnemyData>().ToList()
                        .Select(e => $"'{e.Nome}'").Distinct().ToList();
                return _cachedEnemyNames;
            }

            if (_cachedSpellNames == null)
                _cachedSpellNames = db.Table<SpellData>().ToList()
                    .Select(s => $"'{s.Nome}'").Distinct().ToList();
            return _cachedSpellNames;
        }

        private string DetectTable(string upperText)
        {
            foreach (var t in Tables)
                if (upperText.Contains(t.ToUpper()))
                    return t;
            return null;
        }

        private IEnumerable<string> AllColumns()
        {
            return Columns.Values.SelectMany(c => c).Distinct();
        }

        private string GetLastColumn(string afterWhere, string table)
        {
            if (table == null || !Columns.ContainsKey(table)) return null;
            var words = afterWhere.Split(' ', ',', '=', '>', '<', '!');
            for (int i = words.Length - 1; i >= 0; i--)
            {
                var w = words[i].Trim();
                if (Columns[table].Any(c => c.Equals(w, System.StringComparison.OrdinalIgnoreCase)))
                    return w;
            }
            return null;
        }

        private string NormalizeCol(string col, string table)
        {
            if (table != null && Columns.ContainsKey(table))
            {
                var found = Columns[table].FirstOrDefault(c => c.Equals(col, System.StringComparison.OrdinalIgnoreCase));
                if (found != null) return found;
            }
            // procura em todas
            foreach (var kvp in Columns)
            {
                var found = kvp.Value.FirstOrDefault(c => c.Equals(col, System.StringComparison.OrdinalIgnoreCase));
                if (found != null) return found;
            }
            return col;
        }

        // ─────────────────────────────────────────────────────────────────────
        // CRIAÇÃO DOS BOTÕES
        // ─────────────────────────────────────────────────────────────────────

        private void CreateButton(string suggestion)
        {
            if (suggestionsContainer == null) return;

            var go = new GameObject($"Sug_{suggestion}");
            go.transform.SetParent(suggestionsContainer, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80, 30);

            var img = go.AddComponent<Image>();
            img.color = buttonColor;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => ApplySuggestion(suggestion));

            // Texto
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(10, 0);
            textRT.offsetMax = new Vector2(-10, 0);

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = suggestion;
            tmp.fontSize = fontSize;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;

            // LayoutElement controla o tamanho do botão dentro do layout group.
            // A largura se adapta ao texto: ~11px por caractere + padding.
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = Mathf.Max(50f, suggestion.Length * 11f + 20f);
            le.preferredWidth = suggestion.Length * 11f + 20f;
            le.minHeight = 30f;
            le.preferredHeight = 30f;

            _buttons.Add(go);
        }

        private void ApplySuggestion(string suggestion)
        {
            if (queryInput == null) return;

            string current = queryInput.text;

            // Se termina com espaço ou vírgula, só concatena; senão adiciona espaço
            string separator = "";
            if (!string.IsNullOrEmpty(current) && !current.EndsWith(" ") && !current.EndsWith(","))
                separator = " ";

            // Vírgula gruda sem espaço antes
            if (suggestion == ",")
            {
                queryInput.text = current.TrimEnd() + ", ";
            }
            else
            {
                queryInput.text = current + separator + suggestion + " ";
            }

            // Devolve o foco ao input e posiciona o cursor no fim
            queryInput.caretPosition = queryInput.text.Length;
            queryInput.ActivateInputField();
        }

        private void ClearButtons()
        {
            foreach (var b in _buttons)
                Destroy(b);
            _buttons.Clear();
        }
    }
}
