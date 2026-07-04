// Assets/_QueryQuest/UI/ArenaSceneView.cs
// Visualização GRANDE do cenário de combate (vista de lado).
// Sincronizada com a barra de slots do topo via o mesmo SlotSystem.
//
// Hierarquia esperada:
//
// ArenaSceneView (este script)  [Image = fundo do cenário]
// └── FloorContainer            [HorizontalLayoutGroup] — os 6 slots de chão
//     ├── Floor1 ... Floor6     (criados por código)
//
// Os personagens (tokens grandes) são criados por código e posicionados
// sobre o slot correspondente.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QueryQuest.Combat;

namespace QueryQuest.UI
{
    public class ArenaSceneView : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private Transform floorContainer;

        [Header("Cores do Chão")]
        [SerializeField] private Color floorNormal    = new Color(0.10f, 0.10f, 0.16f, 0.6f);
        [SerializeField] private Color floorHighlight = new Color(0.45f, 0.25f, 0.08f, 0.8f); // alcance
        [SerializeField] private Color floorTarget    = new Color(0.60f, 0.10f, 0.10f, 0.85f); // acerta inimigo
        [SerializeField] private Color floorLineColor = new Color(0.30f, 0.30f, 0.50f, 0.5f);

        [Header("Personagens")]
        [SerializeField] private Color playerColor = new Color(0.20f, 0.80f, 0.30f);
        [SerializeField] private Color enemyColor  = new Color(0.85f, 0.20f, 0.20f);
        [SerializeField] private float tokenSize   = 80f;

        [Header("Sprites (opcional — deixe vazio para usar placeholder colorido)")]
        [Tooltip("Sprite do herói. Se vazio, usa o quadrado verde.")]
        [SerializeField] private Sprite playerSprite;
        [Tooltip("Sprite do inimigo. Se vazio, usa o quadrado vermelho.")]
        [SerializeField] private Sprite enemySprite;
        [Tooltip("Tamanho do sprite do herói quando atribuído")]
        [SerializeField] private Vector2 playerSpriteSize = new Vector2(140f, 180f);
        [Tooltip("Tamanho do sprite do inimigo quando atribuído")]
        [SerializeField] private Vector2 enemySpriteSize = new Vector2(140f, 180f);

        private Image[] _floorSlots = new Image[6];
        private RectTransform[] _floorRects = new RectTransform[6];

        private RectTransform _playerToken;
        private RectTransform _enemyToken;
        private Image _playerTokenImg;
        private Image _enemyTokenImg;

