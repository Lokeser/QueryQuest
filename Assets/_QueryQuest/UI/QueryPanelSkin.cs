// Assets/_QueryQuest/UI/QueryPanelSkin.cs
// Arruma a aba QUERY do grimório e acrescenta a barra de custo de mana.
//
// A leitura da tela passa a ser de cima para baixo, na ordem em que o jogador
// pensa:
//
//   ┌──────────────────────────────────────────────┐
//   │ CUSTO DA MAGIA            45 de mana         │  ← rótulo
//   │ ██████████████████████████████████████████   │  ← barra (esvazia)
//   │ ┌──────────────────────────────────────────┐ │
//   │ │ SELECT * FROM Magias WHERE ...           │ │  ← consulta
//   │ └──────────────────────────────────────────┘ │
//   │ [SELECT] [*] [Nome] [Elemento] ...           │  ← sugestões
//   │              [  EXECUTAR  ]                  │
//   │ ┌──────────────────────────────────────────┐ │
//   │ │ retorno da consulta                      │ │  ← registro
//   │ └──────────────────────────────────────────┘ │
//   └──────────────────────────────────────────────┘
//
// A BARRA nasce CHEIA no custo máximo (SELECT * FROM Magias = 45 de mana) e
// esvazia conforme a consulta fica específica: ela mostra o que a magia vai
// CUSTAR, não a mana que sobra. É a regra do jogo virando imagem — o aluno vê
// o preço cair no instante em que digita o WHERE, antes de executar.
//
// Tudo é posicionado por FRAÇÃO do painel, e não em pixels, para acompanhar
// qualquer resolução. A cena não é tocada.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QueryQuest.Combat;

namespace QueryQuest.UI
{
    public static class QueryPanelSkin
    {
        // Faixas do painel, em fração medida a partir do TOPO.
        private static readonly Vector2 FaixaMedidor   = new Vector2(0.020f, 0.115f);
        private static readonly Vector2 FaixaConsulta  = new Vector2(0.133f, 0.303f);
        private static readonly Vector2 FaixaSugestoes = new Vector2(0.317f, 0.387f);
        private static readonly Vector2 FaixaExecutar  = new Vector2(0.399f, 0.477f);
        private static readonly Vector2 FaixaRegistro  = new Vector2(0.495f, 0.930f);
        private static readonly Vector2 FaixaDica      = new Vector2(0.940f, 1.000f);

        /// <summary>Monta o layout e a barra de custo. Idempotente.</summary>
        public static void Instalar(Transform painel)
        {
            if (painel == null) return;

            var consulta  = Buscar(painel, "QueryInputField");
            var executar  = Buscar(painel, "ExecuteButton");
            var sugestoes = Buscar(painel, "SuggestionsBar");
            var registro  = Buscar(painel, "LogScrollView");
            var dica      = Buscar(painel, "HintText");

            // O campo e o botão nasceram dentro de InputRow, um
            // HorizontalLayoutGroup que os espremia numa faixa de 36px no rodapé.
            // Saem de lá para serem posicionados por fração; a linha vazia some.
            var linha = Buscar(painel, "InputRow");
            if (linha != null && linha != painel)
            {
                if (consulta != null) consulta.SetParent(painel, false);
                if (executar != null) executar.SetParent(painel, false);
                linha.gameObject.SetActive(false);
            }

            PorFracao(consulta,  0.030f, FaixaConsulta.x,  0.970f, FaixaConsulta.y);
            PorFracao(sugestoes, 0.030f, FaixaSugestoes.x, 0.970f, FaixaSugestoes.y);
            PorFracao(executar,  0.300f, FaixaExecutar.x,  0.700f, FaixaExecutar.y);
            PorFracao(registro,  0.030f, FaixaRegistro.x,  0.970f, FaixaRegistro.y);
            PorFracao(dica,      0.030f, FaixaDica.x,      0.970f, FaixaDica.y);

            VestirConsulta(consulta);
            ArrumarSugestoes(sugestoes);
            VestirExecutar(executar);
            VestirRegistro(registro);
            VestirDica(dica);

            MontarMedidor(painel, consulta);
        }

