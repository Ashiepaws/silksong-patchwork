using System.IO;
using UnityEngine.SceneManagement;

namespace Patchwork;

/// <summary>
/// Watches the sprite load directory for changes and invalidates cache entries accordingly.
/// </summary>
public class SpriteFileWatcher
{
    public FileSystemWatcher Watcher;

    public SpriteFileWatcher()
    {
        Watcher = new FileSystemWatcher();
        Watcher.Path = SpriteLoader.LoadPath;
        Watcher.IncludeSubdirectories = true;
        Watcher.Filter = "*.png";
        Watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName;
        Watcher.Changed += OnChanged;
        Watcher.Created += OnChanged;
        Watcher.Deleted += OnChanged;
        Watcher.Renamed += (s, e) => OnChanged(s, e);
        Watcher.EnableRaisingEvents = true;

        // TODO: Watch spritesheets directory
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        string relativePath = Path.GetRelativePath(SpriteLoader.LoadPath, e.FullPath);
        string[] pathParts = relativePath.Split(Path.DirectorySeparatorChar);
        if (pathParts.Length < 3)
            return;

        string collectionName = pathParts[0];
        string materialName = pathParts[1];

        SpriteLoader.InvalidateCacheEntry(collectionName, materialName);
        Plugin.Logger.LogInfo($"Invalidated cache for collection {collectionName}, material {materialName} due to file change: {e.ChangeType} {e.FullPath}");

        if (Plugin.Config.ReloadSceneOnChange)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}