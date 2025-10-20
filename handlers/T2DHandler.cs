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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SpriteRenderer), nameof(SpriteRenderer.sprite), MethodType.Setter)]
    public static void SetSpritePostfix(SpriteRenderer __instance, Sprite value)
    {
        if (__instance == null || value == null || __instance.gameObject.name == "TempSpriteRenderer")
            return;

        if (Plugin.Config.DumpSprites && !string.IsNullOrEmpty(value.name) && !string.IsNullOrEmpty(value.texture.name))
        {
            if (value.texture.name.Contains("-BC7-"))
            {
                int width = (int)value.rect.width;
                int height = (int)value.rect.height;
                int renderLayer = 31;

                GameObject spriteGO = new GameObject("TempSpriteRenderer");
                var spriteRenderer = spriteGO.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = value;
                spriteGO.layer = renderLayer;
                spriteGO.transform.position = Vector3.zero;

                GameObject camGO = new GameObject("TempCamera");
                Camera cam = camGO.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0, 0, 0, 0);
                cam.orthographic = true;
                cam.cullingMask = 1 << renderLayer;
                cam.orthographicSize = height / value.pixelsPerUnit / 2f;
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

                // Split at -BC7- and remove last hyphen part
                string cleanName = CleanTextureName(value.texture.name);
                string saveDir = Path.Combine(T2DDumpPath, cleanName);
                IOUtil.EnsureDirectoryExists(saveDir);
                string savePath = Path.Combine(saveDir, value.name + ".png");
                if (!File.Exists(savePath))
                {
                    byte[] pngData = spriteTex.EncodeToPNG();
                    File.WriteAllBytes(savePath, pngData);
                    if (Plugin.Config.LogSpriteLoading)
                        Plugin.Logger.LogInfo($"Dumped T2D sprite {value.name} from texture {value.texture.name} to {savePath}");
                }
            }
        }

        if (Plugin.Config.LoadSprites)
        {
            if (LoadedT2DSprites.ContainsKey(value.name))
            {
                __instance.sprite = LoadedT2DSprites[value.name];
                return;
            }
            if (value.texture.name.Contains("-BC7-"))
            {
                Texture2D spriteTex = FindT2DSprite(CleanTextureName(value.texture.name), value.name);
                if (spriteTex == null)
                    return;

                Sprite newSprite = Sprite.Create(spriteTex, new Rect(0, 0, spriteTex.width, spriteTex.height), new Vector2(0.5f, 0.5f), value.pixelsPerUnit);
                LoadedT2DSprites[value.name] = newSprite;
                __instance.sprite = newSprite;
                if (Plugin.Config.LogSpriteLoading)
                    Plugin.Logger.LogInfo($"Loaded T2D sprite {value.name} from custom PNG");
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
        return textureName;
    }
}