        // ─────────────────────────────────────────────────────────────────────
        // A BARRA DE CUSTO
        // ─────────────────────────────────────────────────────────────────────

        private static void MontarMedidor(Transform painel, Transform consulta)
        {
            if (painel.Find("MedidorMana") != null) return;

            var go = new GameObject("MedidorMana", typeof(RectTransform));
            go.transform.SetParent(painel, false);
            go.transform.SetAsFirstSibling();
            PorFracao(go.transform, 0.030f, FaixaMedidor.x, 0.970f, FaixaMedidor.y);

            // Rótulo e número dividem a linha de cima; a barra ocupa a de baixo.
            var rotulo = Texto(go.transform, "Rotulo", "CUSTO DA MAGIA", 13f,
                               GrimoireSkin.Ouro, TextAlignmentOptions.Left,
                               0f, 0f, 0.50f, 0.42f);

            var valor = Texto(go.transform, "Valor", "", 13f,
                              GrimoireSkin.Pergaminho, TextAlignmentOptions.Right,
                              0.50f, 0f, 1f, 0.42f);

            // Trilho escuro com filete dourado — a barra precisa de um fundo para
            // que "vazia" também seja uma informação visível.
            var trilhoGO = new GameObject("Trilho", typeof(RectTransform));
            trilhoGO.transform.SetParent(go.transform, false);
            PorFracao(trilhoGO.transform, 0f, 0.52f, 1f, 1f);

            var trilho = trilhoGO.AddComponent<Image>();
            GrimoireSkin.Vestir(trilho, new Color(0.11f, 0.06f, 0.02f), GrimoireSkin.OuroEscuro, 7, 2);
            trilho.raycastTarget = false;

            var fillGO = new GameObject("Fill", typeof(RectTransform));
            fillGO.transform.SetParent(trilhoGO.transform, false);
            var frt = (RectTransform)fillGO.transform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(3f, 3f);
            frt.offsetMax = new Vector2(-3f, -3f);

            var fill = fillGO.AddComponent<Image>();
            // Sprite BRANCO em 9-slice: a cor vem do tint, que muda para vermelho
            // quando a mana não dá.
            //
            // Com Type.Filled o sprite arredondado era ESTICADO na largura toda da
            // barra, e os cantos de 6px viravam rampas longas — a barra parecia
            // afinar nas pontas. Em 9-slice os cantos mantêm o tamanho original e
            // só o miolo estica; quem controla o preenchimento é a largura do
            // próprio rect (ver ManaCostMeter.Atualizar).
            GrimoireSkin.Vestir(fill, Color.white, Color.white, 6, 0);
            fill.raycastTarget = false;

            var medidor = go.AddComponent<ManaCostMeter>();
            medidor.Configurar(consulta != null ? consulta.GetComponent<TMP_InputField>() : null,
                               fill, rotulo, valor);
        }

        // ─────────────────────────────────────────────────────────────────────
        // ACABAMENTO DE CADA PEÇA
        // ─────────────────────────────────────────────────────────────────────

        private static void VestirConsulta(Transform t)
        {
            var campo = t != null ? t.GetComponent<TMP_InputField>() : null;
            if (campo == null) return;

            // Faixa alta: a consulta pode passar de uma linha. MultiLineSubmit e
            // não MultiLineNewline — com Newline o Enter insere quebra de linha e
            // o onSubmit nunca dispara, ou seja, o Enter deixaria de executar a
            // consulta (ver QueryTerminalUI.Awake).
            campo.lineType = TMP_InputField.LineType.MultiLineSubmit;
            campo.textComponent.alignment = TextAlignmentOptions.TopLeft;
            campo.textComponent.fontSize = 17f;
            campo.textComponent.enableAutoSizing = false;
            campo.pointSize = 17f;

            if (campo.placeholder is TextMeshProUGUI ph)
            {
                ph.text = "SELECT * FROM Magias WHERE Elemento = 'Fogo'";
                ph.fontSize = 15f;
                ph.enableAutoSizing = false;
                ph.alignment = TextAlignmentOptions.TopLeft;
                ph.fontStyle = FontStyles.Italic;
            }

            var area = t.Find("Text Area");
            if (area is RectTransform art)
            {
                art.offsetMin = new Vector2(12f, 8f);
                art.offsetMax = new Vector2(-12f, -8f);
            }
        }

