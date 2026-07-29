// Assets/_QueryQuest/UI/UiFrame.cs
// Aplica uma moldura (arte de HUD) num Image de UI.
//
// As molduras são PNGs grandes (~1500px) com cantos dourados ornamentados.
// Elas são importadas com bordas de 9-slice em PIXELS DA ARTE, que seriam
// desenhadas gigantes num painel pequeno. O pixelsPerUnitMultiplier é
// calculado aqui para que as bordas ocupem uma fração previsível do painel —
// os cantos ficam nítidos e o miolo (pergaminho) estica.

using UnityEngine;
using UnityEngine.UI;

namespace QueryQuest.UI
{
    public static class UiFrame
    {
        /// <summary>
        /// Aplica o sprite como moldura esticável.
        /// middleFraction = quanto do painel é miolo esticado (o resto são as bordas).
        /// </summary>
        public static void Apply(Image img, Sprite sprite, float middleFraction = 0.55f,
                                 bool fillCenter = true)
        {
            if (img == null || sprite == null) return;

            img.sprite     = sprite;
            img.type       = Image.Type.Sliced;
            img.fillCenter = fillCenter;   // false = só a borda, miolo transparente
            img.color      = Color.white;

            var rt = img.transform as RectTransform;
            if (rt == null) return;

            float borderX = sprite.border.x + sprite.border.z;
            float borderY = sprite.border.y + sprite.border.w;
            float room    = Mathf.Max(0.05f, 1f - middleFraction);

            float multiplier = 1f;
            if (rt.rect.width  > 1f && borderX > 0f)
                multiplier = Mathf.Max(multiplier, borderX / (rt.rect.width  * room));
            if (rt.rect.height > 1f && borderY > 0f)
                multiplier = Mathf.Max(multiplier, borderY / (rt.rect.height * room));

            img.pixelsPerUnitMultiplier = multiplier;
        }
    }
}
