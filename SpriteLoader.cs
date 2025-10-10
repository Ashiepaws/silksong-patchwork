using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Patchwork;

public static class SpriteLoader
{
    public static string LoadPath { get { return Path.Combine(Plugin.Config.DataBasePath, "load"); } }

    private static readonly Dictionary<string, Texture2D> BuiltAtlases = new();

    public static void LoadCollection(tk2dSpriteCollectionData collection)
    {
        IOUtil.EnsureDirectoryExists(LoadPath);

        if (!Directory.Exists(Path.Combine(LoadPath, collection.name)))
            return;

        foreach (var mat in collection.materials)
        {
            string matname = mat.name.Split(' ')[0];
            if (!Directory.Exists(Path.Combine(LoadPath, collection.name, matname)))
                continue;

            if (BuiltAtlases.TryGetValue(collection.name + matname, out Texture2D cachedTex) && Plugin.Config.CacheAtlases)
            {
                mat.mainTexture = cachedTex;
                Plugin.Logger.LogInfo($"Loaded cached atlas for collection {collection.name}, material {matname}");
                continue;
            }

            Texture matTex = mat.mainTexture;
            if (matTex.width == 0 || matTex.height == 0)
            {
                Plugin.Logger.LogWarning($"Skipping material {mat.name} with invalid texture size {matTex.width}x{matTex.height}");
                continue;
            }
            Texture2D rawTex = TexUtil.TransferFromGPU(matTex);
            tk2dSpriteDefinition[] spriteDefinitions = [.. collection.spriteDefinitions.Where(def => def.material == mat)];

            foreach (var def in spriteDefinitions)
            {
                string spritePath = Path.Combine(LoadPath, collection.name, matname, def.name + ".png");
                if (!File.Exists(spritePath))
                    continue;

                byte[] pngData = File.ReadAllBytes(spritePath);
                Rect spriteRect = SpriteUtil.GetSpriteRect(def, rawTex);

                Texture2D spriteTex = new((int)spriteRect.width, (int)spriteRect.height, TextureFormat.RGBA32, false);
                if (!spriteTex.LoadImage(pngData))
                {
                    Plugin.Logger.LogWarning($"Failed to load sprite image from {spritePath}");
                    continue;
                }

                if (def.flipped == tk2dSpriteDefinition.FlipMode.Tk2d)
                    spriteTex = TexUtil.RotateCCW(spriteTex);
                else if (def.flipped == tk2dSpriteDefinition.FlipMode.TPackerCW)
                    spriteTex = TexUtil.RotateCW(spriteTex);

                Color[] pixels = spriteTex.GetPixels();
                rawTex.SetPixels((int)spriteRect.x, (int)spriteRect.y, (int)spriteRect.width, (int)spriteRect.height, pixels);
                rawTex.Apply();
            }

            if (Plugin.Config.CacheAtlases)
                BuiltAtlases[matname] = rawTex;

            mat.mainTexture = rawTex;
            Plugin.Logger.LogInfo($"Loaded sprites for collection {collection.name}, material {matname}");
        }
    }
    
    public static void InvalidateCacheEntry(string collectionName, string materialName)
    {
        BuiltAtlases.Remove(collectionName + materialName);
    }
}