// Assets/_QueryQuest/UI/PlayerSpriteLibrary.cs
// Carrega as animações do protagonista a partir dos spritesheets fatiados em
// Assets/_QueryQuest/Resources/Sprites/Player/ (o fatiamento é feito
// automaticamente pelo PlayerSpriteSheetImporter, na pasta Editor).
//
// Uso: animator.SetAnimations(PlayerSpriteLibrary.LoadAnimations());

using System;
using System.Collections.Generic;
using UnityEngine;

namespace QueryQuest.UI
{
    public static class PlayerSpriteLibrary
    {
        private const string BasePath = "Sprites/Player/";

        // nome da animação → arquivo do sheet, fps, loop e flipX.
        // Os nomes batem com os defaults do PlayerAnimationController.
        // flipX: os sheets de idle/andando foram desenhados olhando para a ESQUERDA,
        // mas o inimigo fica à direita — espelha só esses (ataque já olha p/ direita,
        // dano e grimorio são frontais).
        // grimorio NÃO loopa: o personagem desliza um pouco ao longo dos frames, então
        // toca uma vez e segura no último frame enquanto o grimório estiver aberto.
        private static readonly (string name, string sheet, float fps, bool loop, bool flipX)[] Defs =
        {
            ("idle",     "idle_sheet",     12f, true,  true),   // 25 frames ~2s respirando
            ("grimorio", "grimorio_sheet", 14f, false, false),  // 36 frames abrindo o livro
            ("ataque",   "ataque_sheet",   18f, false, false),  // 36 frames ~2s de conjuração
            ("dano",     "dano_sheet",     16f, false, false),
            ("andando",  "andando_sheet",  18f, false, true),
        };

        /// <summary>True se os sheets fatiados estão disponíveis em Resources.</summary>
        public static bool HasSprites =>
            Resources.LoadAll<Sprite>(BasePath + "idle_sheet").Length > 0;

        /// <summary>Monta o conjunto completo de animações do protagonista.</summary>
        public static SpriteAnimation[] LoadAnimations()
        {
            var result = new List<SpriteAnimation>();

            foreach (var def in Defs)
            {
                var frames = Resources.LoadAll<Sprite>(BasePath + def.sheet);
                if (frames == null || frames.Length == 0)
                {
                    Debug.LogWarning($"[PlayerSpriteLibrary] Sheet '{def.sheet}' não encontrado " +
                                     $"ou não fatiado em Resources/{BasePath}.");
                    continue;
                }

                // LoadAll não garante ordem — ordena pelo índice no nome (idle_sheet_0, _1, ...)
                Array.Sort(frames, (a, b) => FrameIndex(a.name).CompareTo(FrameIndex(b.name)));

                result.Add(new SpriteAnimation
                {
                    name   = def.name,
                    frames = frames,
                    fps    = def.fps,
                    loop   = def.loop,
                    flipX  = def.flipX
                });
            }

            return result.ToArray();
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
