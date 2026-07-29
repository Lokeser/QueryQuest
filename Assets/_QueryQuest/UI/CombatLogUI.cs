// Assets/_QueryQuest/UI/CombatLogUI.cs
// Painel de log de combate no jogo (canto inferior direito).
// Escuta os eventos do CombatManager e mostra as mensagens na tela,
// como um "console" visível para o jogador.
//
// Hierarquia esperada:
//
// CombatLogPanel (este script)  [Image = fundo escuro]
// ├── Header                     [Image]
// │   └── TitleText              (TMP) "REGISTRO DE COMBATE"
// └── LogScrollView              [ScrollRect]
//     └── Viewport
//         └── LogContent         [VerticalLayoutGroup + ContentSizeFitter]
//
// LogLinePrefab: um TMP simples (reutiliza o mesmo prefab do grimório, ou cria um próprio)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QueryQuest.Combat;

namespace QueryQuest.UI
{
    public class CombatLogUI : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private ScrollRect logScrollView;
        [SerializeField] private RectTransform logContent;
        [SerializeField] private GameObject logLinePrefab;

        [Header("Configuração")]
        [SerializeField] private int maxLines = 50;
        [SerializeField] private float fontSize = 13f;

        [Header("Cores por tipo")]
        [SerializeField] private Color colorDefault = new Color(0.78f, 0.76f, 0.90f);
        [SerializeField] private Color colorPlayer  = new Color(0.40f, 0.85f, 0.50f); // verde - ações do jogador
        [SerializeField] private Color colorEnemy   = new Color(0.90f, 0.45f, 0.45f); // vermelho - ações do inimigo
        [SerializeField] private Color colorDamage  = new Color(0.95f, 0.70f, 0.30f); // laranja - dano
        [SerializeField] private Color colorSystem  = new Color(0.55f, 0.55f, 0.75f); // cinza - sistema
        [SerializeField] private Color colorVictory = new Color(0.45f, 0.90f, 0.55f);
        [SerializeField] private Color colorDefeat  = new Color(0.95f, 0.35f, 0.35f);

        private readonly List<GameObject> _lines = new();

        // ─────────────────────────────────────────────────────────────────────
        // UNITY
        // ─────────────────────────────────────────────────────────────────────

