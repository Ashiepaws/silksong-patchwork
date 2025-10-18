using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Patchwork;

public static class SpriteUtil
{
    public static Rect GetSpriteRect(tk2dSpriteDefinition def, Texture tex)
    {
        Vector2[] uvs = def.uvs;

        int xMin = (int)(uvs.Min(uv => uv.x) * tex.width);
        int xMax = (int)(uvs.Max(uv => uv.x) * tex.width);
        int yMin = (int)(uvs.Min(uv => uv.y) * tex.height);
        int yMax = (int)(uvs.Max(uv => uv.y) * tex.height);
        int width = math.min(xMax - xMin, tex.width);
        int height = math.min(yMax - yMin, tex.height);

        return new Rect(xMin, yMin, width, height);
    }
}