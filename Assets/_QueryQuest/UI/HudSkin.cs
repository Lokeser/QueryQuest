// Assets/_QueryQuest/UI/HudSkin.cs
// Veste a HUD com as molduras de pergaminho/ouro (Resources/Sprites/UI).
// Roda sozinho ao carregar a cena — nada para ligar no Inspector.
//
// Dois modos, escolhidos pelo que o painel já tem:
//
//   SUBSTITUIR  — painéis sem fundo (status, barra de slots, barra de baixo):
//                 a moldura vira o fundo. Textos claros que ficariam sobre o
//                 pergaminho são passados para tinta escura.
//
//   MOLDURA     — painéis que JÁ têm fundo próprio (grimório, registro de
//                 combate): a moldura vira o fundo e o fundo antigo é
//                 recriado por dentro, com uma margem. O conteúdo continua
//                 sobre o mesmo fundo escuro de antes (zero risco de
//                 ilegibilidade) e a borda dourada aparece em volta.

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using QueryQuest.Combat;

namespace QueryQuest.UI
{
    public class HudSkin : MonoBehaviour
    {
        private static readonly Color Ink = new Color(0.24f, 0.15f, 0.06f);

        /// <summary>Fundo das telas cheias, num marrom próximo ao da HUD.</summary>
        private static readonly Color ScreenTint = new Color(0.10f, 0.06f, 0.03f, 0.90f);

        // ─────────────────────────────────────────────────────────────────────
        // A BARRA DE BAIXO (hud_baixo)
        //
        // A arte já vem com o arco central e as caixas dos botões DESENHADAS,
        // então ela nunca é esticada: entra na proporção original e cada botão
        // é colocado por fração da imagem, na caixa que lhe pertence.
        // Números medidos nos pixels da arte (1522x607, já sem o fundo):
        //   (x0, x1, yTopo0, yTopo1) — y contado a partir do TOPO da imagem.
        // ─────────────────────────────────────────────────────────────────────

        private const float ArteBaixoW = 1522f;
        private const float ArteBaixoH = 607f;

        private static readonly Vector4 SlotVoltar   = new Vector4(0.1886f, 0.3377f, 0.6755f, 0.8929f);
        private static readonly Vector4 SlotManter   = new Vector4(0.3568f, 0.5046f, 0.6771f, 0.9012f);
        private static readonly Vector4 SlotAvancar  = new Vector4(0.5230f, 0.6787f, 0.6738f, 0.8979f);
        private static readonly Vector4 SlotTurno    = new Vector4(0.7359f, 0.9501f, 0.6705f, 0.9012f);
        private static readonly Vector4 SlotGrimorio = new Vector4(0.4238f, 0.5644f, 0.1779f, 0.4975f);

        /// <summary>Proporção das placas recortadas, para encaixar sem deformar.</summary>
        private const float PlacaMovAspect   = 1452f / 662f;    // ~2.193
        private const float PlacaTurnoAspect = 1493f / 592f;    // ~2.522

        /// <summary>Tamanho que a barra recebeu nesta resolução.</summary>
        private Vector2 _barSize;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            // ATENÇÃO: este atributo roda UMA VEZ por execução, não a cada cena.
            // Como o jogo agora começa pelo menu, é preciso recriar o skin a cada
            // cena carregada — senão a HUD do combate nunca seria vestida.
            Criar();
            SceneManager.sceneLoaded -= AoCarregarCena;
            SceneManager.sceneLoaded += AoCarregarCena;
        }

        private static void AoCarregarCena(Scene cena, LoadSceneMode modo) => Criar();

        private static void Criar()
        {
            var go = new GameObject("~HudSkin");
            go.AddComponent<HudSkin>();
        }

        private IEnumerator Start()
        {
            // Espera o layout resolver os tamanhos (o 9-slice depende deles)
            yield return null;
            Canvas.ForceUpdateCanvases();
            Apply();
        }

