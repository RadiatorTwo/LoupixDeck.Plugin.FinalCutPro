using LoupixDeck.PluginSdk;

namespace LoupixDeck.Plugin.FinalCutPro;

public sealed class FinalCutProPlugin : LoupixPlugin
{
    public override PluginMetadata Metadata { get; } = new()
    {
        Id = "finalcutpro",
        Name = "FinalCutPro",
        Version = new Version(1, 0, 0),
        SdkVersion = SdkInfo.Version,
        Author = "",
        Description = ""
    };

    public override void Initialize(IPluginHost host)
    {
    }

    public override IEnumerable<IPluginCommand> GetCommands() => [];
}
