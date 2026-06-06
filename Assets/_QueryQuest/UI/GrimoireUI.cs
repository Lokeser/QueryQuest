// Assets/_QueryQuest/UI/GrimoireUI.cs
// Hierarquia esperada no Canvas:
//
// Canvas
// └── GrimoirePanel              (este script aqui)
//     ├── Header
//     │   └── TitleText          (TMP)
//     ├── TabBar
//     │   ├── TabQuery           (Button)
//     │   ├── TabMagias        (Button)
//     │   ├── TabArsenal         (Button)
//     │   └── TabDocs            (Button)
//     ├── ContentArea
//     │   ├── PanelQuery         (QueryTerminalUI aqui)
//     │   ├── PanelMagias      (MagiaListUI aqui)
//     │   ├── PanelArsenal       (ArsenalUI aqui)
//     │   └── PanelDocs          (DocsUI aqui)
//     └── StatusBar
//         └── StatusText         (TMP)

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QueryQuest.Combat;

namespace QueryQuest.UI
{
    public class GrimoireUI : MonoBehaviour
    {
        // ─── Referências ──────────────────────────────────────────────────────
        [Header("Panels")]
        [SerializeField] private GameObject panelQuery;
        [SerializeField] private GameObject panelMagias;
        [SerializeField] private GameObject panelArsenal;
        [SerializeField] private GameObject panelDocs;

        [Header("Tab Buttons")]
        [SerializeField] private Button tabQuery;
        [SerializeField] private Button tabMagias;
        [SerializeField] private Button tabArsenal;
        [SerializeField] private Button tabDocs;

        [Header("Tab Colors")]
        [SerializeField] private Color tabActiveColor = new Color(0.67f, 0.62f, 0.85f); // #a89fd8
        [SerializeField] private Color tabInactiveColor = new Color(0.42f, 0.41f, 0.56f); // #6b6890

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Animação")]
        [SerializeField] private float openDuration = 0.18f;
        [SerializeField] private float closeDuration = 0.14f;

        // ─── Sub-controllers ──────────────────────────────────────────────────
        private QueryTerminalUI _queryTerminal;
        private MagiaListUI _magiaList;
        private ArsenalUI _arsenal;

        private CanvasGroup _canvasGroup;
        private RectTransform _rect;

        public enum Tab { Query, Magias, Arsenal, Docs }
        private Tab _currentTab = Tab.Query;

        // ─────────────────────────────────────────────────────────────────────
        // UNITY
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _rect = GetComponent<RectTransform>();

            _queryTerminal = panelQuery?.GetComponent<QueryTerminalUI>();
            _magiaList = panelMagias?.GetComponent<MagiaListUI>();
            _arsenal = panelArsenal?.GetComponent<ArsenalUI>();

            // Registra listeners das abas
            tabQuery?.onClick.AddListener(() => SwitchTab(Tab.Query));
            tabMagias?.onClick.AddListener(() => SwitchTab(Tab.Magias));
            tabArsenal?.onClick.AddListener(() => SwitchTab(Tab.Arsenal));
            tabDocs?.onClick.AddListener(() => SwitchTab(Tab.Docs));

            // Começa invisível via CanvasGroup — NÃO desativa o GameObject
            // pois SetActive(false) impede o Start() e inscrição nos eventos
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            // gameObject.SetActive(false); <- REMOVIDO
        }

        private void Start()
        {
            if (CombatManager.Instance != null)
            {
                Debug.Log("[GrimoireUI] Inscrevendo no CombatManager. Estado atual: " + CombatManager.Instance.CurrentState);
                CombatManager.Instance.OnStateChanged += OnCombatStateChanged;

                // Se o combate já começou antes do Start() rodar, verifica o estado atual
                if (CombatManager.Instance.CurrentState == CombatState.GRIMOIRE_OPEN)
                {
                    Debug.Log("[GrimoireUI] Estado já é GRIMOIRE_OPEN, abrindo agora");
                    Open();
                }
            }
            else
            {
                Debug.LogError("[GrimoireUI] CombatManager.Instance é null no Start()!");
            }
        }

        private void OnDestroy()
        {
            if (CombatManager.Instance != null)
                CombatManager.Instance.OnStateChanged -= OnCombatStateChanged;
        }

        // ─────────────────────────────────────────────────────────────────────
        // ABRIR / FECHAR
        // ─────────────────────────────────────────────────────────────────────

        public void Open()
        {
            Debug.Log("[GrimoireUI] Open() chamado");
            gameObject.SetActive(true);
            Debug.Log("[GrimoireUI] SetActive(true) OK");
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            Debug.Log($"[GrimoireUI] CanvasGroup alpha={_canvasGroup.alpha} interactable={_canvasGroup.interactable}");
            SwitchTab(Tab.Query);
            _queryTerminal?.FocusInput();
            SetStatus("turno: jogador  ·  banco conectado");
            Debug.Log("[GrimoireUI] Open() concluido");
        }

        public void Close()
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            StopAllCoroutines();
            StartCoroutine(AnimateAlphaAndHide(1f, 0f, closeDuration));
        }

        public bool IsOpen => gameObject.activeSelf && _canvasGroup.alpha > 0.5f;

        // ─────────────────────────────────────────────────────────────────────
        // TROCA DE ABA
        // ─────────────────────────────────────────────────────────────────────

        public void SwitchTab(Tab tab)
        {
            _currentTab = tab;

            panelQuery?.SetActive(tab == Tab.Query);
            panelMagias?.SetActive(tab == Tab.Magias);
            panelArsenal?.SetActive(tab == Tab.Arsenal);
            panelDocs?.SetActive(tab == Tab.Docs);

            RefreshTabColors();

            // Recarrega dados das abas quando abertas
            if (tab == Tab.Magias) _magiaList?.Refresh();
            if (tab == Tab.Arsenal) _arsenal?.Refresh();
        }

        private void RefreshTabColors()
        {
            SetTabColor(tabQuery, _currentTab == Tab.Query);
            SetTabColor(tabMagias, _currentTab == Tab.Magias);
            SetTabColor(tabArsenal, _currentTab == Tab.Arsenal);
            SetTabColor(tabDocs, _currentTab == Tab.Docs);
        }

        private void SetTabColor(Button btn, bool active)
        {
            if (btn == null) return;
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.color = active ? tabActiveColor : tabInactiveColor;
        }

        // ─────────────────────────────────────────────────────────────────────
        // EVENTOS DO COMBATE
        // ─────────────────────────────────────────────────────────────────────

        private void OnCombatStateChanged(CombatState state)
        {
            // Nao reage se nenhum combate estiver ativo (ex: teste de UI)
            if (CombatManager.Instance?.CurrentEnemy == null) return;

            switch (state)
            {
                case CombatState.GRIMOIRE_OPEN:
                    Open();
                    break;

                case CombatState.QUERY_EXECUTING:
                    SetStatus("executando query...");
                    break;

                case CombatState.PLAYER_TURN:
                case CombatState.ENEMY_TURN:
                    if (IsOpen) Close();
                    break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────

        public void SetStatus(string msg)
        {
            if (statusText != null) statusText.text = msg;
        }

        public void AppendQueryLog(string msg)
        {
            _queryTerminal?.AppendLog(msg);
        }

        private IEnumerator AnimateAlpha(float from, float to, float duration)
        {
            float elapsed = 0f;
            _canvasGroup.alpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            _canvasGroup.alpha = to;
        }

        private IEnumerator AnimateAlphaAndHide(float from, float to, float duration)
        {
            yield return AnimateAlpha(from, to, duration);
            gameObject.SetActive(false);
        }
    }
}