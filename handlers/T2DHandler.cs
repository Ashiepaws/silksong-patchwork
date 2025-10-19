using System.IO;
using HarmonyLib;
using UnityEngine;
using System.Linq;

namespace Patchwork;

[HarmonyPatch]
public static class T2DHandler
{
    public static string T2DDumpPath { get { return Path.Combine(SpriteDumper.DumpPath, "T2D"); } }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SpriteRenderer), nameof(SpriteRenderer.sprite), MethodType.Setter)]
    public static void SetSpritePostfix(SpriteRenderer __instance, Sprite value)
    {
        if (__instance == null || value == null)
            return;
        string texName = StringUtil.SanitizeFileName(value.texture.name);

        if (Plugin.Config.DumpSprites && value.texture != null && value.texture.name != null && value.texture.name != "")
        {
            string path = Path.Combine(T2DDumpPath, $"{texName}.png");
            if (!File.Exists(path))
            {
                if (Plugin.Config.LogSpriteDumping) Plugin.Logger.LogInfo($"Dumping sprite set on SpriteRenderer {__instance.name} - {texName}");
                var rt = TexUtil.GetReadable(value.texture);
                Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = previous;
                Object.Destroy(rt);
                var png = tex.EncodeToPNG();
                File.WriteAllBytes(path, png);
            }
        }

        if (Plugin.Config.LoadSprites)
        {
            var customTex = FindT2DSprite(texName);
            if (customTex != null)
            {
                if (Plugin.Config.LogSpriteLoading) Plugin.Logger.LogInfo($"Loading custom sprite for SpriteRenderer {__instance.name} - {texName}");
                Rect rect = new(0, 0, customTex.width, customTex.height);
                Vector2 pivot = new(0.5f, 0.5f);
                __instance.sprite = Sprite.Create(customTex, rect, pivot, value.pixelsPerUnit);
            }
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
}