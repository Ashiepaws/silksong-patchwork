using System.IO;

namespace Patchwork.Util;

public static class IOUtil
{
    public static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}