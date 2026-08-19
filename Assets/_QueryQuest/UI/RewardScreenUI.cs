// Assets/_QueryQuest/UI/RewardScreenUI.cs
// Tela de recompensa exibida após vencer um andar.
// Mostra 3 itens; o jogador escolhe 1, que é aplicado, e avança de andar.
//
// Hierarquia esperada:
//
// RewardScreen (este script)  [Image = fundo escurecido tela cheia, começa inativo]
// ├── TitleText               (TMP) "ESCOLHA UMA RECOMPENSA"
// └── CardsRow                [HorizontalLayoutGroup]
//     ├── Card1               [Button + Image]
//     │   ├── CardTitle       (TMP)
//     │   └── CardDesc        (TMP)
//     ├── Card2 (igual)
//     └── Card3 (igual)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QueryQuest.Combat;

namespace QueryQuest.UI
{
    public class RewardScreenUI : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private GameObject panel;       // o painel inteiro (liga/desliga)
        [SerializeField] private Button[] cardButtons;   // 3 botões
        [SerializeField] private TextMeshProUGUI[] cardTitles; // 3 títulos
        [SerializeField] private TextMeshProUGUI[] cardDescs;  // 3 descrições

        [Header("Cores por tipo")]
        [SerializeField] private Color armorColor = new Color(0.30f, 0.45f, 0.70f);
        [SerializeField] private Color staffColor = new Color(0.70f, 0.40f, 0.30f);
        [SerializeField] private Color pageColor  = new Color(0.45f, 0.35f, 0.65f);

        private List<RewardItem> _currentRewards;

        private void Awake()
        {
            // Conecta os botões
            for (int i = 0; i < cardButtons.Length; i++)
            {
                int index = i; // captura local
                cardButtons[i]?.onClick.AddListener(() => OnCardChosen(index));
            }
        }

        private void Start()
        {
            if (FloorManager.Instance != null)
                FloorManager.Instance.OnRewardReady += ShowRewards;

            HidePanel();
        }

        private void OnDestroy()
        {
            if (FloorManager.Instance != null)
                FloorManager.Instance.OnRewardReady -= ShowRewards;
        }

        // ─────────────────────────────────────────────────────────────────────
        // EXIBIÇÃO
        // ─────────────────────────────────────────────────────────────────────

        public void ShowRewards()
        {
            _currentRewards = RewardGenerator.Generate(3);

            for (int i = 0; i < cardButtons.Length && i < _currentRewards.Count; i++)
                VestirCartao(cardButtons[i], cardTitles[i], cardDescs[i], _currentRewards[i]);

            ShowPanel();
        }

