// Assets/_QueryQuest/Editor/SpriteSheetAutoImporter.cs
// Configura automaticamente os sprites do jogo na importação.
//
// SPRITESHEETS (fatiados em grade uniforme; a grade é calculada pelo tamanho
// do PNG, então novos sheets com a mesma célula funcionam sem mexer aqui).
// Frames nomeados <sheet>_0 ... <sheet>_N (esquerda→direita, cima→baixo) —
// a ordem que as *SpriteLibrary usam em runtime.
//
//   Player  (célula 420x700) → grades 5x5 / 6x6, pivô na base
//   Enemies (célula 275x298) → grade  5x5,      pivô na base
//   Spirit  (célula 266x317) → grade  5x5,      pivô no centro (flutua)
//   VFX     (célula 266x317) → grade  6x6,      pivô no centro (projétil/explosão)
//
// IMAGENS ÚNICAS
//   UI          → sprite único com bordas 9-slice (molduras esticam sem deformar)
//   Backgrounds → sprite único (cenário de cada andar)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace QueryQuest.EditorTools
{
    public class SpriteSheetAutoImporter : AssetPostprocessor
    {
        private const string Root = "Assets/_QueryQuest/Resources/Sprites/";

        private struct SheetConfig
        {
            public string folder;
            public int cellW, cellH;
            public bool bottomPivot;
        }

        private static readonly SheetConfig[] Sheets =
        {
            new SheetConfig { folder = Root + "Player",  cellW = 420, cellH = 700, bottomPivot = true  },
            new SheetConfig { folder = Root + "Enemies", cellW = 275, cellH = 298, bottomPivot = true  },
            new SheetConfig { folder = Root + "Spirit",  cellW = 266, cellH = 317, bottomPivot = false },
            new SheetConfig { folder = Root + "VFX",     cellW = 266, cellH = 317, bottomPivot = false },
        };

        // Molduras de HUD: fração da largura/altura reservada para os cantos
        // ornamentados no 9-slice (o miolo é o que estica). Serve para as
        // molduras "quadradas" (hp/mana, slots, painel...); banners bem mais
        // largos que altos têm cantos proporcionalmente bem menores, então
        // usam a tabela BordasEspecificas abaixo em vez desta fração genérica.
        private const float UiBorderFraction = 0.28f;

        /// <summary>
        /// Bordas exatas (em pixels da própria arte), para imagens em que a
        /// fração genérica erra feio. Vector4 = (esquerda, baixo, direita, cima).
        /// </summary>
        private static readonly Dictionary<string, Vector4> BordasEspecificas = new Dictionary<string, Vector4>
        {
        };

        /// <summary>
        /// Artes que NUNCA são esticadas: entram com Image.Type.Simple e
        /// preserveAspect, mantendo a proporção original. Marcar borda de
        /// 9-slice nelas não faria nada além de confundir quem for mexer depois.
        /// hud_baixo tem um arco no meio e as caixas dos botões já desenhadas —
        /// qualquer esticão deformaria o arco e desalinharia as caixas.
        /// </summary>
        private static readonly HashSet<string> SemBorda = new HashSet<string>
        {
            "hud_baixo", "hud_botao_mov", "hud_botao_turno",
        };

        private void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.StartsWith(Root)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType         = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled       = false;
            importer.alphaIsTransparency = true;
            importer.filterMode          = FilterMode.Bilinear;
            importer.maxTextureSize      = 4096;

            if (!TryReadPngSize(path, out int texW, out int texH)) return;

            // ── Imagens únicas (UI / Backgrounds) ────────────────────────────
            if (path.StartsWith(Root + "UI") || path.StartsWith(Root + "Backgrounds"))
            {
                importer.spriteImportMode = SpriteImportMode.Single;

                if (path.StartsWith(Root + "UI"))
                {
                    // Sem compressão: a HUD é pouca coisa em memória e qualquer
                    // artefato de compressão fica óbvio numa moldura esticada
                    // em tela cheia (é o que causava a perda de qualidade).
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.crunchedCompression = false;

                    string uiName = Path.GetFileNameWithoutExtension(path);
                    if (SemBorda.Contains(uiName))
                    {
                        importer.spriteBorder = Vector4.zero;
                    }
                    else if (BordasEspecificas.TryGetValue(uiName, out var borda))
                    {
                        importer.spriteBorder = borda;
                    }
                    else
                    {
                        // Bordas do 9-slice preservam os cantos dourados ao esticar
                        int bx = Mathf.RoundToInt(texW * UiBorderFraction);
                        int by = Mathf.RoundToInt(texH * UiBorderFraction);
                        importer.spriteBorder = new Vector4(bx, by, bx, by);
                    }
                }
                return;
            }

            // ── Spritesheets fatiados em grade ───────────────────────────────
            int cellW = 0, cellH = 0;
            bool bottomPivot = true;
            foreach (var cfg in Sheets)
            {
                if (path.StartsWith(cfg.folder))
                {
                    cellW = cfg.cellW;
                    cellH = cfg.cellH;
                    bottomPivot = cfg.bottomPivot;
                    break;
                }
            }
            if (cellW == 0) return;

            importer.spriteImportMode = SpriteImportMode.Multiple;

            int cols = texW / cellW;
            int rows = texH / cellH;
            int expected = cols * rows;
            if (expected == 0) return;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(assetImporter);
            provider.InitSpriteEditorDataProvider();

            // Já fatiado com a contagem certa? Não refatia (preserva os GUIDs dos sprites)
            if (provider.GetSpriteRects().Length == expected) return;

            string baseName = Path.GetFileNameWithoutExtension(path);
            var rects = new List<SpriteRect>();
            int index = 0;

            for (int row = 0; row < rows; row++)      // linha 0 = topo do sheet
            {
                for (int col = 0; col < cols; col++)
                {
                    rects.Add(new SpriteRect
                    {
                        name      = $"{baseName}_{index}",
                        spriteID  = GUID.Generate(),
                        rect      = new Rect(col * cellW, texH - (row + 1) * cellH, cellW, cellH),
                        alignment = bottomPivot ? SpriteAlignment.BottomCenter : SpriteAlignment.Center,
                        pivot     = bottomPivot ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f),
                    });
                    index++;
                }
            }

            provider.SetSpriteRects(rects.ToArray());

            // Unity 2021.2+: tabela nome→fileId precisa acompanhar os rects
            var nameFileId = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameFileId != null)
            {
                nameFileId.SetNameFileIdPairs(
                    rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
            }

            provider.Apply();
            Debug.Log($"[SpriteSheetAutoImporter] '{path}' fatiado em {cols}x{rows} = {expected} frames.");
        }

        // Lê largura/altura direto do cabeçalho IHDR do PNG (no OnPreprocessTexture
        // a textura ainda não foi importada, então não dá para perguntar ao Unity).
        private static bool TryReadPngSize(string path, out int width, out int height)
        {
            width = 0;
            height = 0;
            try
            {
                using (var fs = File.OpenRead(path))
                {
                    var buf = new byte[24];
                    if (fs.Read(buf, 0, 24) < 24) return false;
                    width  = (buf[16] << 24) | (buf[17] << 16) | (buf[18] << 8) | buf[19];
                    height = (buf[20] << 24) | (buf[21] << 16) | (buf[22] << 8) | buf[23];
                }
                return width > 0 && height > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