        /// <summary>
        /// A barra de sugestões vinha com um GridLayoutGroup numa faixa de 100px
        /// de largura (sizeDelta -900 sobre um painel de 1000): a grade só cabia
        /// uma coluna, e os chips empilhavam verticalmente por cima do painel.
        /// Vira uma linha horizontal.
        /// </summary>
        private static void ArrumarSugestoes(Transform t)
        {
            if (t == null) return;

            // Tem de ser LayoutGroup (a base): o GridLayoutGroup NÃO herda de
            // HorizontalOrVerticalLayoutGroup, e procurar só por este deixava a
            // grade viva — o AddComponent abaixo então devolvia null, porque a
            // Unity só aceita um LayoutGroup por objeto.
            var antigo = t.GetComponent<LayoutGroup>();
            var linha = antigo as HorizontalLayoutGroup;

            if (antigo != null && linha == null)
            {
                // Destroy() só some no fim do quadro; aqui o lugar precisa estar
                // livre AGORA, senão o AddComponent falha na mesma chamada.
                Object.DestroyImmediate(antigo);
                antigo = null;
            }

            if (linha == null) linha = t.gameObject.AddComponent<HorizontalLayoutGroup>();
            if (linha == null)
            {
                Debug.LogWarning("[QueryPanelSkin] Não consegui pôr a barra de sugestões em linha.");
                return;
            }

            linha.spacing = 6f;
            linha.padding = new RectOffset(2, 2, 2, 2);
            linha.childAlignment = TextAnchor.MiddleLeft;
            // childControlWidth PRECISA ser true: com false o grupo só posiciona,
            // e todo chip fica com os 80px que nasce do CreateButton — nomes como
            // "FraquezaDistancia" transbordariam da própria caixa. Com true vale
            // o LayoutElement de cada chip, que mede pelo tamanho do texto.
            linha.childControlWidth = true;
            linha.childControlHeight = true;
            linha.childForceExpandWidth = false;
            linha.childForceExpandHeight = false;

            var sug = t.GetComponent<SmartSuggestions>();
            if (sug != null) sug.AplicarPaleta(GrimoireSkin.MadeiraClara, GrimoireSkin.Ouro);
        }

        private static void VestirExecutar(Transform t)
        {
            var btn = t != null ? t.GetComponent<Button>() : null;
            if (btn == null) return;

            GrimoireSkin.VestirBotao(btn);

            foreach (var tmp in btn.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                tmp.text = "EXECUTAR";
                tmp.fontSize = 19f;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 12f;
                tmp.fontSizeMax = 19f;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = GrimoireSkin.Pergaminho;
            }
        }

        private static void VestirRegistro(Transform t)
        {
            var img = t != null ? t.GetComponent<Image>() : null;
            if (img != null) GrimoireSkin.Vestir(img, GrimoireSkin.Pergaminho, GrimoireSkin.OuroEscuro, 10, 3);

            // As cores do terminal foram feitas para fundo escuro; aqui o fundo
            // é pergaminho.
            var terminal = t != null ? t.GetComponentInParent<QueryTerminalUI>() : null;
            if (terminal != null) terminal.UsarPaletaPergaminho();

            // As linhas nasciam coladas na borda do pergaminho. Quem afasta é o
            // próprio terminal: ele é o dono do conteúdo, e procurar o objeto
            // pelo caminho na hierarquia já falhou uma vez.
            if (terminal != null) terminal.AjustarMargem();

            // Sem um Graphic com raycastTarget sob o cursor o ScrollRect não
            // recebe a roda do mouse — mesmo motivo da aba TABELAS.
            var viewport = t != null ? t.Find("Viewport") : null;
            if (viewport != null && viewport.GetComponent<Image>() == null)
            {
                var captura = viewport.gameObject.AddComponent<Image>();
                captura.color = new Color(0f, 0f, 0f, 0f);
                captura.raycastTarget = true;
            }
        }

