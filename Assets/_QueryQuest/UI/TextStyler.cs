// Assets/_QueryQuest/UI/TextStyler.cs
// Aumenta a fonte de TODOS os textos da UI e deixa em negrito.
//
// Boa parte da UI do jogo é criada por código depois que a cena carrega
// (linhas de log, sugestões, slots, cartas de recompensa...), então uma
// passada única não pegaria tudo — este componente revisita a cena de tempos
// em tempos e estiliza só o que ainda não passou (controle por HashSet, para
// nunca escalar o mesmo texto duas vezes).

using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace QueryQuest.UI
{
    public class TextStyler : MonoBehaviour
    {
        /// <summary>Fator de aumento da fonte (+20%).</summary>
        public const float Scale = 1.2f;

        private const float RescanInterval = 0.35f;

        private readonly HashSet<int> _styled = new HashSet<int>();
        private readonly List<Transform> _ignoreRoots = new List<Transform>();
        private float _timer;

        /// <summary>Subárvores que se estilizam sozinhas (ex.: o registro de combate).</summary>
        public void Ignore(Transform root)
        {
            if (root != null) _ignoreRoots.Add(root);
        }

        private void Update()
        {
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;
            _timer = RescanInterval;
            Restyle();
        }

        public void Restyle()
        {
            var texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in texts)
            {
                if (t == null) continue;
                if (!_styled.Add(t.GetInstanceID())) continue;   // já estilizado
                if (IsIgnored(t.transform)) continue;

                var fonte = FonteRPG(t.font);
                if (fonte != null) t.font = fonte;

                t.fontSize *= Scale;
                if (t.enableAutoSizing)
                {
                    t.fontSizeMin *= Scale;
                    t.fontSizeMax *= Scale;
                }
                t.fontStyle |= FontStyles.Bold;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // FONTE
        // ─────────────────────────────────────────────────────────────────────

        // Serifadas de "livro antigo": clima de RPG sem sacrificar a legibilidade
        // (as decorativas de verdade, tipo Old English, somem em fonte pequena).
        private static readonly string[] Preferidas =
        {
            "Palatino Linotype", "Book Antiqua", "Constantia", "Garamond",
            "Georgia", "Cambria", "Times New Roman"
        };

        private static TMP_FontAsset _fonte;
        private static bool _tentou;

        /// <summary>
        /// A fonte escolhida (null se nenhuma preferida existir). O registro de
        /// combate usa isto para aplicar a fonte ANTES de medir a altura das
        /// linhas — medir com uma fonte e desenhar com outra deixaria tudo torto.
        /// </summary>
        public static TMP_FontAsset Fonte => FonteRPG(null);

        /// <summary>
        /// Monta (uma vez) a fonte a partir das instaladas no sistema. Nada é
        /// copiado para o projeto — se nenhuma existir, mantém a fonte original.
        /// </summary>
        private static TMP_FontAsset FonteRPG(TMP_FontAsset original)
        {
            if (_tentou) return _fonte;
            _tentou = true;

            try
            {
                // A lista pode vir vazia (acontece rodando em batch); nesse caso
                // tentamos criar pelo nome mesmo assim.
                var nomes = Font.GetOSInstalledFontNames();
                var instaladas = new HashSet<string>(nomes ?? new string[0]);

                foreach (var nome in Preferidas)
                {
                    if (instaladas.Count > 0 && !instaladas.Contains(nome)) continue;

                    // Sobrecarga própria para fontes do SISTEMA. A versão que
                    // recebe um Font dinâmico devolve null: o TMP não consegue
                    // montar o atlas sem o arquivo da fonte.
                    var asset = TMP_FontAsset.CreateFontAsset(nome, "Regular", 90);
                    if (asset == null || asset.material == null) continue;

                    // Guarda a fonte antiga como reserva para glifos que faltarem
                    if (original != null)
                    {
                        asset.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
                        asset.fallbackFontAssetTable.Add(original);
                    }

                    asset.name = $"{nome} SDF";
                    _fonte = asset;
                    Debug.Log($"[TextStyler] Fonte da UI: {nome}");
                    break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TextStyler] Não consegui montar a fonte: {e.Message}");
                _fonte = null;
            }

            if (_fonte == null) Debug.LogWarning("[TextStyler] Nenhuma fonte preferida instalada; mantendo a original.");
            return _fonte;
        }

        private bool IsIgnored(Transform t)
        {
            foreach (var root in _ignoreRoots)
                if (root != null && t.IsChildOf(root)) return true;
            return false;
        }
    }
}
