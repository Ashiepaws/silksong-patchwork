using BepInEx.Configuration;

namespace Patchwork;

public class PatchworkConfig
{
    private readonly ConfigEntry<string> _DataBasePath;
    public string DataBasePath { get { return _DataBasePath.Value; } }

    private readonly ConfigEntry<bool> _DumpSprites;
    public bool DumpSprites { get { return _DumpSprites.Value; } }

    private readonly ConfigEntry<bool> _LoadSprites;
    public bool LoadSprites { get { return _LoadSprites.Value; } }

    private readonly ConfigEntry<bool> _CacheAtlases;
    public bool CacheAtlases { get { return _CacheAtlases.Value; } }

    private readonly ConfigEntry<bool> _ReloadSceneOnChange;
    public bool ReloadSceneOnChange { get { return _ReloadSceneOnChange.Value; } }

    private readonly ConfigEntry<bool> _EnableForceReload;
    public bool EnableForceReload { get { return _EnableForceReload.Value; } }

    private readonly ConfigEntry<UnityEngine.KeyCode> _ForceReloadKey = null;
    public UnityEngine.KeyCode ForceReloadKey { get { return _ForceReloadKey.Value; } }

    private readonly ConfigEntry<UnityEngine.KeyCode> _FullDumpKey = null;
    public UnityEngine.KeyCode FullDumpKey { get { return _FullDumpKey.Value; } }

    private readonly ConfigEntry<bool> _LogSpriteLoading;
    public bool LogSpriteLoading { get { return _LogSpriteLoading.Value; } }

    private readonly ConfigEntry<bool> _LogSpriteDumping;
    public bool LogSpriteDumping { get { return _LogSpriteDumping.Value; } }

    private readonly ConfigEntry<bool> _LogSpriteWarnings;
    public bool LogSpriteWarnings { get { return _LogSpriteWarnings.Value; } }

    public PatchworkConfig(ConfigFile config)
    {
        _DataBasePath = config.Bind("Paths", "PatchworkFolder", "Patchwork", "Path to the folder for all Patchwork-related files, including dumps and modded sprites, relative to the game folder.");

        _DumpSprites = config.Bind("General", "DumpSprites", false, "Enable dumping of sprites");
        _LoadSprites = config.Bind("General", "LoadSprites", true, "Enable loading of custom sprites");

        _CacheAtlases = config.Bind("Advanced", "CacheAtlases", true, "Enable caching of sprite atlases in memory to speed up sprite loading");

        _ReloadSceneOnChange = config.Bind("Reloading", "ReloadSceneOnChange", false, "Enable automatic scene reload when a sprite file changes. May cause instability.");
        _EnableForceReload = config.Bind("Reloading", "EnableForceReload", false, "Enable the ability to force reload the current scene with a key press.");

        _ForceReloadKey = config.Bind("Keybinds", "ForceReloadKey", UnityEngine.KeyCode.F5, "Key to force reload all sprite collections");
        _FullDumpKey = config.Bind("Keybinds", "FullDumpKey", UnityEngine.KeyCode.F6, "Key to load all scenes in the game and dump all their sprites. Only works when DumpSprites is enabled.");

        _LogSpriteLoading = config.Bind("Logging", "LogSpriteLoading", false, "Enable detailed logging of sprite loading operations. May slow down the game.");
        _LogSpriteDumping = config.Bind("Logging", "LogSpriteDumping", false, "Enable detailed logging of sprite dumping operations. May slow down the game.");
        _LogSpriteWarnings = config.Bind("Logging", "LogSpriteWarnings", false, "Enable logging of warnings related to sprite loading and dumping.");
    }
}