        private static void VestirDica(Transform t)
        {
            var tmp = t != null ? t.GetComponent<TextMeshProUGUI>() : null;
            if (tmp == null) return;

            // Fica sobre a madeira do painel, então precisa de tom claro
            tmp.color = new Color(0.76f, 0.65f, 0.42f);
            tmp.fontSize = 12f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Posiciona por fração do pai, com y medido a partir do TOPO.</summary>
        private static void PorFracao(Transform t, float x0, float yTop0, float x1, float yTop1)
        {
            if (t is not RectTransform rt) return;
            rt.anchorMin = new Vector2(x0, 1f - yTop1);
            rt.anchorMax = new Vector2(x1, 1f - yTop0);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI Texto(Transform pai, string nome, string texto, float tamanho,
                                             Color cor, TextAlignmentOptions alinhamento,
                                             float x0, float yTop0, float x1, float yTop1)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(pai, false);
            PorFracao(go.transform, x0, yTop0, x1, yTop1);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = texto;
            tmp.fontSize = tamanho;
            tmp.color = cor;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = alinhamento;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            return tmp;
        }

        /// <summary>Busca em profundidade — os nomes podem estar aninhados.</summary>
        private static Transform Buscar(Transform raiz, string nome)
        {
            if (raiz == null) return null;
            if (raiz.name == nome) return raiz;
            for (int i = 0; i < raiz.childCount; i++)
            {
                var achado = Buscar(raiz.GetChild(i), nome);
                if (achado != null) return achado;
            }
            return null;
        }
    }

    /// <summary>
    /// Mantém a barra de custo em dia enquanto o jogador digita. O custo é
    /// previsto pelo ManaSystem — a MESMA conta que vai cobrar de verdade na
    /// execução, então a barra nunca promete um preço e cobra outro.
    /// </summary>
    public class ManaCostMeter : MonoBehaviour
    {
        private static readonly Color Azul     = new Color(0.30f, 0.62f, 0.90f);
        private static readonly Color Vermelho = new Color(0.78f, 0.28f, 0.22f);

        private TMP_InputField _campo;
        private Image _fill;
        private TextMeshProUGUI _rotulo, _valor;

        public void Configurar(TMP_InputField campo, Image fill,
                               TextMeshProUGUI rotulo, TextMeshProUGUI valor)
        {
            _campo = campo;
            _fill = fill;
            _rotulo = rotulo;
            _valor = valor;

            if (_campo != null) _campo.onValueChanged.AddListener(Atualizar);
            if (ManaSystem.Instance != null) ManaSystem.Instance.OnManaChanged += AoMudarMana;

            Atualizar(_campo != null ? _campo.text : "");
        }

        private void OnDestroy()
        {
            if (_campo != null) _campo.onValueChanged.RemoveListener(Atualizar);
            if (ManaSystem.Instance != null) ManaSystem.Instance.OnManaChanged -= AoMudarMana;
        }

        private void OnEnable() => Atualizar(_campo != null ? _campo.text : "");

        // Gastar mana pode transformar um custo viável em inviável, então o aviso
        // precisa acompanhar também o que acontece fora do campo de texto.
        private void AoMudarMana(int atual, int max) => Atualizar(_campo != null ? _campo.text : "");

        private void Atualizar(string consulta)
        {
            if (_fill == null) return;

            var previsao = ManaSystem.Prever(consulta);
            // A largura do rect é o preenchimento: a âncora direita acompanha a
            // fração, e o 9-slice preserva os cantos em qualquer tamanho.
            var rt = _fill.rectTransform;
            rt.anchorMax = new Vector2(previsao.Fracao, 1f);
            rt.offsetMax = new Vector2(-3f, -3f);

            var mana = ManaSystem.Instance;
            bool falta = mana != null && !mana.HasMana(previsao.Custo);

            _fill.color = falta ? Vermelho : Azul;

            if (_rotulo != null)
                _rotulo.text = previsao.EhAnalise ? "CUSTO DA ANALISE" : "CUSTO DA MAGIA";

            if (_valor != null)
            {
                _valor.text = falta
                    ? $"<b>{previsao.Custo}</b> de mana — você tem {mana.CurrentMana}"
                    : $"<b>{previsao.Custo}</b> de mana";
                _valor.color = falta ? new Color(0.95f, 0.72f, 0.66f) : GrimoireSkin.Pergaminho;
            }
        }
    }
}
