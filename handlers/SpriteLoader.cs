using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Patchwork;

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
            if (!mat.mainTexture.isReadable)
            {
                var unreadableTex = mat.mainTexture;
                mat.mainTexture = FindSpritesheet(collection, matname);
                Object.Destroy(unreadableTex);
                if (Plugin.Config.LogSpriteLoading) Plugin.Logger.LogInfo($"Made texture readable for collection {collection.name}, material {mat.name}");
            }
            var baseTex = mat.mainTexture as Texture2D;

            tk2dSpriteDefinition[] spriteDefinitions = [.. collection.spriteDefinitions.Where(def => def.material == mat)];
            foreach (var def in spriteDefinitions)
            {
                if (def.name == null || def.name.Length == 0)
                    continue; // Skip nameless sprites
                if (LoadedSprites.ContainsKey(collection.name) && LoadedSprites[collection.name].Contains(def.name))
                    continue; // Skip already loaded sprites

                if (!LoadedSprites.ContainsKey(collection.name))
                    LoadedSprites[collection.name] = new HashSet<string>();
                LoadedSprites[collection.name].Add(def.name);

                // Try to find a custom sprite for this definition
                Texture2D spriteTex = FindSprite(collection.name, matname, def.name);
                if (spriteTex == null)
                    continue; // No custom sprite found, will use vanilla

                // Handle flipping modes
                if (def.flipped == tk2dSpriteDefinition.FlipMode.Tk2d)
                    spriteTex = TexUtil.RotateCCW(spriteTex);
                else if (def.flipped == tk2dSpriteDefinition.FlipMode.TPackerCW)
                    spriteTex = TexUtil.RotateCW(spriteTex);

                Rect spriteRect = SpriteUtil.GetSpriteRect(def, baseTex);
                if (spriteRect.width != spriteTex.width || spriteRect.height != spriteTex.height)
                {
                    Plugin.Logger.LogError($"Sprite {collection.name}/{matname}/{def.name} size mismatch: expected {spriteRect.width}x{spriteRect.height}, got {spriteTex.width}x{spriteTex.height}. Resizing, which may cause distortion.");
                    spriteTex = TexUtil.ResizeTexture(spriteTex, (int)spriteRect.width, (int)spriteRect.height);
                }

                Color[] pixels = spriteTex.GetPixels();
                baseTex.SetPixels((int)spriteRect.x, (int)spriteRect.y, (int)spriteRect.width, (int)spriteRect.height, pixels);
                Object.Destroy(spriteTex);
            }
            baseTex.Apply();
            Resources.UnloadUnusedAssets();
            if (Plugin.Config.LogSpriteLoading) Plugin.Logger.LogInfo($"Loaded sprites for collection {collection.name}, material {matname}");
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

    private static Texture2D FindSpritesheet(tk2dSpriteCollectionData collection, string materialName)
    {
        var files = Directory.GetFiles(AtlasLoadPath, $"{materialName}.png", SearchOption.AllDirectories)
            .Where(f => Path.GetDirectoryName(f).EndsWith(collection.name));
        if (files.Any())
            return TexUtil.LoadFromPNG(files.First());
        var mat = collection.materials.FirstOrDefault(m => m.name.StartsWith(materialName + " ") || m.name == materialName);
        return TexUtil.TransferFromGPU(mat?.mainTexture);
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