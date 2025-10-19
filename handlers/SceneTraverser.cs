using System.Collections.Generic;

namespace Patchwork.Handlers;

public static class SceneTraverser
{
    private static Queue<string> sceneQueue = new();

    public static void TraverseAllScenes()
    {
        Plugin.Logger.LogInfo("Starting scene traversal for full sprite dump...");
        sceneQueue.Clear();

        var teleportMap = SceneTeleportMap.GetTeleportMap();
        foreach (var sceneName in teleportMap.Keys)
        {
            if (!sceneQueue.Contains(sceneName) && teleportMap[sceneName].MapZone != GlobalEnums.MapZone.NONE)
            {
                Plugin.Logger.LogInfo($"Enqueued scene: {sceneName} : {teleportMap[sceneName].MapZone}");
                sceneQueue.Enqueue(sceneName);
            }
        }

        LoadNextScene();
    }

    public static void OnDumpCompleted()
    {
        LoadNextScene();
    }

    private static bool LoadNextScene()
    {
        if (sceneQueue.Count > 0)
        {
            string nextScene = sceneQueue.Dequeue();
            GameManager.instance.LoadScene(nextScene);
            return true;
        }
        return false;
    }
}