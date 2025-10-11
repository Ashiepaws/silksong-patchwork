using System.IO;
using System.Linq;
using UnityEngine;

namespace Patchwork;

/// <summary>
/// Handles dumping of sprites from sprite collections to individual PNG files.
/// </summary>
public static class SpriteDumper
{
    public static string DumpPath { get { return Path.Combine(Plugin.Config.DataBasePath, "Dumps"); } }

    public static void DumpCollection(tk2dSpriteCollectionData collection)
    {
        IOUtil.EnsureDirectoryExists(DumpPath);

        foreach (var mat in collection.materials)
        {
            Texture matTex = mat.mainTexture;
            if (matTex.width == 0 || matTex.height == 0)
            {
                Plugin.Logger.LogWarning($"Skipping material {mat.name} with invalid texture size {matTex.width}x{matTex.height}");
                continue;
            }
            Texture2D rawTex = TexUtil.TransferFromGPU(matTex);

            string matname = mat.name.Split(' ')[0];
            tk2dSpriteDefinition[] spriteDefinitions = [.. collection.spriteDefinitions.Where(def => def.material == mat)];

            foreach (var def in spriteDefinitions)
            {
                var spriteTex = TexUtil.GetCutout(rawTex, SpriteUtil.GetSpriteRect(def, rawTex));
                if (spriteTex == null)
                {
                    Plugin.Logger.LogWarning($"Failed to extract sprite {def.name} from material {matname}");
                    continue;
                }

                if (def.flipped == tk2dSpriteDefinition.FlipMode.Tk2d)
                    spriteTex = TexUtil.RotateCW(spriteTex);
                else if (def.flipped == tk2dSpriteDefinition.FlipMode.TPackerCW)
                    spriteTex = TexUtil.RotateCCW(spriteTex);

                var png = spriteTex.EncodeToPNG();
                IOUtil.EnsureDirectoryExists(Path.Combine(DumpPath, collection.name, matname));
                File.WriteAllBytes(Path.Combine(DumpPath, collection.name, matname, def.name + ".png"), png);
                Plugin.Logger.LogInfo($"Dumped sprite {def.name} from collection {collection.name}, material {matname}");
            }
        }
    }
}