        private void Apply()
        {
            // O cenário virou uma imagem OPACA e é irmão posterior da HUD da arena,
            // então estava desenhando por cima dos slots e da barra de baixo.
            var arenaScene = Find("ArenaScene");
            if (arenaScene != null)
            {
                arenaScene.SetAsFirstSibling();
                // A cena vinha deslocada 40px para cima, deixando uma faixa sem
                // cenário no rodapé — aqui ela passa a cobrir a tela inteira.
                Stretch(arenaScene, 0f, 0f, 0f, 0f);
            }

            LayoutStatusBars();
            LayoutSlotsBar();
            LayoutBottomBar();
            LayoutHintText();

            // Painéis sem fundo próprio → a moldura vira o fundo
            // Estes três têm elementos POSICIONADOS por fração da arte (barras e
            // caixas de slot), então precisam esticar por igual — 9-slice
            // deslocaria o miolo e os retângulos medidos sairiam do lugar.
            SkinReplace("StatusBars",       "hud_hp_mana",  simple: true);
            SkinReplace("EnemyStatusPanel", "hud_inimigo",  simple: true);
            SkinReplace("ArenaUI",          "hud_slots",    simple: true);

            SkinReplace("CombatLogPanel",   "hud_registro");

            // A barra NUNCA estica: o rect dela já tem a proporção exata da arte
            // (ver LayoutBottomBar), então a imagem entra inteira e o arco central
            // e as caixas dos botões continuam com o desenho original.
            SkinReplace("MovementBar", "hud_baixo", simple: true);
            var barImg = Find("MovementBar")?.GetComponent<Image>();
            if (barImg != null) barImg.preserveAspect = true;

            // O log tem texto claro (feito para fundo preto): sobre o pergaminho
            // ele precisa virar tinta escura.
            var log = FindAnyObjectByType<CombatLogUI>();
            if (log != null) log.ApplyParchmentPalette();

            // O menu do grimório fica SEM moldura (a pedido)

            // Telas cheias de recompensa e fim de partida
            SkinScreen("RewardScreen", new Vector2(1060f, 620f));
            SkinScreen("EndScreen",    new Vector2(820f, 520f));

            LayoutBotoesDaBarra();
            StylePlaqueButton(Find("RestartButton"), null);   // mantém o rect da cena

            // Paletas legíveis sobre as novas artes
            var hud = FindAnyObjectByType<ArenaHUD>();
            if (hud != null) hud.ApplyParchmentSkin();

            BuildEndTurnButton();
            BuildGrimoireButton();
            BuildGrimoireCloseButton();
            SkinGrimoire();
            BuildColunasDoBanco();
            BuildFragmentosPanel();
            BuildTutorial();
            ApplyTextStyle();
        }

        /// <summary>Botão com placa de madeira e rótulo branco. size null = mantém o da cena.</summary>
        private void StylePlaqueButton(Transform t, Vector2? size)
        {
            if (t == null) return;

            var botao = Load("hud_botao");
            var img = t.GetComponent<Image>();
            if (img != null && botao != null) UiFrame.Apply(img, botao, 0.5f);

            if (size.HasValue)
            {
                if (t is RectTransform rt) rt.sizeDelta = size.Value;

                // Dentro do HorizontalLayoutGroup quem manda é o LayoutElement
                var le = t.GetComponent<LayoutElement>();
                if (le == null) le = t.gameObject.AddComponent<LayoutElement>();
                le.minWidth = le.preferredWidth = size.Value.x;
                le.minHeight = le.preferredHeight = size.Value.y;
            }

            // A cena vinha com disabledColor de alpha 0.5: ao clicar, o botão é
            // desabilitado e a placa praticamente sumia. Mantemos a arte visível
            // (quem escurece o botão inativo é o tint do ArenaHUD).
            var btn = t.GetComponent<Button>();
            if (btn != null)
            {
                var cb = btn.colors;
                cb.normalColor      = Color.white;
                cb.highlightedColor = Color.white;
                cb.pressedColor     = new Color(0.85f, 0.85f, 0.85f, 1f);
                cb.selectedColor    = Color.white;
                cb.disabledColor    = Color.white;
                cb.fadeDuration     = 0.08f;
                btn.colors = cb;
            }

            foreach (var tmp in t.GetComponentsInChildren<TextMeshProUGUI>(true))
                tmp.color = Color.white;
        }

        // ─────────────────────────────────────────────────────────────────────
        // BARRAS (HP / MANA / INIMIGO)
        // ─────────────────────────────────────────────────────────────────────

        private static Sprite _solid;

