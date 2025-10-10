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
        
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Config = new PatchworkConfig(base.Config);
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

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
}
