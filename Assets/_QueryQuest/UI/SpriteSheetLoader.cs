// Assets/_QueryQuest/UI/SpriteSheetLoader.cs
// Carrega os frames de um spritesheet fatiado em Resources, na ordem certa.
// Resources.LoadAll NÃO garante ordem, então ordenamos pelo índice do nome
// (<sheet>_0, <sheet>_1, ...) que o SpriteSheetAutoImporter gera.

using System;
using UnityEngine;

namespace QueryQuest.UI
{
    public static class SpriteSheetLoader
    {
        /// <summary>Frames de um sheet em Resources (ex.: "Sprites/VFX/projetil_sheet").</summary>
        public static Sprite[] Load(string resourcePath)
        {
            var frames = Resources.LoadAll<Sprite>(resourcePath);
            if (frames == null || frames.Length == 0)
            {
                Debug.LogWarning($"[SpriteSheetLoader] '{resourcePath}' não encontrado ou não fatiado.");
                return Array.Empty<Sprite>();
            }

            Array.Sort(frames, (a, b) => FrameIndex(a.name).CompareTo(FrameIndex(b.name)));
            return frames;
        }

        private static int FrameIndex(string spriteName)
        {
            int i = spriteName.LastIndexOf('_');
            if (i >= 0 && int.TryParse(spriteName.Substring(i + 1), out int n))
                return n;
            return 0;
        }
    }
}
