using System.IO;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using Patchwork.Util;
using System.Collections.Generic;

namespace Patchwork.Handlers;

[HarmonyPatch]
public static class T2DHandler
{
    public static string T2DDumpPath { get { return Path.Combine(SpriteDumper.DumpPath, "T2D"); } }

    private static readonly Dictionary<string, Sprite> LoadedT2DSprites = new();
    private static readonly Dictionary<string, HashSet<string>> SpriteAtlasMap = new();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SpriteRenderer), nameof(SpriteRenderer.sprite), MethodType.Setter)]
    public static void SetSpritePostfix(SpriteRenderer __instance, Sprite value)
    {
        if (__instance == null || value == null || __instance.gameObject.name == "TempSpriteRenderer")
            return;

        if (Plugin.Config.DumpSprites && !string.IsNullOrEmpty(value.name) && !string.IsNullOrEmpty(value.texture.name))
            HandleDump(__instance, value);

        if (Plugin.Config.LoadSprites)
            HandleLoad(__instance, value);
    }

    public static void InvalidateCache(string spriteName)
    {
        if (LoadedT2DSprites.Remove(spriteName))
            Plugin.Logger.LogDebug($"Invalidated T2D sprite cache for {spriteName}");
        if (SpriteAtlasMap.ContainsKey(spriteName))
        {
            foreach (var sprName in SpriteAtlasMap[spriteName])
            {
                if (LoadedT2DSprites.Remove(sprName))
                    Plugin.Logger.LogDebug($"Invalidated T2D sprite cache for {sprName} due to atlas change {spriteName}");
            }
            SpriteAtlasMap.Remove(spriteName);
        }
    }

    private static void HandleLoad(SpriteRenderer spriteRenderer, Sprite sprite)
    {
        if (LoadedT2DSprites.ContainsKey(sprite.name))
        {
            spriteRenderer.sprite = LoadedT2DSprites[sprite.name];
            return;
        }

        if (sprite.texture.name.Contains("-BC7-") || sprite.texture.name.Contains("DXT5|BC3-"))
        {
            Texture2D spriteTex = FindT2DSprite(CleanTextureName(sprite.texture.name), sprite.name);
            if (spriteTex == null)
                return;

            Sprite newSprite = Sprite.Create(spriteTex, new Rect(0, 0, spriteTex.width, spriteTex.height), new Vector2(0.5f, 0.5f), sprite.pixelsPerUnit);
            LoadedT2DSprites[sprite.name] = newSprite;
            spriteRenderer.sprite = newSprite;
            if (Plugin.Config.LogSpriteLoading)
                Plugin.Logger.LogInfo($"Loaded T2D sprite {sprite.name} (Atlas texture {sprite.texture.name})");

            if(!SpriteAtlasMap.ContainsKey(sprite.texture.name))
                SpriteAtlasMap[sprite.texture.name] = new HashSet<string>();
            SpriteAtlasMap[sprite.texture.name].Add(sprite.name);
        }
        else
        {
            if (LoadedT2DSprites.ContainsKey(sprite.texture.name))
            {
                spriteRenderer.sprite = LoadedT2DSprites[sprite.texture.name];
                return;
            }
            
            Texture2D spriteTex = FindT2DSprite(sprite.texture.name);
            if (spriteTex == null)
                return;
            Sprite newSprite = Sprite.Create(spriteTex, new Rect(0, 0, spriteTex.width, spriteTex.height), new Vector2(0.5f, 0.5f), sprite.pixelsPerUnit);
            LoadedT2DSprites[sprite.texture.name] = newSprite;
            spriteRenderer.sprite = newSprite;
            if (Plugin.Config.LogSpriteLoading)
                Plugin.Logger.LogInfo($"Loaded T2D texture {sprite.texture.name} (Standalone texture)");
        }
    }

    private static void HandleDump(SpriteRenderer spriteRenderer, Sprite sprite)
    {
        if (sprite.texture.name.Contains("-BC7-") || sprite.texture.name.Contains("DXT5|BC3-"))
        {
            string cleanName = CleanTextureName(sprite.texture.name);
            string saveDir = Path.Combine(T2DDumpPath, cleanName);
            IOUtil.EnsureDirectoryExists(saveDir);
            string savePath = Path.Combine(saveDir, sprite.name + ".png");

            if (File.Exists(savePath))
                return;

            int width = (int)sprite.rect.width;
            int height = (int)sprite.rect.height;
            int renderLayer = 31;

            GameObject spriteGO = new GameObject("TempSpriteRenderer");
            var tempSpriteRenderer = spriteGO.AddComponent<SpriteRenderer>();
            tempSpriteRenderer.sprite = sprite;
            spriteGO.layer = renderLayer;
            spriteGO.transform.position = Vector3.zero;

            GameObject camGO = new GameObject("TempCamera");
            Camera cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.orthographic = true;
            cam.cullingMask = 1 << renderLayer;
            cam.orthographicSize = height / sprite.pixelsPerUnit / 2f;
            cam.transform.position = new Vector3(0, 0, -10);

            RenderTexture rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Point;
            cam.targetTexture = rt;

            cam.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D spriteTex = new Texture2D(width, height, TextureFormat.ARGB32, false);
            spriteTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            spriteTex.Apply();

            RenderTexture.active = previous;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(spriteGO);
            Object.DestroyImmediate(camGO);

            byte[] pngData = spriteTex.EncodeToPNG();
            File.WriteAllBytes(savePath, pngData);
            if (Plugin.Config.LogSpriteDumping)
                Plugin.Logger.LogInfo($"Dumped T2D sprite {sprite.name} from texture {sprite.texture.name} to {savePath}");
        }
        else
        {
            RenderTexture spriteTex = TexUtil.GetReadable(sprite.texture);
            Texture2D readableTex = new Texture2D(spriteTex.width, spriteTex.height, TextureFormat.ARGB32, false);
            var previous = RenderTexture.active;
            RenderTexture.active = spriteTex;
            readableTex.ReadPixels(new Rect(0, 0, spriteTex.width, spriteTex.height), 0, 0);
            readableTex.Apply();
            RenderTexture.active = previous;

            byte[] pngData = readableTex.EncodeToPNG();
            string savePath = Path.Combine(T2DDumpPath, sprite.texture.name + ".png");
            if (File.Exists(savePath))
                return;
            File.WriteAllBytes(savePath, pngData);
            if (Plugin.Config.LogSpriteDumping)
                Plugin.Logger.LogInfo($"Dumped T2D texture {sprite.texture.name} to {savePath}");
        }
    }

    private static Texture2D FindT2DSprite(string spriteName)
    {
        var files = Directory.GetFiles(SpriteLoader.LoadPath, spriteName + ".png", SearchOption.AllDirectories)
            .Where(f => Path.GetDirectoryName(f).EndsWith("T2D"));
        if (files.Any())
            return TexUtil.LoadFromPNG(files.First());
        return null;
    }

    private static Texture2D FindT2DSprite(string texName, string spriteName)
    {
        var files = Directory.GetFiles(SpriteLoader.LoadPath, spriteName + ".png", SearchOption.AllDirectories)
            .Where(f => Path.GetDirectoryName(f).EndsWith(Path.Combine("T2D", texName)));
        if (files.Any())
            return TexUtil.LoadFromPNG(files.First());
        return null;
    }

    private static string CleanTextureName(string textureName)
    {
        if (textureName.Contains("-BC7-"))
        {
            string cleanName = textureName.Split(["-BC7-"], System.StringSplitOptions.None)[1];
            cleanName = string.Join("-", cleanName.Split('-').Take(cleanName.Split('-').Length - 1));
            return cleanName;
        }
        if (textureName.Contains("DXT5|BC3-"))
        {
            string cleanName = textureName.Split(["DXT5|BC3-"], System.StringSplitOptions.None)[1];
            cleanName = string.Join("-", cleanName.Split('-').Take(cleanName.Split('-').Length - 1));
            return cleanName;
        }
        return textureName;
    }
}