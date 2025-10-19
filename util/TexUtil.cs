using System.IO;
using BepInEx;
using UnityEngine;

namespace Patchwork.Util;

public static class TexUtil
{
    public static Material RotateMaterial = null;

    public static void Initialize()
    {
        string bundlePath = Path.Combine(Paths.PluginPath, "Patchwork", "patchwork.assetbundle");
        AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
        var RotateShader = bundle.LoadAsset<Shader>("Assets/Patchwork/Rotate.shader");
        RotateMaterial = new Material(RotateShader);
    }

    public static RenderTexture GetReadable(Texture tex)
    {
        RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Graphics.Blit(tex, rt);
        return rt;
    }

    public static Texture2D GetCutout(Texture2D source, Rect rect)
    {
        if (rect.x < 0 || rect.y < 0 || rect.x + rect.width > source.width || rect.y + rect.height > source.height)
        {
            if (Plugin.Config.LogSpriteWarnings) Plugin.Logger.LogWarning($"GetCutout: Rect {rect} is out of bounds for texture size {source.width}x{source.height}");
            return null;
        }
        if (rect.width <= 0 || rect.height <= 0)
        {
            if (Plugin.Config.LogSpriteWarnings) Plugin.Logger.LogWarning($"GetCutout: Rect {rect} has non-positive dimensions");
            return null;
        }

        Color[] pixels = source.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
        Texture2D newTex = new((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
        newTex.SetPixels(pixels);
        newTex.Apply();
        return newTex;
    }

    public static Texture2D RotateCW(Texture2D source)
    {
        int width = source.width;
        int height = source.height;
        Texture2D rotated = new(height, width);
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                rotated.SetPixel(height - j - 1, i, source.GetPixel(i, j));
            }
        }
        rotated.Apply();
        return rotated;
    }

    public static Texture2D RotateCCW(Texture2D source)
    {
        int width = source.width;
        int height = source.height;
        Texture2D rotated = new(height, width);
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                rotated.SetPixel(j, width - i - 1, source.GetPixel(i, j));
            }
        }
        rotated.Apply();
        return rotated;
    }

    public static Texture2D LoadFromPNG(string path)
    {
        if (!File.Exists(path))
        {
            Plugin.Logger.LogWarning($"LoadFromPNG: File {path} does not exist");
            return null;
        }

        byte[] pngData = File.ReadAllBytes(path);
        Texture2D tex = new(2, 2);
        if (!tex.LoadImage(pngData))
        {
            Plugin.Logger.LogWarning($"LoadFromPNG: Failed to load image data from {path}");
            return null;
        }
        return tex;
    }

    public static Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        Graphics.Blit(source, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D resized = new(newWidth, newHeight);
        resized.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        resized.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return resized;
    }
}