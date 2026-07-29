// Assets/_QueryQuest/UI/ElementPalette.cs
// Cor de cada elemento das magias. Usada para tingir os VFX
// (projétil/explosão são desenhados em tons de cinza justamente
// para receberem essa cor por multiplicação).

using UnityEngine;

namespace QueryQuest.UI
{
    public static class ElementPalette
    {
        public static Color For(string elemento)
        {
            switch (elemento == null ? "" : elemento.Trim().ToLower())
            {
                case "fogo":  return new Color(1.00f, 0.42f, 0.16f);
                case "agua":  return new Color(0.30f, 0.65f, 1.00f);
                case "vento": return new Color(0.45f, 0.95f, 0.55f);
                case "terra": return new Color(0.95f, 0.70f, 0.30f);
                case "raio":  return new Color(0.78f, 0.55f, 1.00f);
                default:      return Color.white;
            }
        }

        /// <summary>Escala do VFX pelo nível da magia: 0.75x (1), 1.2x (2), 1.8x (3+).</summary>
        public static float ScaleForLevel(int nivel)
        {
            if (nivel <= 1) return 0.75f;
            if (nivel == 2) return 1.2f;
            return 1.8f;
        }
    }
}
