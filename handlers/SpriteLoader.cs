using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Patchwork.Util;

namespace Patchwork.Handlers;

/// <summary>
/// Handles loading of sprites from individual PNG files into sprite collections.
/// </summary>
public static class SpriteLoader
{
    public static string LoadPath { get { return Path.Combine(Plugin.Config.DataBasePath, "Sprites"); } }
    public static string AtlasLoadPath { get { return Path.Combine(Plugin.Config.DataBasePath, "Spritesheets"); } }

    private static readonly Dictionary<string, HashSet<string>> LoadedSprites = new();

    /// <summary>
    /// Loads sprites for the given sprite collection from individual PNG files.
    /// </summary>
    public static void LoadCollection(tk2dSpriteCollectionData collection)
    {
        foreach (var mat in collection.materials)
        {
            string matname = mat.name.Split(' ')[0];
            if (!mat.mainTexture.isReadable || mat.mainTexture is not RenderTexture)
            {
                var unreadableTex = mat.mainTexture;
                mat.mainTexture = FindSpritesheet(collection, matname);
                Object.Destroy(unreadableTex);
                if (Plugin.Config.LogSpriteLoading) Plugin.Logger.LogInfo($"Made texture readable for collection {collection.name}, material {mat.name}");
            }

            var previous = RenderTexture.active;
            RenderTexture.active = mat.mainTexture as RenderTexture;
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, mat.mainTexture.width, mat.mainTexture.height, 0);

            tk2dSpriteDefinition[] spriteDefinitions = [.. collection.spriteDefinitions.Where(def => def.material == mat)];

            foreach (var def in spriteDefinitions)
            {
                if (string.IsNullOrEmpty(def.name)) continue;
                if (!LoadedSprites.ContainsKey(collection.name))
                    LoadedSprites[collection.name] = new HashSet<string>();
                if (!LoadedSprites[collection.name].Add(def.name)) continue;

                Texture2D spriteTex = FindSprite(collection.name, matname, def.name);
                if (spriteTex == null) continue;

                Rect spriteRect = SpriteUtil.GetSpriteRect(def, mat.mainTexture);
                spriteRect.y = mat.mainTexture.height - spriteRect.y - spriteRect.height;
                Plugin.Logger.LogInfo($"Loaded sprite {def.name} for collection {collection.name}, material {mat.name}");

                Vector2 uBasis, vBasis;
                switch (def.flipped)
                {
                    case tk2dSpriteDefinition.FlipMode.Tk2d:
                        uBasis = Vector2.down; vBasis = Vector2.right;
                        break;

                    case tk2dSpriteDefinition.FlipMode.TPackerCW:
                        uBasis = Vector2.up; vBasis = Vector2.left;
                        break;

                    default:
                        uBasis = Vector2.right; vBasis = Vector2.up;
                        break;
                }

                TexUtil.RotateMaterial.SetVector("_Basis", new Vector4(uBasis.x, uBasis.y, vBasis.x, vBasis.y));

                Graphics.DrawTextureImpl(spriteRect, spriteTex, new Rect(0, 0, 1, 1), 0, 0, 0, 0, Color.white, TexUtil.RotateMaterial, 0);
            }

            mat.mainTexture.IncrementUpdateCount();
            GL.PopMatrix();
            RenderTexture.active = previous;
        }
    }

    private static Texture2D FindSprite(string collectionName, string materialName, string spriteName)
    {
        var files = Directory.GetFiles(LoadPath, $"{spriteName}.png", SearchOption.AllDirectories)
            .Where(f => Path.GetDirectoryName(f).EndsWith(Path.Combine(collectionName, materialName)));
        if (files.Any())
            return TexUtil.LoadFromPNG(files.First());
        return null;
    }

    private static RenderTexture FindSpritesheet(tk2dSpriteCollectionData collection, string materialName)
    {
        var files = Directory.GetFiles(AtlasLoadPath, $"{materialName}.png", SearchOption.AllDirectories)
            .Where(f => Path.GetDirectoryName(f).EndsWith(collection.name));
        if (files.Any())
        {
            var tex2d = TexUtil.LoadFromPNG(files.First());
            RenderTexture rt = RenderTexture.GetTemporary(tex2d.width, tex2d.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(tex2d, rt);
            Object.Destroy(tex2d);
            return rt;
        }
        var mat = collection.materials.FirstOrDefault(m => m.name.StartsWith(materialName + " ") || m.name == materialName);
        return TexUtil.GetReadable(mat?.mainTexture);
    }

    public static void InvalidateCacheEntry(string collectionName, string spriteName)
    {
        if (LoadedSprites.ContainsKey(collectionName))
        {
            if (LoadedSprites[collectionName].Contains(spriteName))
                LoadedSprites[collectionName].Remove(spriteName);
        }
    }
    
    public static void InvalidateCacheForCollection(string collectionName)
    {
        if (LoadedSprites.ContainsKey(collectionName))
            LoadedSprites.Remove(collectionName);
    }
}