        // ─────────────────────────────────────────────────────────────────────
        // UNITY
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            BuildFloor();
            BuildCharacters();
        }

        private void Start()
        {
            if (SlotSystem.Instance != null)
            {
                SlotSystem.Instance.OnPositionsChanged    += OnPositionsChanged;
                SlotSystem.Instance.OnSpellRangeHighlight += OnSpellRangeHighlight;
                UpdatePositions(SlotSystem.Instance.PlayerSlot, SlotSystem.Instance.EnemySlot);
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
        // CONSTRUÇÃO DO CHÃO (6 SLOTS)
        // ─────────────────────────────────────────────────────────────────────

        [Header("Estilo do Cenário")]
        [Tooltip("Se true, os slots do chão ficam discretos (só marcadores de posição, não quadrados)")]
        [SerializeField] private bool subtleFloor = true;
        [SerializeField] private bool showSlotNumbers = false;

        private void BuildFloor()
        {
            if (floorContainer == null)
            {
                Debug.LogError("[ArenaSceneView] FloorContainer não configurado!");
                return;
            }

            foreach (Transform child in floorContainer)
                Destroy(child.gameObject);

            for (int i = 0; i < 6; i++)
            {
                int slotNumber = i + 1;

                var slotGO = new GameObject($"Floor{slotNumber}");
                slotGO.transform.SetParent(floorContainer, false);

                var rt = slotGO.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(120f, 70f);
                _floorRects[i] = rt;

                var img = slotGO.AddComponent<Image>();
                _slotSlots_SetColor(img, i);

                if (subtleFloor)
                {
                    // Cenário: apenas uma faixa fina no "chão" (base do slot), discreta.
                    // Sem borda de quadrado, para não parecer a barra do topo.
                    img.color = new Color(floorNormal.r, floorNormal.g, floorNormal.b, 0.15f);
                }
                else
                {
                    // Modo antigo: quadrado com borda
                    var outline = slotGO.AddComponent<Outline>();
                    outline.effectColor = floorLineColor;
                    outline.effectDistance = new Vector2(2f, -2f);
                }

                // Número do slot (opcional no cenário)
                if (showSlotNumbers)
                {
                    var labelGO = new GameObject("FloorLabel");
                    labelGO.transform.SetParent(slotGO.transform, false);
                    var labelRT = labelGO.AddComponent<RectTransform>();
                    labelRT.anchorMin = new Vector2(0, 0);
                    labelRT.anchorMax = new Vector2(1, 0.3f);
                    labelRT.offsetMin = Vector2.zero;
                    labelRT.offsetMax = Vector2.zero;

                    var label = labelGO.AddComponent<TextMeshProUGUI>();
                    label.text = slotNumber.ToString();
                    label.fontSize = 12;
                    label.alignment = TextAlignmentOptions.Center;
                    label.color = new Color(0.5f, 0.5f, 0.7f, 0.4f);
                }
            }
        }

        private void _slotSlots_SetColor(Image img, int i)
        {
            _floorSlots[i] = img;
        }

        // ─────────────────────────────────────────────────────────────────────
        // CONSTRUÇÃO DOS PERSONAGENS (TOKENS GRANDES)
        // ─────────────────────────────────────────────────────────────────────

        private void BuildCharacters()
        {
            _playerToken = CreateCharacter("PlayerCharacter", playerColor, "P", playerSprite, playerSpriteSize, out _playerTokenImg);
            _enemyToken  = CreateCharacter("EnemyCharacter",  enemyColor,  "E", enemySprite,  enemySpriteSize,  out _enemyTokenImg);
        }

        private RectTransform CreateCharacter(string name, Color color, string letter, Sprite sprite, Vector2 spriteSize, out Image imgOut)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false); // filho do ArenaSceneView (não do floor)

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0f); // ancora pela base (fica "em pé" no chão)

            var img = go.AddComponent<Image>();
            imgOut = img;

            bool hasSprite = sprite != null;
            if (hasSprite)
            {
                // Modo sprite: usa a imagem, tamanho definido, sem tint
                img.sprite = sprite;
                img.color = Color.white;
                img.preserveAspect = true;
                rt.sizeDelta = spriteSize;
            }
            else
            {
                // Modo placeholder: quadrado colorido com a letra
                img.color = color;
                rt.sizeDelta = new Vector2(tokenSize, tokenSize);
            }

            // Letra dentro (só aparece no modo placeholder)
            var labelGO = new GameObject("CharLabel");
            labelGO.transform.SetParent(go.transform, false);
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = hasSprite ? "" : letter;
            label.fontSize = 28;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;

            return rt;
        }

        // ─────────────────────────────────────────────────────────────────────
        // ATUALIZAÇÃO DE POSIÇÕES
        // ─────────────────────────────────────────────────────────────────────

        private void OnPositionsChanged(int playerSlot, int enemySlot)
        {
            UpdatePositions(playerSlot, enemySlot);
        }

        private void UpdatePositions(int playerSlot, int enemySlot)
        {
            PositionTokenOnSlot(_playerToken, playerSlot, isPlayer: true, sharesSlot: playerSlot == enemySlot);
            PositionTokenOnSlot(_enemyToken,  enemySlot,  isPlayer: false, sharesSlot: playerSlot == enemySlot);
        }

        private void PositionTokenOnSlot(RectTransform token, int slot, bool isPlayer, bool sharesSlot)
        {
            if (token == null) return;
            int idx = Mathf.Clamp(slot - 1, 0, 5);
            var floorRect = _floorRects[idx];
            if (floorRect == null) return;

            // Converte a posição do slot de chão para coordenadas locais do ArenaSceneView
            Vector3 worldPos = floorRect.position;
            RectTransform selfRect = transform as RectTransform;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                selfRect,
                RectTransformUtility.WorldToScreenPoint(null, worldPos),
                null,
                out localPoint);

            // Desloca lateralmente se compartilham o slot (corpo a corpo)
            float offsetX = 0f;
            if (sharesSlot)
                offsetX = isPlayer ? -tokenSize * 0.35f : tokenSize * 0.35f;

            // Posiciona pela base do token na metade inferior do slot de chão
            token.localPosition = new Vector3(
                localPoint.x + offsetX,
                localPoint.y - floorRect.rect.height * 0.2f,
                0f);
        }

        // ─────────────────────────────────────────────────────────────────────
        // HIGHLIGHT DE ALCANCE DA MAGIA
        // ─────────────────────────────────────────────────────────────────────

        private void OnSpellRangeHighlight(System.Collections.Generic.List<int> targetSlots)
        {
            Color restColor = subtleFloor
                ? new Color(floorNormal.r, floorNormal.g, floorNormal.b, 0.15f)
                : floorNormal;

            for (int i = 0; i < 6; i++)
            {
                int slot = i + 1;
                if (targetSlots == null || targetSlots.Count == 0)
                {
                    _floorSlots[i].color = restColor;
                }
                else if (targetSlots.Contains(slot))
                {
                    bool hitsEnemy = SlotSystem.Instance != null && slot == SlotSystem.Instance.EnemySlot;
                    _floorSlots[i].color = hitsEnemy ? floorTarget : floorHighlight;
                }
                else
                {
                    _floorSlots[i].color = restColor;
                }
            }
        }
    }
}
