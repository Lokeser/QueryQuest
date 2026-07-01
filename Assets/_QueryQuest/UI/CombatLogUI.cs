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
                tmp.color = GetColorForMessage(message);
            }

            _lines.Add(lineGO);

            // Limita o número de linhas
            while (_lines.Count > maxLines)
            {
                var old = _lines[0];
                _lines.RemoveAt(0);
                Destroy(old);
            }

            // Auto-scroll para o fim
            Canvas.ForceUpdateCanvases();
            if (logScrollView != null)
                logScrollView.verticalNormalizedPosition = 0f;
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