        private void Start()
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.OnCombatLog    += AppendLog;
                CombatManager.Instance.OnStateChanged += OnStateChanged;
            }

            // Escuta movimentos do jogador
            if (SlotSystem.Instance != null)
                SlotSystem.Instance.OnPositionsChanged += OnPositionsChanged;

            AppendLog("[SISTEMA] Registro de combate iniciado.");
        }

        private void OnDestroy()
        {
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.OnCombatLog    -= AppendLog;
                CombatManager.Instance.OnStateChanged -= OnStateChanged;
            }

            if (SlotSystem.Instance != null)
                SlotSystem.Instance.OnPositionsChanged -= OnPositionsChanged;
        }

        // ─────────────────────────────────────────────────────────────────────
        // RECEBIMENTO DE MENSAGENS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Conteúdo do log — o TextStyler pula esta subárvore (ver ScaleFont).</summary>
        public RectTransform ContentRoot => logContent;

        /// <summary>
        /// Paleta de tinta escura, para quando o painel tem fundo de pergaminho.
        /// As cores originais são claras (feitas para fundo preto) e sumiriam nele.
        /// </summary>
        public void ApplyParchmentPalette()
        {
            colorDefault = new Color(0.24f, 0.16f, 0.07f);
            colorPlayer  = new Color(0.11f, 0.35f, 0.14f);
            colorEnemy   = new Color(0.55f, 0.12f, 0.09f);
            colorDamage  = new Color(0.55f, 0.30f, 0.04f);
            colorSystem  = new Color(0.36f, 0.29f, 0.20f);
            colorVictory = new Color(0.10f, 0.40f, 0.16f);
            colorDefeat  = new Color(0.58f, 0.09f, 0.09f);

            foreach (var line in _lines)
            {
                if (line == null) continue;
                var tmp = line.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.color = GetColorForMessage(tmp.text);
            }
        }

        /// <summary>
        /// Escala a fonte do log. Precisa ser feito AQUI (e não por fora) porque a
        /// altura de cada linha é calculada manualmente a partir do fontSize —
        /// mexer no texto depois deixaria as linhas sobrepostas.
        /// </summary>
        public void ScaleFont(float factor)
        {
            fontSize *= factor;

            foreach (var line in _lines)
            {
                if (line == null) continue;
                var tmp = line.GetComponent<TextMeshProUGUI>();
                if (tmp == null) continue;

                tmp.fontSize = fontSize;
                tmp.fontStyle |= FontStyles.Bold;

                var fonte = QueryQuest.UI.TextStyler.Fonte;
                if (fonte != null) tmp.font = fonte;

                var le = line.GetComponent<LayoutElement>();
                if (le == null || logContent == null) continue;

                float availableWidth = logContent.rect.width - 16f;
                if (availableWidth < 50f) availableWidth = 300f;
                float h = tmp.GetPreferredValues(tmp.text, availableWidth, 0f).y;
                le.minHeight = h + 2f;
                le.preferredHeight = h + 2f;
            }
        }

        public void AppendLog(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            if (logLinePrefab == null || logContent == null) return;

            var lineGO = Instantiate(logLinePrefab, logContent);
            lineGO.SetActive(true);

            var tmp = lineGO.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = message;
                tmp.fontSize = fontSize;
                tmp.fontStyle |= FontStyles.Bold;
                tmp.color = GetColorForMessage(message);

                // A fonte entra ANTES do cálculo de altura (GetPreferredValues)
                var fonte = QueryQuest.UI.TextStyler.Fonte;
                if (fonte != null) tmp.font = fonte;

                // Garante que o texto quebra linha e não transborda
                tmp.textWrappingMode = TextWrappingModes.Normal;
                tmp.overflowMode = TextOverflowModes.Overflow;

                // Dimensiona a linha manualmente: calcula a altura que o texto
                // ocupa na largura disponível e aplica via LayoutElement.
                var le = lineGO.GetComponent<LayoutElement>();
                if (le == null) le = lineGO.AddComponent<LayoutElement>();

                float availableWidth = logContent.rect.width - 16f; // menos o padding
                if (availableWidth < 50f) availableWidth = 300f;     // fallback
                float preferredHeight = tmp.GetPreferredValues(message, availableWidth, 0f).y;
                le.minHeight = preferredHeight + 2f;
                le.preferredHeight = preferredHeight + 2f;
            }

            _lines.Add(lineGO);

            // Limita o número de linhas
            while (_lines.Count > maxLines)
            {
                var old = _lines[0];
                _lines.RemoveAt(0);
                Destroy(old);
            }

            // Auto-scroll robusto para o fim
            StopAllCoroutines();
            StartCoroutine(ScrollToBottomNextFrame());
        }

        private System.Collections.IEnumerator ScrollToBottomNextFrame()
        {
            yield return null; // espera 1 frame para o layout processar as novas linhas
            LayoutRebuilder.ForceRebuildLayoutImmediate(logContent);
            Canvas.ForceUpdateCanvases();

            if (logScrollView != null)
            {
                logScrollView.verticalNormalizedPosition = 0f;
                logScrollView.velocity = Vector2.zero;
            }
        }

        private Color GetColorForMessage(string msg)
        {
            // Define a cor baseada nas tags da mensagem
            if (msg.Contains("[VITORIA]"))   return colorVictory;
            if (msg.Contains("[DERROTA]") && msg.Contains("foi derrotado")) return colorVictory;
            if (msg.Contains("Você foi derrotado")) return colorDefeat;
            if (msg.Contains("[INIMIGO]"))   return colorEnemy;
            if (msg.Contains("[FEITICO]") || msg.Contains("[Magia]") || msg.Contains("[COMBATE]")) return colorPlayer;
            if (msg.Contains("[DANO]"))      return colorDamage;
            if (msg.Contains("[SISTEMA]") || msg.Contains("---")) return colorSystem;
            if (msg.Contains("[MOVIMENTO]")) return colorSystem;
            return colorDefault;
        }

        // ─────────────────────────────────────────────────────────────────────
        // EVENTOS DE MOVIMENTO
        // ─────────────────────────────────────────────────────────────────────

        private int _lastPlayerSlot = -1;
        private int _lastEnemySlot = -1;

        private void OnPositionsChanged(int playerSlot, int enemySlot)
        {
            // Detecta quem se moveu e em qual direção
            if (_lastPlayerSlot != -1 && playerSlot != _lastPlayerSlot)
            {
                string dir = playerSlot > _lastPlayerSlot ? "avançou" : "recuou";
                AppendLog($"[MOVIMENTO] Você {dir} para o slot {playerSlot}.");
            }

            if (_lastEnemySlot != -1 && enemySlot != _lastEnemySlot)
            {
                AppendLog($"[MOVIMENTO] Inimigo avançou para o slot {enemySlot}.");
            }

            _lastPlayerSlot = playerSlot;
            _lastEnemySlot = enemySlot;
        }

        // ─────────────────────────────────────────────────────────────────────
        // EVENTOS DE ESTADO
        // ─────────────────────────────────────────────────────────────────────

        private void OnStateChanged(CombatState state)
        {
            // Opcional: log discreto de mudança de turno
            // Deixado vazio para não poluir — o CombatManager já loga os turnos
        }
    }
}
