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

    private static readonly Dictionary<string, Texture2D> BuiltAtlases = new();
    private static readonly Dictionary<string, Texture2D> VanillaAtlases = new();

    private static readonly HashSet<string> InvalidatedAtlases = new();

    /// <summary>
    /// Loads sprites for the given sprite collection from individual PNG files.
    /// </summary>
    public static void LoadCollection(tk2dSpriteCollectionData collection)
    {
        // Make sure directories exist
        IOUtil.EnsureDirectoryExists(LoadPath);
        IOUtil.EnsureDirectoryExists(AtlasLoadPath);

        foreach (var mat in collection.materials)
        {
            string matname = mat.name.Split(' ')[0];
            StoreVanillaAtlas(collection, matname);
            CheckInvalidation(collection, matname);

            // If we have a cached version of this atlas, use it
            if (BuiltAtlases.TryGetValue(collection.name + matname, out Texture2D cachedTex) && Plugin.Config.CacheAtlases)
            {
                mat.mainTexture = cachedTex;
                if (Plugin.Config.LogSpriteLoading) Plugin.Logger.LogInfo($"Loaded cached atlas for collection {collection.name}, material {matname}");
                continue;
            }

            Texture2D baseTex = FindSpritesheet(collection, matname) ?? VanillaAtlases[collection.name + matname];
            tk2dSpriteDefinition[] spriteDefinitions = [.. collection.spriteDefinitions.Where(def => def.material == mat)];
            foreach (var def in spriteDefinitions)
            {
                // Try to find a custom sprite for this definition
                Texture2D spriteTex = FindSprite(collection.name, matname, def.name);
                if (spriteTex == null)
                    continue; // No custom sprite found, will use vanilla
                    
                // Handle flipping modes
                if (def.flipped == tk2dSpriteDefinition.FlipMode.Tk2d)
                    spriteTex = TexUtil.RotateCCW(spriteTex);
                else if (def.flipped == tk2dSpriteDefinition.FlipMode.TPackerCW)
                    spriteTex = TexUtil.RotateCW(spriteTex);

                // Blit the sprite into the atlas texture
                Rect spriteRect = SpriteUtil.GetSpriteRect(def, baseTex);
                Color[] pixels = spriteTex.GetPixels();
                baseTex.SetPixels((int)spriteRect.x, (int)spriteRect.y, (int)spriteRect.width, (int)spriteRect.height, pixels);
                baseTex.Apply();
            }

            // Cache the built atlas if enabled
            if (Plugin.Config.CacheAtlases)
                BuiltAtlases[collection.name + matname] = baseTex;

            // Apply the built texture to the material
            mat.mainTexture = baseTex;
            if (Plugin.Config.LogSpriteLoading) Plugin.Logger.LogInfo($"Loaded sprites for collection {collection.name}, material {matname}");
        }
    }

    private static void CheckInvalidation(tk2dSpriteCollectionData collection, string materialName)
    {
        lock (InvalidatedAtlases)
        {
            if (InvalidatedAtlases.Contains(collection.name + materialName))
            {
                BuiltAtlases.Remove(collection.name + materialName);
                InvalidatedAtlases.Remove(collection.name + materialName);
                var mat = collection.materials.FirstOrDefault(m => m.name.StartsWith(materialName + " ") || m.name == materialName);
                mat.mainTexture = VanillaAtlases[collection.name + materialName];
            }
        }
    }

    private static Texture2D FindSprite(string collectionName, string materialName, string spriteName)
    {
        var files = Directory.GetFiles(LoadPath, "*.png", SearchOption.AllDirectories)
            .Where(f => Path.GetFileNameWithoutExtension(f).Equals(spriteName) && Path.GetDirectoryName(f).EndsWith(Path.Combine(collectionName, materialName)));
        if (files.Any())
            return TexUtil.LoadFromPNG(files.First());
        return null;
    }

    private static Texture2D FindSpritesheet(tk2dSpriteCollectionData collection, string materialName)
    {
        var files = Directory.GetFiles(AtlasLoadPath, "*.png", SearchOption.AllDirectories)
            .Where(f => Path.GetFileNameWithoutExtension(f).Equals(materialName) && Path.GetDirectoryName(f).EndsWith(collection.name));
        if (files.Any())
            return TexUtil.LoadFromPNG(files.First());
        return null;
    }

    /// <summary>
    /// Caches the vanilla atlas texture for the given collection and material.
    /// </summary>
    private static void StoreVanillaAtlas(tk2dSpriteCollectionData collection, string materialName)
    {
        if (VanillaAtlases.ContainsKey(collection.name + materialName))
            return;
        var mat = collection.materials.FirstOrDefault(m => m.name.StartsWith(materialName + " ") || m.name == materialName);
        var tex = TexUtil.TransferFromGPU(mat?.mainTexture);
        if (tex != null)
            VanillaAtlases[collection.name + materialName] = tex;
    }

    /// <summary>
    /// Invalidates a cached atlas entry for the given collection and material.
    /// </summary>
    public static void InvalidateCacheEntry(string collectionName, string materialName)
    {
        BuiltAtlases.Remove(collectionName + materialName);
        InvalidatedAtlases.Add(collectionName + materialName);
    }
}