// Assets/_QueryQuest/UI/DocsUI.cs
// A aba DOCS é majoritamente estática — conteúdo configurado direto no Prefab/Inspector.
// Este script gerencia apenas o sistema de busca/filtro dos exemplos se desejado.
//
// Hierarquia mínima em PanelDocs:
//
// PanelDocs  (este script — opcional, pode ser só conteúdo estático)
// └── DocsScrollView
//     └── Viewport
//         └── DocsContent
//             ├── SectionSelect     (conteúdo estático TMP — blocos de texto)
//             ├── SectionWhere      (conteúdo estático TMP)
//             ├── SectionOperators  (conteúdo estático TMP)
//             └── SectionColumns    (conteúdo estático TMP)
//
// Para uma implementação sem código, basta criar o texto como TextMeshProUGUI
// com rich text habilitado e usar as tags <color=#a89fd8> para destacar keywords.

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QueryQuest.UI
{
    /// <summary>
    /// Aba de documentação SQL do grimório.
    /// Popula os blocos de texto com a referência de comandos programaticamente
    /// para evitar digitação manual no Inspector.
    /// </summary>
    public class DocsUI : MonoBehaviour
    {
        [Header("Blocos de Conteúdo (TextMeshProUGUI)")]
        [SerializeField] private TextMeshProUGUI blockSelect;
        [SerializeField] private TextMeshProUGUI blockWhere;
        [SerializeField] private TextMeshProUGUI blockOperators;
        [SerializeField] private TextMeshProUGUI blockColumns;
        [SerializeField] private TextMeshProUGUI blockExamples;

        // Cores usadas nos exemplos (rich text TMP)
        private const string C_KEYWORD = "#a89fd8"; // roxo — palavras-chave SQL
        private const string C_TABLE = "#5dcaa5"; // verde — nomes de tabelas
        private const string C_VALUE = "#ef9f27"; // âmbar — valores
        private const string C_MUTED = "#534ab7"; // roxo escuro — comentários

        private void OnEnable()
        {
            PopulateContent();
        }

        private void PopulateContent()
        {
            if (blockSelect != null)
                blockSelect.text = Fmt(
                    "<color={0}>SELECT</color> * <color={0}>FROM</color> <color={1}>Magias</color>",
                    K, T) + "\n" +
                    Desc("retorna todos os registros da tabela.");

            if (blockWhere != null)
                blockWhere.text =
                    Fmt("<color={0}>SELECT</color> * <color={0}>FROM</color> <color={1}>Magias</color> <color={0}>WHERE</color> Elemento = <color={2}>'Fogo'</color>", K, T, V) + "\n" +
                    Desc("filtra por valor exato. Strings usam aspas simples.") + "\n\n" +
                    Fmt("<color={0}>SELECT</color> * <color={0}>FROM</color> <color={1}>Magias</color> <color={0}>WHERE</color> Nivel >= <color={2}>2</color>", K, T, V) + "\n" +
                    Desc("filtra por número. Sem aspas para valores numéricos.");

            if (blockOperators != null)
                blockOperators.text =
                    Fmt("=   igual       !=  diferente") + "\n" +
                    Fmt(">   maior       <   menor") + "\n" +
                    Fmt(">=  maior/igual <=  menor/igual") + "\n" +
                    Fmt("<color={0}>AND</color>  ambas condições verdadeiras", K) + "\n" +
                    Fmt("<color={0}>OR</color>   pelo menos uma verdadeira", K);

            if (blockColumns != null)
                blockColumns.text =
                    Fmt("<color={1}>Magias</color>: Id | Nome | Elemento | Nivel | Distancia | DanoBase | Desbloqueado", K, T) + "\n" +
                    Fmt("<color={1}>Inimigos</color>: Id | Nome | Elemento | HP | Nivel | FraquezaElemento", K, T);

            if (blockExamples != null)
                blockExamples.text =
                    Fmt("<color={0}>SELECT</color> * <color={0}>FROM</color> <color={1}>Magias</color> <color={0}>WHERE</color> Elemento = <color={2}>'Agua'</color> <color={0}>AND</color> Distancia = <color={2}>'LONGO'</color>", K, T, V) + "\n" +
                    Desc("seleciona feitiço de Água para longa distância.") + "\n\n" +
                    Fmt("<color={0}>SELECT</color> * <color={0}>FROM</color> <color={1}>Inimigos</color> <color={0}>WHERE</color> FraquezaElemento = <color={2}>'Fogo'</color>", K, T, V) + "\n" +
                    Desc("encontra inimigos fracos ao Fogo.");
        }

        // ─── Helpers de formatação ────────────────────────────────────────────

        private const string K = C_KEYWORD;
        private const string T = C_TABLE;
        private const string V = C_VALUE;

        private static string Fmt(string template, params string[] args)
        {
            // Substitui {0},{1},{2} manualmente para evitar conflito com tags TMP
            string result = template;
            for (int i = 0; i < args.Length; i++)
                result = result.Replace("{" + i + "}", args[i]);
            return result;
        }

        private static string Desc(string text)
            => $"<color={C_MUTED}><size=11>  {text}</size></color>";
    }
}