// Assets/_QueryQuest/UI/QueryTerminalUI.cs
// Hierarquia esperada em PanelQuery:
//
// PanelQuery  (este script)
// ├── LogScrollView
// │   └── Viewport
// │       └── LogContent          (RectTransform com VerticalLayoutGroup)
// ├── InputRow
// │   ├── QueryInputField         (TMP_InputField)
// │   └── ExecuteButton           (Button)
// └── HintText                    (TextMeshProUGUI)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QueryQuest.Combat;

namespace QueryQuest.UI
{
    public class QueryTerminalUI : MonoBehaviour
    {
        [Header("Log")]
        [SerializeField] private ScrollRect logScrollView;
        [SerializeField] private RectTransform logContent;
        [SerializeField] private GameObject logLinePrefab; // Prefab: GameObject com TextMeshProUGUI

        [Header("Input")]
        [SerializeField] private TMP_InputField queryInputField;
        [SerializeField] private Button executeButton;

        [Header("Cores do Log")]
        [SerializeField] private Color colorOk      = new Color(0.36f, 0.79f, 0.65f); // #5dcaa5 verde
        [SerializeField] private Color colorError   = new Color(0.94f, 0.58f, 0.58f); // #f09595 vermelho
        [SerializeField] private Color colorInfo    = new Color(0.66f, 0.62f, 0.85f); // #a89fd8 roxo
        [SerializeField] private Color colorWarning = new Color(0.94f, 0.62f, 0.15f); // #ef9f27 âmbar

        [Header("Limite de linhas no log")]
        [SerializeField] private int maxLogLines = 80;

        private readonly Queue<GameObject> _logLines = new();

        // ─────────────────────────────────────────────────────────────────────
        // UNITY
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            executeButton?.onClick.AddListener(OnExecuteClicked);

            // Enter no input field executa a query
            queryInputField?.onSubmit.AddListener(_ => OnExecuteClicked());
        }

        private void OnEnable()
        {
            // Mensagem de boas-vindas ao abrir
            if (_logLines.Count == 0)
            {
                AppendLog("[GRIMORIO.SQL] Sistema inicializado.", LogType.Info);
                AppendLog("[DICA] Use SELECT * FROM Feiticos para listar seus feitiços.", LogType.Info);
                AppendLog("[DICA] Filtre com WHERE Elemento = 'Fogo' para selecionar um feitiço.", LogType.Info);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // API PÚBLICA
        // ─────────────────────────────────────────────────────────────────────

        public void FocusInput()
        {
            queryInputField?.ActivateInputField();
            queryInputField?.Select();
        }

        /// <summary>Adiciona linha ao log com cor automática baseada no conteúdo.</summary>
        public void AppendLog(string message)
        {
            LogType type = DetectType(message);
            AppendLog(message, type);
        }

        public void AppendLog(string message, LogType type)
        {
            if (logLinePrefab == null || logContent == null) return;

            // Remove linhas antigas se ultrapassar o limite
            while (_logLines.Count >= maxLogLines)
            {
                var old = _logLines.Dequeue();
                Destroy(old);
            }

            var lineGO = Instantiate(logLinePrefab, logContent);
            var tmp = lineGO.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = message;
                tmp.color = type switch
                {
                    LogType.Ok      => colorOk,
                    LogType.Error   => colorError,
                    LogType.Warning => colorWarning,
                    _               => colorInfo,
                };
            }

            _logLines.Enqueue(lineGO);

            // Força scroll para o fundo no próximo frame
            Canvas.ForceUpdateCanvases();
            if (logScrollView != null)
                logScrollView.verticalNormalizedPosition = 0f;
        }

        public void ClearLog()
        {
            foreach (var line in _logLines) Destroy(line);
            _logLines.Clear();
        }

        // ─────────────────────────────────────────────────────────────────────
        // EXECUÇÃO
        // ─────────────────────────────────────────────────────────────────────

        private void OnExecuteClicked()
        {
            string query = queryInputField?.text?.Trim();
            if (string.IsNullOrEmpty(query)) return;

            AppendLog("> " + query, LogType.Info);

            if (CombatManager.Instance == null)
            {
                AppendLog("[ERRO] CombatManager não encontrado.", LogType.Error);
                return;
            }

            // Delega ao CombatManager — ele chama o SQLInterpreter
            var result = CombatManager.Instance.SubmitQuery(query);

            // O log do resultado já vem via OnCombatLog (inscrito no GrimoireUI/CombatManager)
            // mas exibimos aqui também para feedback imediato
            if (!result.Success)
            {
                AppendLog(result.FormattedLog, LogType.Error);
            }
            else
            {
                // Linhas do FormattedLog separadas por \n
                foreach (var line in result.FormattedLog.Split('\n'))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        AppendLog(line, DetectType(line));
                }
            }

            // Limpa o input após execução
            queryInputField.text = "";
            FocusInput();
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private LogType DetectType(string msg)
        {
            if (msg.Contains("ERROR") || msg.Contains("ERRO") || msg.Contains("não existe"))
                return LogType.Error;
            if (msg.Contains("VALIDATED") || msg.Contains("OUTPUT") || msg.Contains("⚡"))
                return LogType.Ok;
            if (msg.Contains("⚠") || msg.Contains("Nenhum") || msg.Contains("refine"))
                return LogType.Warning;
            return LogType.Info;
        }

        public enum LogType { Ok, Error, Info, Warning }
    }
}
