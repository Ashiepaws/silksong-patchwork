using System.IO;

namespace Patchwork.Util;

public static class StringUtil
{
    public static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}