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

    /// <summary>
    /// Loads sprites for the given sprite collection from individual PNG files.
    /// </summary>
    public static void LoadCollection(tk2dSpriteCollectionData collection)
    {
        // Make sure directories exist
        IOUtil.EnsureDirectoryExists(LoadPath);
        IOUtil.EnsureDirectoryExists(AtlasLoadPath);

        // If collection directory doesn't exist in either sprites or spritesheets, skip it
        if (!Directory.Exists(Path.Combine(LoadPath, collection.name)) && !Directory.Exists(Path.Combine(AtlasLoadPath, collection.name)))
            return;

        foreach (var mat in collection.materials)
        {
            string matname = mat.name.Split(' ')[0];

            // If material directory doesn't exist in sprites and no spritesheet exists, skip it
            if (!Directory.Exists(Path.Combine(LoadPath, collection.name, matname)) && !File.Exists(Path.Combine(AtlasLoadPath, collection.name, matname + ".png")))
                continue;

            // If we have a cached version of this atlas, use it
            if (BuiltAtlases.TryGetValue(collection.name + matname, out Texture2D cachedTex) && Plugin.Config.CacheAtlases)
            {
                mat.mainTexture = cachedTex;
                Plugin.Logger.LogInfo($"Loaded cached atlas for collection {collection.name}, material {matname}");
                continue;
            }

            // Load the base texture (either from spritesheet or from GPU)
            Texture matTex = mat.mainTexture;
            if (matTex.width == 0 || matTex.height == 0)
            {
                Plugin.Logger.LogWarning($"Skipping material {mat.name} with invalid texture size {matTex.width}x{matTex.height}");
                continue;
            }
            bool spritesheetExists = File.Exists(Path.Combine(AtlasLoadPath, collection.name, matname + ".png"));
            Texture2D rawTex = spritesheetExists ? TexUtil.LoadFromPNG(Path.Combine(AtlasLoadPath, collection.name, matname + ".png")) : GetVanillaAtlas(collection, matname);

            // Load and apply each sprite from file if it exists
            tk2dSpriteDefinition[] spriteDefinitions = [.. collection.spriteDefinitions.Where(def => def.material == mat)];
            foreach (var def in spriteDefinitions)
            {
                // If the sprite file doesn't exist, skip it
                string spritePath = Path.Combine(LoadPath, collection.name, matname, def.name + ".png");
                if (!File.Exists(spritePath))
                    continue;

                // Load the sprite image
                byte[] pngData = File.ReadAllBytes(spritePath);
                Rect spriteRect = SpriteUtil.GetSpriteRect(def, rawTex);
                Texture2D spriteTex = new((int)spriteRect.width, (int)spriteRect.height, TextureFormat.RGBA32, false);
                if (!spriteTex.LoadImage(pngData))
                {
                    Plugin.Logger.LogWarning($"Failed to load sprite image from {spritePath}");
                    continue;
                }

                // Handle flipping modes
                if (def.flipped == tk2dSpriteDefinition.FlipMode.Tk2d)
                    spriteTex = TexUtil.RotateCCW(spriteTex);
                else if (def.flipped == tk2dSpriteDefinition.FlipMode.TPackerCW)
                    spriteTex = TexUtil.RotateCW(spriteTex);

                // Blit the sprite into the atlas texture
                Color[] pixels = spriteTex.GetPixels();
                rawTex.SetPixels((int)spriteRect.x, (int)spriteRect.y, (int)spriteRect.width, (int)spriteRect.height, pixels);
                rawTex.Apply();
            }

            // Cache the built atlas if enabled
            if (Plugin.Config.CacheAtlases)
                BuiltAtlases[matname] = rawTex;

            // Apply the built texture to the material
            mat.mainTexture = rawTex;
            Plugin.Logger.LogInfo($"Loaded sprites for collection {collection.name}, material {matname}");
        }
    }

    /// <summary>
    /// Gets the vanilla atlas texture for the given collection and material, caching it for future use
    /// </summary>
    private static Texture2D GetVanillaAtlas(tk2dSpriteCollectionData collection, string materialName)
    {
        if (VanillaAtlases.TryGetValue(collection.name + materialName, out Texture2D cachedTex))
            return cachedTex;
        var mat = collection.materials.FirstOrDefault(m => m.name.StartsWith(materialName + " "));
        var tex = TexUtil.TransferFromGPU(mat?.mainTexture);
        if (tex != null)
            VanillaAtlases[collection.name + materialName] = tex;
        return tex;
    }

    /// <summary>
    /// Invalidates a cached atlas entry for the given collection and material.
    /// </summary>
    public static void InvalidateCacheEntry(string collectionName, string materialName)
    {
        BuiltAtlases.Remove(collectionName + materialName);
    }
}