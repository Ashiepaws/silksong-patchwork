using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Patchwork;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    internal static new PatchworkConfig Config;
    internal static SpriteFileWatcher FileWatcher;

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Config = new PatchworkConfig(base.Config);
        Logger.LogInfo($"Patchwork is loaded! Version: {MyPluginInfo.PLUGIN_VERSION}");

        FileWatcher = new SpriteFileWatcher(); // Needs config to be initialized first

        if (Config.DumpSprites)
        {
            SceneManager.sceneLoaded += (scene, mode) =>
            {
                var spriteCollections = Resources.FindObjectsOfTypeAll<tk2dSpriteCollectionData>();
                foreach (var collection in spriteCollections)
                    SpriteDumper.DumpCollection(collection);
            };
        }

        if (Config.LoadSprites)
        {
            SceneManager.sceneLoaded += (scene, mode) =>
            {
                var spriteCollections = Resources.FindObjectsOfTypeAll<tk2dSpriteCollectionData>();
                foreach (var collection in spriteCollections)
                    SpriteLoader.LoadCollection(collection);
            };
        }
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(Config.ForceReloadKey) && Config.EnableForceReload)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            Logger.LogInfo("Force reloaded current scene due to key press");
        }
    }
}
