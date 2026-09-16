// Assets/_QueryQuest/UI/TextOutline.cs
// Contorno nas fontes do registro de combate e do grimório.
//
// O caminho óbvio (tmp.outlineWidth) instancia um material NOVO por texto —
// com dezenas de linhas de log isso vira dezenas de draw calls. Aqui o material
// com contorno é criado UMA vez por combinação (material base + cor + largura)
// e compartilhado via fontSharedMaterial, então o batching continua valendo.
//
// A largura é deliberadamente pequena: no TMP o contorno dilata o glifo a
// partir da borda do SDF, e passar do ponto engrossa a letra a ponto de ela
// encostar na moldura. 0,10–0,14 dá o realce sem mexer no espaço ocupado
// (o contorno não altera a métrica do texto, então nada transborda por causa
// dele — o layout continua exatamente o mesmo).

using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace QueryQuest.UI
{
    public static class TextOutline
    {
        /// <summary>Contorno claro, para tinta escura sobre pergaminho.</summary>
        public static readonly Color Claro = new Color(0.98f, 0.94f, 0.82f, 1f);

        /// <summary>Contorno escuro, para texto claro/dourado sobre madeira.</summary>
        public static readonly Color Escuro = new Color(0.09f, 0.05f, 0.02f, 1f);

        public const float Largura = 0.12f;

        private static readonly Dictionary<string, Material> _cache = new Dictionary<string, Material>();

        /// <summary>
        /// Material com contorno → material original. Sem isto, reaplicar (o log
        /// troca de paleta depois de já ter linhas na tela) empilharia contorno
        /// sobre contorno e travaria a cor na primeira escolhida.
        /// </summary>
        private static readonly Dictionary<Material, Material> _origem = new Dictionary<Material, Material>();

        /// <summary>
        /// Escolhe o contorno pelo tom do texto: letra escura ganha contorno
        /// claro, letra clara ganha contorno escuro. Sempre o oposto, senão o
        /// contorno some dentro da própria letra.
        /// </summary>
        public static void Aplicar(TMP_Text tmp, float largura = Largura)
        {
            if (tmp == null) return;
            Aplicar(tmp, Luminancia(tmp.color) > 0.5f ? Escuro : Claro, largura);
        }

        public static void Aplicar(TMP_Text tmp, Color cor, float largura)
        {
            if (tmp == null) return;

            var baseMat = tmp.fontSharedMaterial;
            if (baseMat == null) return;

            // Se já tem contorno, parte do material ORIGINAL — assim uma segunda
            // chamada troca a cor do contorno em vez de contornar o contorno.
            if (_origem.TryGetValue(baseMat, out var original) && original != null)
                baseMat = original;

            string chave = $"{baseMat.GetInstanceID()}|{cor}|{largura:F3}";
            if (!_cache.TryGetValue(chave, out var mat) || mat == null)
            {
                mat = new Material(baseMat) { name = $"~contorno {baseMat.name}" };
                mat.SetFloat(ShaderUtilities.ID_OutlineWidth, largura);
                mat.SetColor(ShaderUtilities.ID_OutlineColor, cor);
                _cache[chave] = mat;
                _origem[mat] = baseMat;
            }

            tmp.fontSharedMaterial = mat;
        }

        private static float Luminancia(Color c) => c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
    }
}
