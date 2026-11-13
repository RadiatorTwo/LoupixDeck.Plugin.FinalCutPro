using LoupixDeck.PluginSdk;

namespace LoupixDeck.Plugin.FinalCutPro;

/// <summary>
/// Runs a Final Cut Pro command by pressing the shortcut the active command set gives it. The parameter is
/// Apple's command-set id (e.g. <c>SelectToolBlade</c>), which is also what Loupedeck profiles store, so a
/// remapped shortcut in Final Cut Pro keeps working without touching the button.
/// </summary>
internal sealed class ShortcutCommand(CommandSetService commandSets) : IPluginCommand
{
    public const string Name = "FinalCutPro.Shortcut";

    public CommandDescriptor Descriptor { get; } = new()
    {
        CommandName = Name,
        DisplayName = "Final Cut Pro command",
        Group = "Final Cut Pro",
        Description = "Presses the shortcut of a Final Cut Pro command from the active command set.",
        ParameterTemplate = "({CommandId})",
        Parameters = [new CommandParameter("CommandId", typeof(string))]
    };

    public ButtonTargets SupportedTargets => ButtonTargets.All;

    public async Task Execute(CommandContext ctx)
    {
        string commandId = ctx.Parameters.Length > 0 ? ctx.Parameters[0].Trim() : string.Empty;
        if (commandId.Length == 0) return;

        try
        {
            string? combination = await commandSets.FindShortcutAsync(commandId);
            if (combination == null)
            {
                ctx.Host.Logger.Warn(commandSets.ActivePath == null
                    ? $"No Final Cut Pro command set found; cannot run '{commandId}'."
                    : $"'{commandId}' has no shortcut in '{commandSets.ActivePath}'.");
                return;
            }

            ctx.Host.ExecuteCommand($"System.KeyCombination({combination})");
        }
        catch (Exception ex)
        {
            ctx.Host.Logger.Error($"Final Cut Pro command '{commandId}' failed", ex);
        }
    }
}