        /// <summary>Sprite branco sólido (sem bordas suaves).</summary>
        private static Sprite SolidSprite()
        {
            if (_solid == null)
            {
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                var px = new Color32[16];
                for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
                tex.SetPixels32(px);
                tex.Apply();

                _solid = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f),
                                       100f, 0, SpriteMeshType.FullRect);
            }
            return _solid;
        }

        // ─────────────────────────────────────────────────────────────────────
        // TELAS DE RECOMPENSA / FIM DE PARTIDA
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Em vez de tirar o fundo da tela cheia, ele é recolorido num tom da HUD
        /// e ganha uma moldura central atrás do conteúdo.
        /// </summary>
        private void SkinScreen(string screenName, Vector2 frameSize)
        {
            var screen = Find(screenName);
            if (screen == null || Child(screen, "HudFrame") != null) return;

            var bg = screen.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = SolidSprite();
                bg.type = Image.Type.Simple;
                bg.color = ScreenTint;
            }

            var frameSprite = Load("hud_painel");
            if (frameSprite == null) return;

            var go = new GameObject("HudFrame", typeof(RectTransform));
            go.transform.SetParent(screen, false);
            go.transform.SetSiblingIndex(0);   // à frente do fundo, atrás do conteúdo

            SetRect(go.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), Vector2.zero, frameSize);

            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            UiFrame.Apply(img, frameSprite);

            // Títulos ficam direto sobre o pergaminho
            DarkenDirectTexts(screen);
        }

        /// <summary>Só os textos filhos diretos (os que caem sobre o pergaminho).</summary>
        private static void DarkenDirectTexts(Transform panel)
        {
            for (int i = 0; i < panel.childCount; i++)
            {
                var tmp = panel.GetChild(i).GetComponent<TextMeshProUGUI>();
                if (tmp != null && Luminance(tmp.color) > 0.5f) tmp.color = Ink;
            }
        }

        /// <summary>Fonte +20% e negrito em toda a UI.</summary>
        private void ApplyTextStyle()
        {
            var styler = gameObject.AddComponent<TextStyler>();

            // O registro de combate calcula a altura das linhas pelo fontSize,
            // então ele mesmo se reescala (e o styler não encosta nele).
            var log = FindAnyObjectByType<CombatLogUI>();
            if (log != null)
            {
                log.ScaleFont(TextStyler.Scale);
                styler.Ignore(log.ContentRoot);
            }

            styler.Restyle();
        }

        // ─────────────────────────────────────────────────────────────────────
        // LAYOUT (posições e tamanhos da HUD, seguindo o desenho de referência)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>HP+Mana no canto superior esquerdo; inimigo no superior direito.</summary>
        private void LayoutStatusBars()
        {
            var statusBars = Find("StatusBars");
            if (statusBars == null) return;

            // A moldura passa a envolver as barras (antes era um retângulo de 220x60
            // ao lado delas)
            SetRect(statusBars, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(16f, -14f), PlayerPanelSize);

            var playerPanel = Child(statusBars, "PlayerStatusPanel");
            if (playerPanel != null)
            {
                Stretch(playerPanel, 0f, 0f, 0f, 0f);   // preenche a moldura

                // Retângulos medidos direto na arte (fração do painel): as barras
                // ocupam exatamente as faixas verde e azul do desenho.
                var hp   = Child(playerPanel, "PlayerHPBarBG");
                var mana = Child(playerPanel, "PlayerManaBarBG");
                SetNormRect(hp,   0.0795f, 0.3151f, 0.9212f, 0.5651f);
                SetNormRect(mana, 0.0795f, 0.6376f, 0.9145f, 0.8787f);
                SkinBar(hp,   "barra_hp");
                SkinBar(mana, "barra_mana");

                // O painel do inimigo estava ANINHADO dentro do painel do jogador,
                // alcançando o canto direito com um deslocamento de +1400px. Sai de
                // dentro dele e passa a ser ancorado no canto superior direito.
                var enemyPanel = Child(playerPanel, "EnemyStatusPanel");
                if (enemyPanel != null)
                {
                    enemyPanel.SetParent(statusBars.parent, false);
                    SetRect(enemyPanel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                            new Vector2(-16f, -14f), EnemyPanelSize);

                    var enemyHP = Child(enemyPanel, "EnemyHPBarBG");
                    var nome    = Child(enemyPanel, "NameText");
                    SetNormRect(nome,    0.10f,   0.14f,   0.85f,   0.55f);
                    SetNormRect(enemyHP, 0.1002f, 0.6974f, 0.8493f, 0.8974f);
                    SkinBar(enemyHP, "barra_inimigo");

                    // O nome fica sobre o pergaminho, então vai de tinta escura
                    var tmp = nome != null ? nome.GetComponent<TextMeshProUGUI>() : null;
                    if (tmp != null) tmp.color = Ink;
                }
            }

            var status = FindAnyObjectByType<StatusBarsUI>();
            if (status != null) status.UseArtSkin();
        }

        // Tamanhos que respeitam a proporção das artes (senão os retângulos
        // medidos nelas não caem no lugar certo)
        private static readonly Vector2 PlayerPanelSize = new Vector2(400f, 203f); // arte 1333x676
        private static readonly Vector2 EnemyPanelSize  = new Vector2(400f, 200f); // arte 1168x585
        private static readonly Vector2 SlotsPanelSize  = new Vector2(760f, 222f); // arte 1512x442

        /// <summary>
        /// Barra de arte: o fundo some (o pergaminho da moldura aparece) e o Fill
        /// vira o sprite colorido, esvaziando da direita para a esquerda.
        /// </summary>
        private void SkinBar(Transform barBG, string spriteName)
        {
            if (barBG == null) return;

            var bgImg = barBG.GetComponent<Image>();
            if (bgImg != null) bgImg.color = new Color(0f, 0f, 0f, 0f);

            var fill = barBG.Find("Fill");
            var img = fill != null ? fill.GetComponent<Image>() : null;
            if (img == null) return;

            var sprite = Load(spriteName);
            if (sprite != null) img.sprite = sprite;

            img.type        = Image.Type.Filled;
            img.fillMethod  = Image.FillMethod.Horizontal;
            img.fillOrigin  = (int)Image.OriginHorizontal.Left;  // esvazia da direita p/ esquerda
            img.color       = Color.white;
            img.preserveAspect = false;
        }

        /// <summary>Posiciona por FRAÇÃO do painel (y medido a partir do topo).</summary>
        private static void SetNormRect(Transform t, float x0, float yTop0, float x1, float yTop1)
        {
            if (t is not RectTransform rt) return;
            rt.anchorMin = new Vector2(x0, 1f - yTop1);
            rt.anchorMax = new Vector2(x1, 1f - yTop0);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Barra de slots: a arte já traz as 6 caixas desenhadas, então cada slot é
        /// encaixado na sua caixa (posições medidas na imagem) e o fundo próprio
        /// dele some. Os tokens viram os ícones do mago e do golem.
        /// </summary>
        private void LayoutSlotsBar()
        {
            var arenaUI = Find("ArenaUI");
            if (arenaUI == null) return;

            SetRect(arenaUI, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -12f), SlotsPanelSize);

            var container = Child(arenaUI, "SlotsContainer");
            if (container == null) return;

            Stretch(container, 0f, 0f, 0f, 0f);

            // O layout automático distribuiria os slots por igual; aqui eles
            // precisam cair exatamente sobre as caixas da arte.
            var group = container.GetComponent<HorizontalLayoutGroup>();
            if (group != null) group.enabled = false;

            for (int i = 0; i < 6; i++)
            {
                var slot = Child(container, $"Slot{i + 1}");
                if (slot == null) continue;

                SetNormRect(slot, SlotBoxes[i, 0], 0.0498f, SlotBoxes[i, 1], 0.9593f);

                // A caixa já está desenhada: o fundo do slot só serve para o realce
                var img = slot.GetComponent<Image>();
                if (img != null) img.color = new Color(0f, 0f, 0f, 0f);

                var outline = slot.GetComponent<Outline>();
                if (outline != null) outline.enabled = false;

                // O número vinha no rodapé do slot (25% de baixo) e, com as caixas
                // altas da arte, ficava largado no pé. Sobe para uma faixa curta.
                var label = Child(slot, "SlotLabel");
                if (label is RectTransform lrt)
                {
                    lrt.anchorMin = new Vector2(0f, 0.04f);
                    lrt.anchorMax = new Vector2(1f, 0.20f);
                    lrt.offsetMin = Vector2.zero;
                    lrt.offsetMax = Vector2.zero;
                }

                SkinToken(Child(slot, "PlayerToken"), "icone_jogador");
                SkinToken(Child(slot, "EnemyToken"),  "icone_inimigo", 0.6f);   // 40% menor
            }

            var arena = FindAnyObjectByType<ArenaUI>();
            if (arena != null) arena.UseArtSkin();
        }

        // Centro das 6 caixas da arte hud_slots, em fração da largura
        private static readonly float[,] SlotBoxes =
        {
            { 0.1250f, 0.2381f }, { 0.2526f, 0.3657f }, { 0.3803f, 0.4927f },
            { 0.5079f, 0.6217f }, { 0.6362f, 0.7493f }, { 0.7639f, 0.8763f },
        };

        /// <summary>
        /// Troca o quadradinho colorido pelo ícone (chapéu / cabeça de golem).
        /// A escala vai no localScale porque o ArenaUI reescreve âncoras e offsets
        /// a cada Refresh — o localScale ele não toca, então o ajuste sobrevive.
        /// </summary>
        private void SkinToken(Transform token, string spriteName, float escala = 1f)
        {
            if (token == null) return;

            var img = token.GetComponent<Image>();
            var sprite = Load(spriteName);
            if (img != null && sprite != null)
            {
                img.sprite = sprite;
                img.color = Color.white;
                img.preserveAspect = true;
            }

            token.localScale = Vector3.one * escala;

            // Centralizado na caixa, acima da faixa do número
            if (token is RectTransform rt)
            {
                rt.anchorMin = new Vector2(0.08f, 0.22f);
                rt.anchorMax = new Vector2(0.92f, 0.96f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            // O ícone substitui as letras P / E
            var label = token.Find("TokenLabel");
            if (label != null) label.gameObject.SetActive(false);
        }

        /// <summary>
        /// Barra inferior: console central apoiado na base, SEM esticar. A arte
        /// manda na proporção; o tamanho sai do menor entre 62% da largura e 34%
        /// da altura da tela. Assim ela fica generosa num monitor comum, não
        /// engole a arena num ultrawide e não estoura a altura num 4:3.
        /// </summary>
        private void LayoutBottomBar()
        {
            var bar = Find("MovementBar");
            if (bar == null) return;

            _barSize = TamanhoDaBarra(TamanhoDoCanvas(bar));

            SetRect(bar, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    Vector2.zero, _barSize);

            // Os botões não são mais distribuídos por layout: cada um vai para a
            // caixa que já está desenhada no banner (ver LayoutBotoesDaBarra).
            var group = bar.GetComponent<HorizontalLayoutGroup>();
            if (group != null) group.enabled = false;
        }

        /// <summary>
        /// Tamanho da barra para uma tela qualquer, SEMPRE na proporção da arte.
        /// Cabe em 62% da largura e 34% da altura — quem apertar primeiro manda.
        /// </summary>
        public static Vector2 TamanhoDaBarra(Vector2 tela)
        {
            float escala = Mathf.Min(tela.x * 0.62f / ArteBaixoW,
                                     tela.y * 0.34f / ArteBaixoH);
            return new Vector2(ArteBaixoW * escala, ArteBaixoH * escala);
        }

        private static Vector2 TamanhoDoCanvas(Transform dentroDe)
        {
            var canvas = dentroDe != null ? dentroDe.GetComponentInParent<Canvas>() : null;
            var rt = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            return rt != null ? rt.rect.size : new Vector2(1920f, 1080f);
        }

        /// <summary>
        /// Voltar / Manter / Avançar nas três caixas juntas, cada um com a placa
        /// nova encaixada sem deformar.
        /// </summary>
        private void LayoutBotoesDaBarra()
        {
            var bar = Find("MovementBar");
            if (bar == null) return;

            MontarBotaoDaBarra(bar, Find("BtnBack"),   SlotVoltar,  "hud_botao_mov", PlacaMovAspect);
            MontarBotaoDaBarra(bar, Find("BtnStay"),   SlotManter,  "hud_botao_mov", PlacaMovAspect);
            MontarBotaoDaBarra(bar, Find("BtnFoward"), SlotAvancar, "hud_botao_mov", PlacaMovAspect);
        }

        /// <summary>
        /// Encaixa um botão numa caixa desenhada do banner: a placa entra com
        /// preserveAspect (fica centrada, nunca esticada) e o rótulo se ajusta
        /// sozinho ao espaço que a placa realmente ocupa.
        /// </summary>
        private void MontarBotaoDaBarra(Transform bar, Transform t, Vector4 frac,
                                        string spriteName, float placaAspect)
        {
            if (t == null) return;

            NaFracao(bar, t, frac);

            var img = t.GetComponent<Image>();
            if (img == null) img = t.gameObject.AddComponent<Image>();
            var placa = Load(spriteName);
            if (placa != null)
            {
                img.sprite = placa;
                img.type = Image.Type.Simple;
                img.preserveAspect = true;      // é isto que impede o esticão
                img.color = Color.white;
            }

            // Com o HorizontalLayoutGroup desligado o LayoutElement não manda
            // mais em nada, mas deixá-lo por aí confunde quem for depurar.
            var le = t.GetComponent<LayoutElement>();
            if (le != null) le.ignoreLayout = true;

            var btn = t.GetComponent<Button>();
            if (btn != null)
            {
                var cb = btn.colors;
                cb.normalColor      = Color.white;
                cb.highlightedColor = new Color(1f, 0.97f, 0.85f);   // acende de leve
                cb.pressedColor     = new Color(0.82f, 0.78f, 0.70f);
                cb.selectedColor    = Color.white;
                cb.disabledColor    = new Color(0.62f, 0.58f, 0.52f);
                cb.fadeDuration     = 0.08f;
                btn.colors = cb;
            }

            // Área que a placa realmente ocupa dentro da caixa
            var caixa = new Vector2((frac.y - frac.x) * _barSize.x,
                                    (frac.w - frac.z) * _barSize.y);
            var placaSize = new Vector2(Mathf.Min(caixa.x, caixa.y * placaAspect),
                                        Mathf.Min(caixa.y, caixa.x / placaAspect));

            foreach (var label in t.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                SetRect(label.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f), Vector2.zero,
                        new Vector2(placaSize.x * 0.80f, placaSize.y * 0.56f));

                label.color = Ink;                       // tinta sobre o pergaminho
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.raycastTarget = false;

                // Auto-size: o mesmo rótulo serve de 720p a 4K sem estourar a placa
                label.enableAutoSizing = true;
                label.fontSizeMin = 6f;
                label.fontSizeMax = Mathf.Max(10f, placaSize.y * 0.40f);
            }
        }

        /// <summary>Posiciona um filho numa região medida em fração da arte do pai.</summary>
        private static RectTransform NaFracao(Transform pai, Transform filho, Vector4 frac)
        {
            if (filho == null) return null;

            var rt = filho as RectTransform;
            if (rt == null) rt = filho.gameObject.AddComponent<RectTransform>();
            if (filho.parent != pai) filho.SetParent(pai, false);

            rt.anchorMin = new Vector2(frac.x, 1f - frac.w);
            rt.anchorMax = new Vector2(frac.y, 1f - frac.z);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>
        /// Texto de dica do painel de query, com os valores ajustados no Inspector
        /// durante o play: bottom stretch, Left 2 / Right 8, Pos Y 8, Height -9.
        /// (O Inspector mostra Left/Right; em RectTransform isso vira
        /// sizeDelta (-10,-9) e anchoredPosition (-3,8).)
        /// </summary>
        private void LayoutHintText()
        {
            if (!(Find("HintText") is RectTransform rt)) return;

            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(-10f, -9f);
            rt.anchoredPosition = new Vector2(-3f, 8f);
        }

        /// <summary>
        /// O grimório mora no medalhão redondo do topo da barra: só o ícone do
        /// livro, sem placa e sem rótulo. Fechado por padrão, acende (abre) quando
        /// o mouse passa por cima e abre o grimório no clique.
        /// </summary>
        private void BuildGrimoireButton()
        {
            var bar = Find("MovementBar");
            if (bar == null || Child(bar, "BtnGrimoire") != null) return;

            var go = new GameObject("BtnGrimoire", typeof(RectTransform));
            NaFracao(bar, go.transform, SlotGrimorio);

            // Sem arte própria: o medalhão já está desenhado no banner. A imagem
            // existe só para o botão ter o que receber de raycast.
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(GrimoireUI.AbrirLivre);

            // O tint do Button pintaria a imagem invisível, não o livro
            var cb = btn.colors;
            cb.normalColor = cb.highlightedColor = cb.pressedColor =
                cb.selectedColor = cb.disabledColor = new Color(1f, 1f, 1f, 0f);
            btn.colors = cb;

            var iconGO = new GameObject("BookIcon", typeof(RectTransform));
            iconGO.transform.SetParent(go.transform, false);
            Stretch(iconGO.transform, 0f, 0f, 0f, 0f);

            var iconImg = iconGO.AddComponent<Image>();
            iconImg.raycastTarget = false;      // quem recebe o ponteiro é o botão
            iconImg.preserveAspect = true;      // o livro nunca deforma no círculo

            go.AddComponent<GrimoireBookIcon>()
              .Setup(iconImg, Load("grimorio_aberto"), Load("grimorio_fechado"));
        }

        /// <summary>
        /// Cria a tela de absorção (fica escondida). Ela NÃO tem botão de abrir:
        /// só aparece na cena que roda ao derrotar um golem.
        /// </summary>
        private void BuildFragmentosPanel()
        {
            var canvas = Find("CombatCanvas");
            if (canvas == null || FragmentosUI.Instance != null) return;

            FragmentosUI.Create(canvas);
        }

        /// <summary>
        /// Tutorial: abre sozinho num jogo novo e fica acessível pelo botão AJUDA,
        /// no topo do grimório (ao lado do X).
        /// </summary>
        private void BuildTutorial()
        {
            var canvas = Find("CombatCanvas");
            if (canvas == null || TutorialPopup.Instance != null) return;

            var popup = TutorialPopup.Create(canvas);

            var panel = Find("GrimoirePanel");
            if (panel != null && Child(panel, "BtnAjuda") == null)
            {
                var go = new GameObject("BtnAjuda", typeof(RectTransform));
                go.transform.SetParent(panel, false);
                go.transform.SetAsLastSibling();

                // Logo à esquerda do X que fecha o grimório
                SetRect(go.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-62f, -10f), new Vector2(104f, 46f));

                go.AddComponent<Image>();

                var btn = go.AddComponent<Button>();
                btn.onClick.AddListener(popup.Abrir);
                GrimoireSkin.VestirBotao(btn);   // dentro do grimório: paleta do grimório

                var labelGO = new GameObject("Label", typeof(RectTransform));
                labelGO.transform.SetParent(go.transform, false);
                Stretch(labelGO.transform, 6f, 3f, 6f, 3f);

                var label = labelGO.AddComponent<TextMeshProUGUI>();
                label.text = "AJUDA";
                label.fontSize = 15f;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.color = Color.white;
                label.raycastTarget = false;
            }

            // Jogo novo → abre a explicação uma vez
            if (GameSession.MostrarTutorial)
            {
                GameSession.MostrarTutorial = false;
                popup.Abrir();
            }
        }

        /// <summary>
        /// "Encerrar Turno", à direita de "Avançar". A magia e o movimento não
        /// passam mais a vez sozinhos — quem encerra o turno é o jogador.
        /// </summary>
        private void BuildEndTurnButton()
        {
            var bar = Find("MovementBar");
            if (bar == null || Child(bar, "BtnEndTurn") != null) return;

            var go = new GameObject("BtnEndTurn", typeof(RectTransform));
            go.transform.SetParent(bar, false);

            var img = go.AddComponent<Image>();

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => CombatManager.Instance?.EndTurn());

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(go.transform, false);

            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = "ENCERRAR TURNO";

            // Vai na caixa larga da direita, com a placa de cantos ogivais —
            // que é justamente o formato daquela caixa.
            MontarBotaoDaBarra(bar, go.transform, SlotTurno, "hud_botao_turno", PlacaTurnoAspect);

            go.AddComponent<EndTurnButton>().Setup(btn);
        }

        /// <summary>
        /// Veste o grimório na paleta da HUD, com superfícies geradas em runtime,
        /// e troca as abas Arsenal e Docs por uma única aba TABELAS.
        /// As artes da HUD NÃO servem aqui: são banners largos, com ornamento nas
        /// pontas, que deformam num painel alto e estreito.
        /// </summary>
        private void SkinGrimoire()
        {
            var painel = Find("GrimoirePanel");
            if (painel == null) return;

            GrimoireSkin.Aplicar(painel);

            var barra = Child(painel, "TabBar");
            var area  = Child(painel, "ContentArea");
            if (barra == null || area == null) return;

            var abaQuery   = Child(barra, "TabQuery");
            var abaMagias  = Child(barra, "TabMagias");
            var abaArsenal = Child(barra, "TabArsenal");
            var abaDocs    = Child(barra, "TabDocs");

            var pQuery   = Child(area, "PanelQuery");
            var pMagias  = Child(area, "PanelMagias");
            var pArsenal = Child(area, "PanelArsenal");
            var pDocs    = Child(area, "PanelDocs");

            // Docs sai de cena: o conteúdo dela foi absorvido pela aba TABELAS
            if (abaDocs != null) abaDocs.gameObject.SetActive(false);
            if (pDocs   != null) pDocs.gameObject.SetActive(false);

            // Arsenal vira TABELAS
            if (abaArsenal != null)
                foreach (var tmp in abaArsenal.GetComponentsInChildren<TextMeshProUGUI>(true))
                    tmp.text = "TABELAS";

            if (pArsenal != null) TabelasUI.Instalar(pArsenal);

            GrimoireSkin.RegistrarAba(abaQuery?.GetComponent<Button>(),   pQuery?.gameObject);
            GrimoireSkin.RegistrarAba(abaMagias?.GetComponent<Button>(),  pMagias?.gameObject);
            GrimoireSkin.RegistrarAba(abaArsenal?.GetComponent<Button>(), pArsenal?.gameObject);
            GrimoireSkin.RepintarAbas();
        }

        /// <summary>
        /// Os nomes das colunas do banco ao lado de cada barra de vida: Magias
        /// junto do jogador, Inimigos junto do golem. É a consulta que o aluno
        /// vai escrever, então o vocabulário fica à vista o tempo todo.
        /// </summary>
        private void BuildColunasDoBanco()
        {
            var statusBars = Find("StatusBars");
            if (statusBars == null || statusBars.parent == null) return;
            if (Child(statusBars.parent, "ColunasMagias") != null) return;

            var pai = statusBars.parent;

            // À direita do painel do jogador (que fica no canto superior esquerdo)
            var magias = CriarLegendaColunas(pai, "ColunasMagias", "Magias",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(PlayerPanelSize.x + 26f, -20f), TextAlignmentOptions.TopLeft);

            // À esquerda do painel do inimigo (canto superior direito)
            var inimigos = CriarLegendaColunas(pai, "ColunasInimigos", "Inimigos",
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-(EnemyPanelSize.x + 26f), -20f), TextAlignmentOptions.TopRight);

            if (inimigos != null) inimigos.rectTransform.pivot = new Vector2(1f, 1f);
        }

        private TextMeshProUGUI CriarLegendaColunas(Transform pai, string nome, string tabela,
                                                    Vector2 ancora, Vector2 pivo, Vector2 pos,
                                                    TextAlignmentOptions alinhamento)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(pai, false);
            SetRect(go.transform, ancora, ancora, pivo, pos, new Vector2(300f, 74f));

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = SchemaGuia.LinhaDeColunas(tabela);
            tmp.fontSize = 12f;
            tmp.color = new Color(0.94f, 0.90f, 0.80f);
            tmp.alignment = alinhamento;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            tmp.lineSpacing = 4f;

            // Contorno escuro: o texto fica sobre o cenário, que muda a cada andar
            tmp.fontMaterial.EnableKeyword("OUTLINE_ON");
            tmp.outlineColor = new Color32(20, 12, 4, 255);
            tmp.outlineWidth = 0.22f;

            // O banco pode ainda não estar carregado quando a HUD é montada
            if (string.IsNullOrEmpty(tmp.text)) StartCoroutine(PreencherQuandoOBancoAbrir(tmp, tabela));
            return tmp;
        }

        private IEnumerator PreencherQuandoOBancoAbrir(TextMeshProUGUI tmp, string tabela)
        {
            float limite = Time.realtimeSinceStartup + 20f;
            while (tmp != null && string.IsNullOrEmpty(tmp.text) && Time.realtimeSinceStartup < limite)
            {
                yield return new WaitForSeconds(0.25f);
                if (tmp != null) tmp.text = SchemaGuia.LinhaDeColunas(tabela);
            }
        }

        /// <summary>X no canto do grimório — fecha a tela.</summary>
        private void BuildGrimoireCloseButton()
        {
            var panel = Find("GrimoirePanel");
            if (panel == null || Child(panel, "BtnCloseGrimoire") != null) return;

            var go = new GameObject("BtnCloseGrimoire", typeof(RectTransform));
            go.transform.SetParent(panel, false);
            go.transform.SetAsLastSibling();   // por cima do conteúdo do grimório

            SetRect(go.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(-10f, -10f), new Vector2(46f, 46f));

            go.AddComponent<Image>();

            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(GrimoireUI.FecharLivre);
            GrimoireSkin.VestirBotao(btn);   // dentro do grimório: paleta do grimório

            var labelGO = new GameObject("X", typeof(RectTransform));
            labelGO.transform.SetParent(go.transform, false);
            Stretch(labelGO.transform, 0f, 0f, 0f, 2f);

            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = "X";
            label.fontSize = 22f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS DE RECTTRANSFORM
        // ─────────────────────────────────────────────────────────────────────

        private static void SetRect(Transform t, Vector2 aMin, Vector2 aMax, Vector2 pivot,
                                    Vector2 pos, Vector2 size)
        {
            if (t == null) return;
            // GameObject "cru" ainda não tem RectTransform — sem isto o ajuste
            // seria silenciosamente ignorado e o objeto ficaria 100x100 no centro.
            var rt = t as RectTransform;
            if (rt == null) rt = t.gameObject.AddComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        /// <summary>Preenche o pai com as margens dadas.</summary>
        private static void Stretch(Transform t, float left, float bottom, float right, float top)
        {
            if (t is not RectTransform rt) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        private static Transform Child(Transform parent, string name)
            => parent == null ? null : parent.Find(name);

        // ─────────────────────────────────────────────────────────────────────
        // MODOS DE APLICAÇÃO
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// A moldura vira o fundo do painel. Com fillCenter=false só a borda é
        /// desenhada (o miolo fica transparente) — nesse caso os textos NÃO são
        /// escurecidos, porque continuam sobre o cenário e não sobre pergaminho.
        /// </summary>
        private void SkinReplace(string panelName, string spriteName,
                                 bool fillCenter = true, float middleFraction = 0.55f,
                                 bool simple = false)
        {
            var panel = Find(panelName);
            var sprite = Load(spriteName);
            if (panel == null || sprite == null) return;

            var img = panel.GetComponent<Image>();
            if (img == null)
            {
                img = panel.gameObject.AddComponent<Image>();
                img.raycastTarget = false;   // fundo novo não deve roubar cliques
            }

            if (simple)
            {
                // Esticamento uniforme: preserva a proporção interna da arte
                img.sprite = sprite;
                img.type = Image.Type.Simple;
                img.color = Color.white;
                img.preserveAspect = false;
                return;
            }

            UiFrame.Apply(img, sprite, middleFraction, fillCenter);

            if (fillCenter) DarkenTextsOnParchment(panel);
        }

        // ─────────────────────────────────────────────────────────────────────
        // LEGIBILIDADE
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Passa para tinta escura os textos claros que ficariam sobre o
        /// pergaminho. Textos que já estão sobre algo escuro (barras de HP,
        /// caixas de slot, botões) são deixados como estão.
        /// </summary>
        private static void DarkenTextsOnParchment(Transform panel)
        {
            foreach (var tmp in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (Luminance(tmp.color) < 0.5f) continue;
                if (HasDarkBackdrop(tmp.transform, panel)) continue;
                if (tmp.GetComponentInParent<Button>() != null) continue; // botão tem placa escura
                tmp.color = Ink;
            }
        }

        private static bool HasDarkBackdrop(Transform from, Transform panel)
        {
            for (var cur = from; cur != null && cur != panel; cur = cur.parent)
            {
                var img = cur.GetComponent<Image>();
                if (img != null && img.enabled && img.color.a > 0.5f && Luminance(img.color) < 0.5f)
                    return true;
            }
            return false;
        }

        private static float Luminance(Color c) => c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private static Sprite Load(string name) => Resources.Load<Sprite>("Sprites/UI/" + name);

        /// <summary>Busca por nome na cena inteira (inclusive objetos desativados).</summary>
        private static Transform Find(string name)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var found = FindDeep(root.transform, name);
                if (found != null) return found;
            }
            return null;
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var found = FindDeep(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }

    /// <summary>Só deixa encerrar o turno quando é a vez do jogador.</summary>
    public class EndTurnButton : MonoBehaviour
    {
        private Button _btn;

        public void Setup(Button btn)
        {
            _btn = btn;
            Refresh(CombatManager.Instance != null ? CombatManager.Instance.CurrentState : CombatState.IDLE);
        }

        private void Start()
        {
            if (CombatManager.Instance != null)
                CombatManager.Instance.OnStateChanged += Refresh;
        }

        private void OnDestroy()
        {
            if (CombatManager.Instance != null)
                CombatManager.Instance.OnStateChanged -= Refresh;
        }

        private void Refresh(CombatState state)
        {
            if (_btn == null) return;
            _btn.interactable = state == CombatState.PLAYER_TURN || state == CombatState.GRIMOIRE_OPEN;
        }
    }

    /// <summary>
    /// Livro fechado por padrão; abre quando o mouse passa sobre o botão.
    /// Fica no GameObject do BOTÃO (é ele que recebe o ponteiro — o ícone
    /// tem raycastTarget desligado).
    /// </summary>
    public class GrimoireBookIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Image _img;
        private Sprite _aberto, _fechado;

        public void Setup(Image img, Sprite aberto, Sprite fechado)
        {
            _img = img;
            _aberto = aberto;
            _fechado = fechado;
            Show(_fechado);
        }

        public void OnPointerEnter(PointerEventData eventData) => Show(_aberto);
        public void OnPointerExit(PointerEventData eventData)  => Show(_fechado);

        private void Show(Sprite s)
        {
            if (_img != null && s != null) _img.sprite = s;
        }
    }
}
