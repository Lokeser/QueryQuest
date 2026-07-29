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
using QueryQuest.Models;

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

        [Header("Protagonista Animado")]
        [Tooltip("Se true, cria o token do jogador com SpriteAnimator + PlayerAnimationController, carregando os sheets fatiados de Resources/Sprites/Player. Se os sprites não existirem, cai no placeholder.")]
        [SerializeField] private bool autoAnimatedPlayer = true;

        [Header("Golem Animado (inimigo)")]
        [Tooltip("Se true, cria o token do inimigo com SpriteAnimator + EnemyAnimationController, carregando a variante do golem pelo Elemento do inimigo do andar. Se os sprites não existirem, cai no placeholder.")]
        [SerializeField] private bool autoAnimatedEnemy = true;

        [Header("Cenário de Fundo (um por andar)")]
        [Tooltip("Troca a arte de fundo automaticamente conforme o andar (Resources/Sprites/Backgrounds/andar1..5).")]
        [SerializeField] private bool autoBackground = true;
        [Tooltip("Tint do fundo — abaixe um pouco para a HUD e os personagens destacarem.")]
        [SerializeField] private Color backgroundTint = new Color(0.88f, 0.88f, 0.92f, 1f);
        [Tooltip("Linha do chão da plataforma, medida do TOPO da imagem (0.5 = meio, 1 = base). Os pés dos personagens ficam aqui.")]
        [SerializeField, Range(0.5f, 1f)] private float groundLine = 0.76f;

        [Header("Espírito (lua)")]
        [Tooltip("Cria a lua alada flutuando atrás e acima do herói, com balão de dica no início do combate.")]
        [SerializeField] private bool autoSpirit = true;

        [Header("Escala dos Personagens")]
        [Tooltip("Multiplica o tamanho do herói (1 = tamanho original do sprite).")]
        [SerializeField] private float playerScale = 1.4f;
        [Tooltip("Multiplica o tamanho do golem (1 = tamanho original do sprite).")]
        [SerializeField] private float enemyScale = 2.4f;

        [Header("Tokens Externos (opcional — têm prioridade sobre tudo acima)")]
        [Tooltip("Se atribuído, usa este objeto (com SpriteAnimator) em vez de gerar o token por código. O objeto deve ser filho do ArenaSceneView.")]
        [SerializeField] private RectTransform externalPlayerToken;
        [Tooltip("Se atribuído, usa este objeto para o inimigo em vez de gerar um quadrado.")]
        [SerializeField] private RectTransform externalEnemyToken;

        private Image[] _floorSlots = new Image[6];
        private RectTransform[] _floorRects = new RectTransform[6];

        private RectTransform _playerToken;
        private RectTransform _enemyToken;
        private Image _playerTokenImg;
        private Image _enemyTokenImg;

        private SpiritCompanionView _spirit;
        private SpellVFXPlayer _vfx;

        // ─────────────────────────────────────────────────────────────────────
        // UNITY
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            ApplyBackground(FloorManager.Instance != null ? FloorManager.Instance.CurrentFloor : 1);
            BuildFloor();
            BuildCharacters();
            BuildSpirit();
            BuildVFXPlayer();
        }

        private void Start()
        {
            if (SlotSystem.Instance != null)
            {
                SlotSystem.Instance.OnPositionsChanged    += OnPositionsChanged;
                SlotSystem.Instance.OnSpellRangeHighlight += OnSpellRangeHighlight;
                UpdatePositions(SlotSystem.Instance.PlayerSlot, SlotSystem.Instance.EnemySlot);
            }

            if (FloorManager.Instance != null)
                FloorManager.Instance.OnFloorStarted += ApplyBackground;

            if (CombatManager.Instance != null)
                CombatManager.Instance.OnSpellCast += OnSpellCast;

            StartCoroutine(RepositionAfterLayout());
        }

        /// <summary>
        /// No primeiro frame os RectTransforms ainda não têm tamanho real, e a
        /// linha do chão depende da altura da arena — reposiciona quando o
        /// layout já resolveu.
        /// </summary>
        private System.Collections.IEnumerator RepositionAfterLayout()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (SlotSystem.Instance != null)
                UpdatePositions(SlotSystem.Instance.PlayerSlot, SlotSystem.Instance.EnemySlot);
        }

        private void OnDestroy()
        {
            if (SlotSystem.Instance != null)
            {
                SlotSystem.Instance.OnPositionsChanged    -= OnPositionsChanged;
                SlotSystem.Instance.OnSpellRangeHighlight -= OnSpellRangeHighlight;
            }

            if (FloorManager.Instance != null)
                FloorManager.Instance.OnFloorStarted -= ApplyBackground;

            if (CombatManager.Instance != null)
                CombatManager.Instance.OnSpellCast -= OnSpellCast;
        }

        // ─────────────────────────────────────────────────────────────────────
        // CENÁRIO DE FUNDO (UM POR ANDAR)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Troca a arte de fundo conforme o andar (1..5).</summary>
        private void ApplyBackground(int floor)
        {
            if (!autoBackground) return;

            int idx = Mathf.Clamp(floor, 1, 5);
            var sprite = Resources.Load<Sprite>($"Sprites/Backgrounds/andar{idx}");
            if (sprite == null) return;

            var img = GetComponent<Image>();
            if (img == null) img = gameObject.AddComponent<Image>();

            img.sprite = sprite;
            img.color = backgroundTint;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;   // o cenário preenche a área da arena
        }

        // ─────────────────────────────────────────────────────────────────────
        // ESPÍRITO (LUA) E VFX DAS MAGIAS
        // ─────────────────────────────────────────────────────────────────────

        private void BuildSpirit()
        {
            if (!autoSpirit) return;

            var go = new GameObject("SpiritCompanion");
            go.transform.SetParent(transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;

            _spirit = go.AddComponent<SpiritCompanionView>();
        }

        private void BuildVFXPlayer()
        {
            var go = new GameObject("SpellVFX");
            go.transform.SetParent(transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;

            _vfx = go.AddComponent<SpellVFXPlayer>();
        }

        private void OnSpellCast(SpellData spell, System.Collections.Generic.List<int> targetSlots, bool hits)
        {
            if (_vfx == null || spell == null || _playerToken == null || _enemyToken == null) return;

            // Sai da altura das mãos do herói
            Vector2 from = (Vector2)_playerToken.localPosition +
                           new Vector2(_playerToken.sizeDelta.x * 0.30f,
                                       _playerToken.sizeDelta.y * 0.55f);

            // Alvo: o inimigo se acertou; senão, o slot mais distante da área
            int targetSlot = hits && SlotSystem.Instance != null
                ? SlotSystem.Instance.EnemySlot
                : (targetSlots != null && targetSlots.Count > 0
                    ? targetSlots[targetSlots.Count - 1]
                    : SlotSystem.Instance?.EnemySlot ?? 6);

            Vector2 to = new Vector2(SlotCenterX(targetSlot),
                                     GroundY() + _enemyToken.sizeDelta.y * 0.45f);

            _vfx.Play(from, to, spell.Elemento, spell.Nivel);
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
            // Prioridade: token externo (Inspector) > protagonista animado (Resources) > placeholder
            if (externalPlayerToken != null)
                _playerToken = externalPlayerToken;
            else if (autoAnimatedPlayer && PlayerSpriteLibrary.HasSprites)
                _playerToken = CreateAnimatedPlayer();
            else
                _playerToken = CreateCharacter("PlayerCharacter", playerColor, "P", playerSprite, playerSpriteSize, playerScale, out _playerTokenImg);

            if (externalEnemyToken != null)
                _enemyToken = externalEnemyToken;
            else if (autoAnimatedEnemy && EnemySpriteLibrary.HasSprites)
                _enemyToken = CreateAnimatedEnemy();
            else
                _enemyToken = CreateCharacter("EnemyCharacter", enemyColor, "E", enemySprite, enemySpriteSize, enemyScale, out _enemyTokenImg);
        }

        /// <summary>
        /// Cria o token do inimigo com as animações do golem (idle/ataque/dano/colapso).
        /// O EnemyAnimationController troca a variante de cor a cada andar.
        /// </summary>
        private RectTransform CreateAnimatedEnemy()
        {
            var go = new GameObject("EnemyCharacter");
            go.transform.SetParent(transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0f); // ancora pela base (fica "em pé" no chão)
            rt.sizeDelta = enemySpriteSize * enemyScale;

            var img = go.AddComponent<Image>();
            img.color = Color.white;
            img.preserveAspect = true;
            img.raycastTarget = false;
            _enemyTokenImg = img;

            // As animações precisam existir ANTES do Start do SpriteAnimator (ele toca
            // a default logo de cara); o controller depois só troca a variante por andar.
            var animator = go.AddComponent<SpriteAnimator>();
            var enemy = CombatManager.Instance != null ? CombatManager.Instance.CurrentEnemy : null;
            animator.SetAnimations(EnemySpriteLibrary.LoadAnimations(enemy != null ? enemy.Elemento : null));

            go.AddComponent<EnemyAnimationController>();

            return rt;
        }

        /// <summary>
        /// Cria o token do jogador com as 5 animações (idle/grimorio/ataque/dano/andando)
        /// carregadas dos spritesheets fatiados — nenhuma configuração de Inspector necessária.
        /// </summary>
        private RectTransform CreateAnimatedPlayer()
        {
            var go = new GameObject("PlayerCharacter");
            go.transform.SetParent(transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0f); // ancora pela base (fica "em pé" no chão)
            rt.sizeDelta = playerSpriteSize * playerScale;

            var img = go.AddComponent<Image>();
            img.color = Color.white;
            img.preserveAspect = true;
            img.raycastTarget = false;
            _playerTokenImg = img;

            var animator = go.AddComponent<SpriteAnimator>();
            animator.SetAnimations(PlayerSpriteLibrary.LoadAnimations());
            go.AddComponent<PlayerAnimationController>();

            return rt;
        }

        private RectTransform CreateCharacter(string name, Color color, string letter, Sprite sprite, Vector2 spriteSize, float scale, out Image imgOut)
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
                rt.sizeDelta = spriteSize * scale;
            }
            else
            {
                // Modo placeholder: quadrado colorido com a letra
                img.color = color;
                rt.sizeDelta = new Vector2(tokenSize, tokenSize) * scale;
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

            // A lua acompanha o herói (atrás e acima dele)
            if (_spirit != null && _playerToken != null)
                _spirit.FollowPlayer(_playerToken.localPosition, _playerToken.sizeDelta.y);
        }

        /// <summary>
        /// Altura (local) do chão da plataforma desenhada no cenário.
        /// Os personagens ficam com os pés nessa linha, não na barra de slots.
        /// </summary>
        private float GroundY()
        {
            var selfRect = transform as RectTransform;
            if (selfRect == null) return 0f;
            return selfRect.rect.height * (0.5f - groundLine);
        }

        /// <summary>Centro horizontal (local) de um slot, lido da barra de chão.</summary>
        private float SlotCenterX(int slot)
        {
            int idx = Mathf.Clamp(slot - 1, 0, 5);
            var floorRect = _floorRects[idx];
            if (floorRect == null) return 0f;

            RectTransform selfRect = transform as RectTransform;
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                selfRect,
                RectTransformUtility.WorldToScreenPoint(null, floorRect.position),
                null,
                out localPoint);
            return localPoint.x;
        }

        private void PositionTokenOnSlot(RectTransform token, int slot, bool isPlayer, bool sharesSlot)
        {
            if (token == null) return;

            // Desloca lateralmente se compartilham o slot (corpo a corpo)
            float offsetX = 0f;
            if (sharesSlot)
                offsetX = isPlayer ? -tokenSize * 0.35f * playerScale : tokenSize * 0.35f * enemyScale;

            // X vem do slot; Y vem da plataforma do cenário (pivô do token = base)
            token.localPosition = new Vector3(SlotCenterX(slot) + offsetX, GroundY(), 0f);
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
