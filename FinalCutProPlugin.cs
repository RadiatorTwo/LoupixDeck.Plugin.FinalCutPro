using LoupixDeck.PluginSdk;

namespace LoupixDeck.Plugin.FinalCutPro;

public sealed class FinalCutProPlugin : LoupixPlugin
{
    private readonly CommandSetService _commandSets = new();

    public override PluginMetadata Metadata { get; } = new()
    {
        Id = "finalcutpro",
        Name = "Final Cut Pro",
        Version = new Version(1, 0, 0),
        SdkVersion = SdkInfo.Version,
        Author = "RadiatorTwo",
        Description = "Runs Final Cut Pro commands through the shortcuts of the active command set."
    };

    public override IEnumerable<IPluginCommand> GetCommands() => [new ShortcutCommand(_commandSets)];
}
