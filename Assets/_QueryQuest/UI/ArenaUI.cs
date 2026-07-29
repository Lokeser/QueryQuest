// Assets/_QueryQuest/UI/ArenaUI.cs
// Hierarquia esperada:
//
// ArenaUI (este script)
// └── SlotsContainer  [HorizontalLayoutGroup]
//     ├── Slot1       [Image fundo] + SlotVisual (script interno)
//     │   ├── SlotLabel    [TMP] "1"
//     │   ├── PlayerToken  [Image verde — filho do slot]
//     │   └── EnemyToken   [Image vermelha — filho do slot]
//     ├── Slot2
//     ├── Slot3
//     ├── Slot4
//     ├── Slot5
//     └── Slot6

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QueryQuest.Combat;

namespace QueryQuest.UI
{
    public class ArenaUI : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private Transform slotsContainer;

        [Header("Prefab do Slot")]
        [SerializeField] private GameObject slotPrefab; // criado por código se null

        [Header("Cores")]
        [SerializeField] private Color slotNormal    = new Color(0.12f, 0.12f, 0.20f);
        [SerializeField] private Color slotHighlight = new Color(0.45f, 0.25f, 0.08f); // laranja escuro = alcance do feitiço
        [SerializeField] private Color slotTarget    = new Color(0.60f, 0.10f, 0.10f); // vermelho = slot atingido
        [SerializeField] private Color playerColor   = new Color(0.20f, 0.80f, 0.30f); // verde
        [SerializeField] private Color enemyColor    = new Color(0.85f, 0.20f, 0.20f); // vermelho
        [SerializeField] private Color tokenBg       = new Color(0.08f, 0.08f, 0.15f);

        /// <summary>
        /// A barra de slots passou a ter as 6 caixas DESENHADAS na arte: o fundo
        /// de cada slot fica invisível e o realce vira um véu translúcido por cima
        /// da caixa (senão o alcance da magia deixaria de aparecer).
        /// </summary>
        private bool _artSkin;

        public void UseArtSkin()
        {
            _artSkin = true;
            slotNormal    = new Color(0f, 0f, 0f, 0f);
            slotHighlight = new Color(0.95f, 0.62f, 0.15f, 0.45f);
            slotTarget    = new Color(0.90f, 0.15f, 0.12f, 0.55f);

            if (SlotSystem.Instance != null)
                Refresh(SlotSystem.Instance.PlayerSlot, SlotSystem.Instance.EnemySlot);
        }

        // Referências internas dos slots (índice 0 = slot 1)
        private Image[]           _slotImages  = new Image[6];
        private TextMeshProUGUI[] _slotLabels  = new TextMeshProUGUI[6];
        private Image[]           _playerTokens = new Image[6];
        private Image[]           _enemyTokens  = new Image[6];

