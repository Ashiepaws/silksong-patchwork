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

    public PatchworkConfig(ConfigFile config)
    {
        _DataBasePath = config.Bind("General", "PatchworkFolder", "Patchwork", "Path to the folder for all Patchwork-related files, including dumps and modded sprites, relative to the game folder.");
        _DumpSprites = config.Bind("General", "DumpSprites", true, "Enable dumping of sprites");
        _LoadSprites = config.Bind("General", "LoadSprites", true, "Enable loading of custom sprites");
        _CacheAtlases = config.Bind("General", "CacheAtlases", true, "Enable caching of sprite atlases in memory to speed up sprite loading");
        _ReloadSceneOnChange = config.Bind("General", "ReloadSceneOnChange", true, "Enable automatic scene reload when a sprite file changes. May cause instability.");
    }
}