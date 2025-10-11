using System.IO;
using UnityEngine.SceneManagement;

namespace Patchwork;

/// <summary>
/// Watches the sprite load directory for changes and invalidates cache entries accordingly.
/// </summary>
public class SpriteFileWatcher
{
    public FileSystemWatcher SpriteWatcher;
    public FileSystemWatcher AtlasWatcher;

    public SpriteFileWatcher()
    {
        SpriteWatcher = new FileSystemWatcher();
        SpriteWatcher.Path = SpriteLoader.LoadPath;
        SpriteWatcher.IncludeSubdirectories = true;
        SpriteWatcher.Filter = "*.png";
        SpriteWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName;
        SpriteWatcher.Changed += OnSpriteChanged;
        SpriteWatcher.Created += OnSpriteChanged;
        SpriteWatcher.Deleted += OnSpriteChanged;
        SpriteWatcher.Renamed += (s, e) => OnSpriteChanged(s, e);
        SpriteWatcher.EnableRaisingEvents = true;

        AtlasWatcher = new FileSystemWatcher();
        AtlasWatcher.Path = SpriteLoader.AtlasLoadPath;
        AtlasWatcher.IncludeSubdirectories = true;
        AtlasWatcher.Filter = "*.png";
        AtlasWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName;
        AtlasWatcher.Changed += OnAtlasChanged;
        AtlasWatcher.Created += OnAtlasChanged;
        AtlasWatcher.Deleted += OnAtlasChanged;
        AtlasWatcher.Renamed += (s, e) => OnAtlasChanged(s, e);
        AtlasWatcher.EnableRaisingEvents = true;
    }

    private void OnSpriteChanged(object sender, FileSystemEventArgs e)
    {
        string relativePath = Path.GetRelativePath(SpriteLoader.LoadPath, e.FullPath);
        string[] pathParts = relativePath.Split(Path.DirectorySeparatorChar);
        if (pathParts.Length < 3)
            return;

        string collectionName = pathParts[pathParts.Length - 3];
        string materialName = pathParts[pathParts.Length - 2];

        SpriteLoader.InvalidateCacheEntry(collectionName, materialName);
        Plugin.Logger.LogDebug($"Invalidated cache for collection {collectionName}, material {materialName} due to file change: {e.ChangeType} {e.FullPath}");

        if (Plugin.Config.ReloadSceneOnChange)
            GameManager.instance.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnAtlasChanged(object sender, FileSystemEventArgs e)
    {
        string relativePath = Path.GetRelativePath(SpriteLoader.AtlasLoadPath, e.FullPath);
        string[] pathParts = relativePath.Split(Path.DirectorySeparatorChar);
        if (pathParts.Length < 2)
            return;

        string collectionName = pathParts[pathParts.Length - 3];
        string materialName = Path.GetFileNameWithoutExtension(pathParts[1]);

        SpriteLoader.InvalidateCacheEntry(collectionName, materialName);
        Plugin.Logger.LogDebug($"Invalidated cache for collection {collectionName}, material {materialName} due to atlas change: {e.ChangeType} {e.FullPath}");

        if (Plugin.Config.ReloadSceneOnChange)
            GameManager.instance.LoadScene(SceneManager.GetActiveScene().name);
    }
}