        // ─────────────────────────────────────────────────────────────────────
        // UNITY
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            BuildSlots();
        }

        private void Start()
        {
            if (SlotSystem.Instance != null)
            {
                SlotSystem.Instance.OnPositionsChanged   += OnPositionsChanged;
                SlotSystem.Instance.OnSpellRangeHighlight += OnSpellRangeHighlight;
                Refresh(SlotSystem.Instance.PlayerSlot, SlotSystem.Instance.EnemySlot);
            }
        }

        private void OnDestroy()
        {
            if (SlotSystem.Instance != null)
            {
                SlotSystem.Instance.OnPositionsChanged    -= OnPositionsChanged;
                SlotSystem.Instance.OnSpellRangeHighlight -= OnSpellRangeHighlight;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // CONSTRUÇÃO DOS SLOTS
        // ─────────────────────────────────────────────────────────────────────

        private void BuildSlots()
        {
            if (slotsContainer == null)
            {
                Debug.LogError("[ArenaUI] SlotsContainer não configurado!");
                return;
            }

            // Limpa filhos existentes
            foreach (Transform child in slotsContainer)
                Destroy(child.gameObject);

            for (int i = 0; i < 6; i++)
            {
                int slotNumber = i + 1;

                // ── Slot raiz ────────────────────────────────────────────────
                var slotGO = new GameObject($"Slot{slotNumber}");
                slotGO.transform.SetParent(slotsContainer, false);

                var slotRT = slotGO.AddComponent<RectTransform>();
                slotRT.sizeDelta = new Vector2(90f, 120f);

                var slotImg = slotGO.AddComponent<Image>();
                slotImg.color = slotNormal;
                _slotImages[i] = slotImg;

                // ── Borda/outline visual ─────────────────────────────────────
                var outline = slotGO.AddComponent<Outline>();
                outline.effectColor = new Color(0.3f, 0.3f, 0.5f, 0.8f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);

                // ── Label do número ──────────────────────────────────────────
                var labelGO = new GameObject("SlotLabel");
                labelGO.transform.SetParent(slotGO.transform, false);

                var labelRT = labelGO.AddComponent<RectTransform>();
                labelRT.anchorMin = new Vector2(0, 0);
                labelRT.anchorMax = new Vector2(1, 0.25f);
                labelRT.offsetMin = Vector2.zero;
                labelRT.offsetMax = Vector2.zero;

                var label = labelGO.AddComponent<TextMeshProUGUI>();
                label.text = slotNumber.ToString();
                label.fontSize = 14;
                label.alignment = TextAlignmentOptions.Center;
                label.color = new Color(0.5f, 0.5f, 0.7f);
                _slotLabels[i] = label;

                // ── Token do Jogador (quadrado verde) ────────────────────────
                var playerGO = CreateToken(slotGO.transform, "PlayerToken", playerColor, new Vector2(0f, 0.35f), new Vector2(1f, 0.85f));
                _playerTokens[i] = playerGO;
                playerGO.gameObject.SetActive(false);

                // Letra P dentro
                AddTokenLabel(playerGO.gameObject, "P");

                // ── Token do Inimigo (quadrado vermelho) ─────────────────────
                var enemyGO = CreateToken(slotGO.transform, "EnemyToken", enemyColor, new Vector2(0f, 0.35f), new Vector2(1f, 0.85f));
                _enemyTokens[i] = enemyGO;
                enemyGO.gameObject.SetActive(false);

                // Letra E dentro
                AddTokenLabel(enemyGO.gameObject, "E");
            }
        }

        private Image CreateToken(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(8, 0);
            rt.offsetMax = new Vector2(-8, 0);

            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private void AddTokenLabel(GameObject tokenGO, string text)
        {
            var labelGO = new GameObject("TokenLabel");
            labelGO.transform.SetParent(tokenGO.transform, false);

            var rt = labelGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        // ─────────────────────────────────────────────────────────────────────
        // ATUALIZAÇÃO VISUAL
        // ─────────────────────────────────────────────────────────────────────

        private void OnPositionsChanged(int playerSlot, int enemySlot)
        {
            Refresh(playerSlot, enemySlot);
        }

        private void OnSpellRangeHighlight(System.Collections.Generic.List<int> targetSlots)
        {
            for (int i = 0; i < 6; i++)
            {
                int slot = i + 1;
                if (targetSlots == null || targetSlots.Count == 0)
                {
                    _slotImages[i].color = slotNormal;
                }
                else if (targetSlots.Contains(slot))
                {
                    bool hitsEnemy = SlotSystem.Instance != null && slot == SlotSystem.Instance.EnemySlot;
                    _slotImages[i].color = hitsEnemy ? slotTarget : slotHighlight;
                }
                else
                {
                    _slotImages[i].color = slotNormal;
                }
            }
        }

        private void Refresh(int playerSlot, int enemySlot)
        {
            for (int i = 0; i < 6; i++)
            {
                int slot = i + 1;
                bool isPlayerHere = slot == playerSlot;
                bool isEnemyHere  = slot == enemySlot;
                bool bothHere     = isPlayerHere && isEnemyHere;

                // Ajusta tokens — se ambos no mesmo slot, reduz tamanho de cada um
                SetTokenPosition(_playerTokens[i], isPlayerHere, bothHere, isLeft: true);
                SetTokenPosition(_enemyTokens[i],  isEnemyHere,  bothHere, isLeft: false);

                // Cor do slot
                _slotImages[i].color = slotNormal;
            }
        }

        private void SetTokenPosition(Image token, bool active, bool sharedSlot, bool isLeft)
        {
            token.gameObject.SetActive(active);
            if (!active) return;

            var rt = token.GetComponent<RectTransform>();

            // Com a arte nova, o ícone é centralizado na caixa desenhada e a faixa
            // de baixo fica livre para o número do slot.
            float y0 = _artSkin ? 0.22f : 0.35f;
            float y1 = _artSkin ? 0.96f : 0.85f;
            float m  = _artSkin ? 0f : 8f;

            // Bottom -30: desce a base do token dentro da caixa desenhada
            float bottom = _artSkin ? -30f : 0f;

            if (sharedSlot)
            {
                // Lado a lado quando compartilhando slot
                rt.anchorMin = isLeft ? new Vector2(0f, y0)   : new Vector2(0.5f, y0);
                rt.anchorMax = isLeft ? new Vector2(0.5f, y1) : new Vector2(1f, y1);
                rt.offsetMin = new Vector2(_artSkin ? 2f : 4f, bottom);
                rt.offsetMax = new Vector2(_artSkin ? -2f : -4f, 0);
            }
            else
            {
                // Ocupa a caixa inteira
                rt.anchorMin = new Vector2(_artSkin ? 0.08f : 0f, y0);
                rt.anchorMax = new Vector2(_artSkin ? 0.92f : 1f, y1);
                rt.offsetMin = new Vector2(m, bottom);
                rt.offsetMax = new Vector2(-m, 0);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // API PÚBLICA
        // ─────────────────────────────────────────────────────────────────────

        public void ClearHighlights()
        {
            OnSpellRangeHighlight(new System.Collections.Generic.List<int>());
        }
    }
}
