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
}