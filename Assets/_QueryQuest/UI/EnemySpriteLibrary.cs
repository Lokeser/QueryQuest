// Assets/_QueryQuest/UI/EnemySpriteLibrary.cs
// Carrega as animações dos golens a partir dos spritesheets fatiados em
// Assets/_QueryQuest/Resources/Sprites/Enemies/golem_<elemento>/.
// Cada andar tem um golem de elemento diferente — a variante certa é
// escolhida pelo Elemento do inimigo atual (cristais recoloridos).
//
// Uso: animator.SetAnimations(EnemySpriteLibrary.LoadAnimations(enemy.Elemento));

using System;
using System.Collections.Generic;
using UnityEngine;

namespace QueryQuest.UI
{
    public static class EnemySpriteLibrary
    {
        private const string BasePath = "Sprites/Enemies/";

        // nome da animação → arquivo do sheet, fps e loop.
        // Todos os sheets são 5x5 = 25 frames. O golem já olha para a esquerda
        // (em direção ao jogador), então nenhum flip é necessário.
        // colapso NÃO volta pro idle: toca uma vez e congela no último frame (morte).
        private static readonly (string name, string sheet, float fps, bool loop)[] Defs =
        {
            ("idle",    "idle_sheet",    12f, true),
            ("ataque",  "ataque_sheet",  16f, false),
            ("dano",    "dano_sheet",    16f, false),
            ("colapso", "colapso_sheet", 14f, false),
        };

        /// <summary>Pasta da variante do golem conforme o elemento do inimigo.</summary>
        private static string FolderFor(string elemento)
        {
            switch (elemento == null ? "" : elemento.Trim().ToLower())
            {
                case "fogo":  return "golem_fogo";
                case "agua":  return "golem_agua";
                case "terra": return "golem_terra";
                case "vento": return "golem_vento";
                case "raio":  return "golem_raio";
                default:      return "golem_raio"; // roxo original
            }
        }

        /// <summary>True se os sheets fatiados estão disponíveis em Resources.</summary>
        public static bool HasSprites =>
            Resources.LoadAll<Sprite>(BasePath + "golem_raio/idle_sheet").Length > 0;

        /// <summary>Monta as animações do golem do elemento dado.</summary>
        public static SpriteAnimation[] LoadAnimations(string elemento)
        {
            string folder = FolderFor(elemento);
            var result = new List<SpriteAnimation>();

            foreach (var def in Defs)
            {
                var frames = Resources.LoadAll<Sprite>($"{BasePath}{folder}/{def.sheet}");
                if (frames == null || frames.Length == 0)
                {
                    Debug.LogWarning($"[EnemySpriteLibrary] Sheet '{folder}/{def.sheet}' não encontrado " +
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
                    loop   = def.loop
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
