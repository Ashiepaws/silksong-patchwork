using BepInEx.Configuration;

namespace Patchwork;

public class PatchworkConfig
{
    private readonly ConfigEntry<string> _DataBasePath;
    public string DataBasePath { get { return _DataBasePath.Value; } }

    private readonly ConfigEntry<bool> _DumpSprites;
    public bool DumpSprites { get { return _DumpSprites.Value; } }

    public PatchworkConfig(ConfigFile config)
    {
        _DataBasePath = config.Bind("General", "PatchworkFolder", "Patchwork", "Path to the folder for all Patchwork-related files, including dumps and modded sprites, relative to the game folder.");
        _DumpSprites = config.Bind("General", "DumpSprites", true, "Enable dumping of sprites");
    }
}