using System.Globalization;

namespace LoupixDeck.Plugin.FinalCutPro;

/// <summary>
/// Finds Final Cut Pro's active command set and looks up the keyboard shortcut of a command in it.
/// </summary>
/// <remarks>
/// The active set is named by the <c>Active Command Set</c> preference of <c>com.apple.FinalCut</c>; without
/// it Final Cut Pro uses the <c>Default.commandset</c> inside its app bundle. User sets live in
/// <c>~/Library/Application Support/Final Cut Pro/Command Sets/</c>. The parsed set is cached until its file
/// changes, and the preference is re-read at most every few seconds, so a switch of the command set in
/// Final Cut Pro is picked up without a restart.
/// </remarks>
internal sealed class CommandSetService
{
    private const string PreferencesDomain = "com.apple.FinalCut";
    private const string ActiveSetKey = "Active Command Set";
    private const string BundleId = "com.apple.FinalCut";
    private const string FallbackAppPath = "/Applications/Final Cut Pro.app";
    private static readonly TimeSpan ActivePathLifetime = TimeSpan.FromSeconds(5);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _activePath;
    private DateTime _activePathRead = DateTime.MinValue;
    private string? _loadedPath;
    private DateTime _loadedWriteTime;
    private Dictionary<string, object?> _commands = new(StringComparer.Ordinal);

    private static string UserSetsFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library", "Application Support", "Final Cut Pro", "Command Sets");

    /// <summary>The file of the active command set, or null when Final Cut Pro is not found.</summary>
    public string? ActivePath => _loadedPath;

    /// <summary>
    /// The key combination for <paramref name="commandId"/> (an Apple command-set id such as
    /// <c>SelectToolBlade</c>), or null when the active set gives it no shortcut.
    /// </summary>
    public async Task<string?> FindShortcutAsync(string commandId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await RefreshAsync(cancellationToken);
            return _commands.GetValueOrDefault(commandId) switch
            {
                Dictionary<string, object?> entry => ShortcutKeys.ToCombination(entry),
                List<object?> entries => entries.OfType<Dictionary<string, object?>>()
                    .Select(ShortcutKeys.ToCombination)
                    .FirstOrDefault(combo => combo != null),
                _ => null
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (_activePath == null || DateTime.UtcNow - _activePathRead > ActivePathLifetime)
        {
            _activePath = await ResolveActivePathAsync(cancellationToken);
            _activePathRead = DateTime.UtcNow;
        }

        if (_activePath == null)
        {
            _loadedPath = null;
            _commands = new Dictionary<string, object?>(StringComparer.Ordinal);
            return;
        }

        DateTime writeTime = File.GetLastWriteTimeUtc(_activePath);
        if (_activePath == _loadedPath && writeTime == _loadedWriteTime) return;

        _commands = await PlistReader.ReadAsync(_activePath, cancellationToken) as Dictionary<string, object?>
                    ?? new Dictionary<string, object?>(StringComparer.Ordinal);
        _loadedPath = _activePath;
        _loadedWriteTime = writeTime;
    }

    private static async Task<string?> ResolveActivePathAsync(CancellationToken cancellationToken)
    {
        string? preference = (await ProcessRunner.RunAsync("/usr/bin/defaults",
            ["read", PreferencesDomain, ActiveSetKey], cancellationToken))?.Trim();

        if (!string.IsNullOrEmpty(preference))
        {
            // Normally a full path; a bare name is taken as one of the user's sets.
            string path = Path.IsPathRooted(preference)
                ? preference
                : Path.Combine(UserSetsFolder,
                    preference.EndsWith(".commandset", StringComparison.OrdinalIgnoreCase)
                        ? preference
                        : preference + ".commandset");
            if (File.Exists(path)) return path;
        }

        return await DefaultSetPathAsync(cancellationToken);
    }

    /// <summary>The <c>Default.commandset</c> in Final Cut Pro's bundle, for the UI language if it has one.</summary>
    private static async Task<string?> DefaultSetPathAsync(CancellationToken cancellationToken)
    {
        string? found = (await ProcessRunner.RunAsync("/usr/bin/mdfind",
            [$"kMDItemCFBundleIdentifier == '{BundleId}'"], cancellationToken))?
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(path => path.EndsWith(".app", StringComparison.OrdinalIgnoreCase));

        string app = found ?? FallbackAppPath;
        string resources = Path.Combine(app, "Contents", "Resources");
        if (!Directory.Exists(resources)) return null;

        string language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        foreach (string folder in new[] { language, "en", "English" })
        {
            string path = Path.Combine(resources, folder + ".lproj", "Default.commandset");
            if (File.Exists(path)) return path;
        }

        return Directory.EnumerateFiles(resources, "Default.commandset", SearchOption.AllDirectories).FirstOrDefault();
    }
}