        /// <summary>
        /// Monta o cartão: pergaminho com borda na cor do tipo, uma faixa
        /// colorida no topo com a categoria, o nome em destaque e o efeito
        /// mecânico separado da descrição narrativa.
        /// </summary>
        private void VestirCartao(Button botao, TextMeshProUGUI titulo, TextMeshProUGUI desc,
                                  RewardItem item)
        {
            if (botao == null || item == null) return;

            Color cor = CorDoTipo(item.Type);

            // Fundo: pergaminho com filete na cor do tipo (superfície gerada em
            // runtime — as artes da HUD são banners largos e deformariam aqui)
            var img = botao.GetComponent<Image>();
            if (img != null) GrimoireSkin.Vestir(img, GrimoireSkin.Pergaminho, Escurecer(cor, 0.75f), 12, 3);

            var cores = botao.colors;
            cores.normalColor      = Color.white;
            cores.highlightedColor = new Color(1f, 0.97f, 0.86f);
            cores.pressedColor     = new Color(0.85f, 0.81f, 0.72f);
            cores.selectedColor    = Color.white;
            cores.fadeDuration     = 0.08f;
            botao.colors = cores;

            FaixaDoTipo(botao.transform, item.Type, cor);

            // O CardTitle da cena nasce colado no topo — exatamente onde a faixa
            // entra. Sem reposicionar, os dois se sobrepõem. As três regiões são
            // fixadas por fração do cartão para não dependerem do que veio da cena.
            PorFracao(titulo, 0.08f, 0.545f, 0.92f, 0.760f);
            PorFracao(desc,   0.08f, 0.070f, 0.92f, 0.525f);

            string hex = ColorUtility.ToHtmlStringRGB(Escurecer(cor, 0.55f));

            if (titulo != null)
            {
                titulo.text = $"<b>{item.Title}</b>";
                titulo.color = GrimoireSkin.Tinta;
                titulo.fontStyle |= FontStyles.Bold;
                titulo.textWrappingMode = TextWrappingModes.Normal;
                titulo.enableAutoSizing = true;
                titulo.fontSizeMin = 11f;
                titulo.fontSizeMax = 19f;
            }

            if (desc != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"<size=92%><color=#{hex}><b>{EfeitoMecanico(item)}</b></color></size>");
                sb.Append($"<size=86%>{item.Description}</size>");
                desc.text = sb.ToString();
                desc.color = GrimoireSkin.Tinta;
                desc.alignment = TextAlignmentOptions.Top;
                desc.textWrappingMode = TextWrappingModes.Normal;
                desc.enableAutoSizing = true;
                desc.fontSizeMin = 9f;
                desc.fontSizeMax = 14f;
            }
        }

        /// <summary>Faixa no topo do cartão com a categoria da recompensa.</summary>
        private static void FaixaDoTipo(Transform cartao, RewardType tipo, Color cor)
        {
            var existente = cartao.Find("FaixaTipo");
            var go = existente != null ? existente.gameObject
                                       : new GameObject("FaixaTipo", typeof(RectTransform));
            if (existente == null)
            {
                go.transform.SetParent(cartao, false);
                go.transform.SetAsFirstSibling();
            }

            // Faixa propria no topo do cartao, em fracao — assim ela nunca
            // colide com o titulo, que fica logo abaixo (ver VestirCartao).
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.08f, 0.790f);
            rt.anchorMax = new Vector2(0.92f, 0.945f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            GrimoireSkin.Vestir(img, cor, Escurecer(cor, 0.6f), 6, 1);
            img.raycastTarget = false;

            var rotuloT = go.transform.Find("Rotulo");
            var rotuloGO = rotuloT != null ? rotuloT.gameObject
                                           : new GameObject("Rotulo", typeof(RectTransform));
            if (rotuloT == null) rotuloGO.transform.SetParent(go.transform, false);
            var rrt = (RectTransform)rotuloGO.transform;
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = new Vector2(3f, 1f); rrt.offsetMax = new Vector2(-3f, -1f);

            var tmp = rotuloGO.GetComponent<TextMeshProUGUI>() ?? rotuloGO.AddComponent<TextMeshProUGUI>();
            tmp.text = NomeDoTipo(tipo);
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            tmp.enableAutoSizing = true;
            // Piso baixo de proposito: o rotulo e curto e precisa caber numa
            // faixa estreita — melhor encolher que transbordar.
            tmp.fontSizeMin = 5f;
            tmp.fontSizeMax = 13f;
        }

        /// <summary>Posiciona um elemento por fração do cartão.</summary>
        private static void PorFracao(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void PorFracao(TextMeshProUGUI t, float x0, float y0, float x1, float y1)
            => PorFracao(t != null ? t.rectTransform : null, x0, y0, x1, y1);

        private static string NomeDoTipo(RewardType t) => t switch
        {
            RewardType.Armor => "DEFESA",
            RewardType.Staff => "ELEMENTO",
            RewardType.Page  => "NOVA MAGIA",
            _ => "RECOMPENSA",
        };

        /// <summary>O número que interessa, separado do texto narrativo.</summary>
        private static string EfeitoMecanico(RewardItem item) => item.Type switch
        {
            RewardType.Armor => "-12% de dano recebido",
            RewardType.Staff => $"+25% de dano com {item.Element}",
            RewardType.Page  => $"Desbloqueia {item.SpellName}",
            _ => "",
        };

        private Color CorDoTipo(RewardType t) => t switch
        {
            RewardType.Armor => armorColor,
            RewardType.Staff => staffColor,
            RewardType.Page  => pageColor,
            _ => Color.gray,
        };

        private static Color Escurecer(Color c, float f) => new Color(c.r * f, c.g * f, c.b * f, c.a);

        private void OnCardChosen(int index)
        {
            if (_currentRewards == null || index >= _currentRewards.Count) return;

            var chosen = _currentRewards[index];
            Debug.Log($"[RewardScreen] Jogador escolheu: {chosen.Title}");

            RewardGenerator.ApplyReward(chosen);
            HidePanel();

            // Avança para o próximo andar
            FloorManager.Instance?.AdvanceToNextFloor();
        }

        // ─────────────────────────────────────────────────────────────────────
        // LIGA/DESLIGA
        // ─────────────────────────────────────────────────────────────────────

        private void ShowPanel()
        {
            if (panel != null) panel.SetActive(true);
            else gameObject.SetActive(true);
        }

        private void HidePanel()
        {
            if (panel != null) panel.SetActive(false);
            else gameObject.SetActive(false);
        }
    }
}
