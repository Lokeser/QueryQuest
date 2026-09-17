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
        [SerializeField] private Color colorOk = new Color(0.36f, 0.79f, 0.65f); // #5dcaa5 verde
        [SerializeField] private Color colorError = new Color(0.94f, 0.58f, 0.58f); // #f09595 vermelho
        [SerializeField] private Color colorInfo = new Color(0.66f, 0.62f, 0.85f); // #a89fd8 roxo
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

        private void Start()
        {
            // Tudo o que o combate relata durante a consulta — erro do
            // interpretador, mana insuficiente, magia bloqueada, dano causado —
            // passa por aqui. O registro de combate continua recebendo o mesmo
            // fluxo; este painel é a cópia que fica ao alcance de quem digitou.
            if (CombatManager.Instance != null)
                CombatManager.Instance.OnCombatLog += AppendLog;
        }

        private void OnDestroy()
        {
            if (CombatManager.Instance != null)
                CombatManager.Instance.OnCombatLog -= AppendLog;
        }

        private void OnEnable()
        {
            // Mensagem de boas-vindas ao abrir
            if (_logLines.Count == 0)
            {
                AppendLog("[GRIMORIO.SQL] Sistema inicializado.", LogType.Info);
                AppendLog("[DICA] Use SELECT * FROM Magias para listar seus feitiços.", LogType.Info);
                AppendLog("[DICA] Filtre com WHERE Elemento = 'Fogo' para selecionar um feitiço.", LogType.Info);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // API PÚBLICA
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Tinta escura no lugar das cores claras do tema antigo: o painel de
        /// retorno tem fundo de pergaminho, e sobre ele o verde e o roxo claros
        /// sumiam. O vermelho do erro é o tom mais saturado de propósito.
        /// </summary>
        public void UsarPaletaPergaminho()
        {
            colorInfo    = new Color(0.30f, 0.22f, 0.11f);
            colorOk      = new Color(0.10f, 0.38f, 0.15f);
            colorWarning = new Color(0.56f, 0.33f, 0.04f);
            colorError   = new Color(0.62f, 0.10f, 0.07f);

            foreach (var linha in _logLines)
            {
                if (linha == null) continue;
                var tmp = linha.GetComponent<TextMeshProUGUI>();
                if (tmp == null) continue;
                var t = DetectType(tmp.text);
                tmp.color = t switch
                {
                    LogType.Ok => colorOk,
                    LogType.Error => colorError,
                    LogType.Warning => colorWarning,
                    _ => colorInfo,
                };
            }
        }

        /// <summary>Respiro entre o texto e a moldura do pergaminho.</summary>
        private const int Margem = 14;

        /// <summary>
        /// Afasta as linhas das bordas do painel. A margem vai no layout do
        /// CONTEÚDO, e não no viewport, para a rolagem continuar percorrendo a
        /// caixa inteira.
        /// </summary>
        public void AjustarMargem()
        {
            if (logContent == null) return;

            var coluna = logContent.GetComponent<VerticalLayoutGroup>();
            if (coluna == null) return;

            coluna.padding = new RectOffset(Margem, Margem, 10, 10);
            coluna.spacing = Mathf.Max(coluna.spacing, 3f);

            // As linhas já criadas foram medidas na largura antiga
            foreach (var linha in _logLines)
            {
                if (linha == null) continue;
                var tmp = linha.GetComponent<TextMeshProUGUI>();
                if (tmp != null) DimensionarLinha(linha, tmp);
            }
        }

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
            if (logContent == null) return;

            // Remove linhas antigas se ultrapassar o limite
            while (_logLines.Count >= maxLogLines)
            {
                var old = _logLines.Dequeue();
                Destroy(old);
            }

            // O prefab de linha nunca foi ligado na cena, e o retorno antigo era
            // desistir aqui — por isso a área abaixo do EXECUTAR ficava sempre
            // vazia. Sem prefab, a linha é montada por código.
            var lineGO = logLinePrefab != null
                ? Instantiate(logLinePrefab, logContent)
                : CriarLinha();

            var tmp = lineGO.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                bool erro = type == LogType.Error;
                // O erro é o que o jogador precisa achar de imediato: vai em
                // negrito, com uma seta que o separa do resto do histórico.
                tmp.text = erro ? $"<b>▸ {message}</b>" : message;
                tmp.color = type switch
                {
                    LogType.Ok => colorOk,
                    LogType.Error => colorError,
                    LogType.Warning => colorWarning,
                    _ => colorInfo,
                };
                tmp.fontStyle = erro ? FontStyles.Bold : FontStyles.Normal;
                DimensionarLinha(lineGO, tmp);
            }

            _logLines.Enqueue(lineGO);

            // Força scroll para o fundo no próximo frame
            Canvas.ForceUpdateCanvases();
            if (logScrollView != null)
                logScrollView.verticalNormalizedPosition = 0f;
        }

        /// <summary>Linha de retorno montada por código (não há prefab na cena).</summary>
        private GameObject CriarLinha()
        {
            var go = new GameObject("LinhaLog", typeof(RectTransform));
            go.transform.SetParent(logContent, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 14f;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.richText = true;
            tmp.raycastTarget = false;
            return go;
        }

        /// <summary>
        /// Altura da linha calculada à mão, como no registro de combate: dentro de
        /// um VerticalLayoutGroup o texto não dimensiona a si mesmo, e sem isso as
        /// linhas se sobrepõem.
        /// </summary>
        private void DimensionarLinha(GameObject linha, TextMeshProUGUI tmp)
        {
            // '??' não serve aqui: GetComponent devolve fake-null, que o operador
            // de coalescência considera preenchido.
            var le = linha.GetComponent<LayoutElement>();
            if (le == null) le = linha.AddComponent<LayoutElement>();

            // Desconta a margem dos dois lados, senão a altura é calculada para
            // uma largura maior que a real e a última linha fica cortada.
            float largura = logContent.rect.width - Margem * 2 - 4f;
            if (largura < 50f) largura = 300f;

            float altura = tmp.GetPreferredValues(tmp.text, largura, 0f).y;
            le.minHeight = le.preferredHeight = altura + 2f;
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

            // Delega ao CombatManager — ele chama o SQLInterpreter e relata o
            // resultado por OnCombatLog, que este painel assina no Start().
            // Registrar o FormattedLog aqui também duplicaria cada linha.
            CombatManager.Instance.SubmitQuery(query);

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