using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;

namespace LoupixDeck.Plugin.FinalCutPro;

/// <summary>
/// Reads a property list into plain .NET values: <see cref="Dictionary{TKey,TValue}"/> for a dict,
/// <see cref="List{T}"/> for an array, and string, long, double or bool for the scalars. A binary plist
/// is first converted to XML with macOS' own <c>plutil</c>, so no plist library is needed.
/// </summary>
internal static class PlistReader
{
    /// <summary>The root value of the plist at <paramref name="path"/>.</summary>
    /// <exception cref="InvalidDataException">The file is not a readable plist.</exception>
    public static async Task<object?> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        string xml = await ProcessRunner.RunAsync("/usr/bin/plutil", ["-convert", "xml1", "-o", "-", path],
            cancellationToken) ?? throw new InvalidDataException($"plutil could not read '{path}'.");

        XElement? root = XDocument.Parse(xml).Root?.Elements().FirstOrDefault();
        return root == null ? null : Parse(root);
    }

    private static object? Parse(XElement element) => element.Name.LocalName switch
    {
        "dict" => ParseDict(element),
        "array" => element.Elements().Select(Parse).ToList(),
        "string" => element.Value,
        "integer" => long.TryParse(element.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long number)
            ? number
            : null,
        "real" => double.TryParse(element.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double real)
            ? real
            : null,
        "true" => true,
        "false" => false,
        _ => element.Value
    };

    private static Dictionary<string, object?> ParseDict(XElement element)
    {
        Dictionary<string, object?> result = new(StringComparer.Ordinal);
        string? key = null;
        foreach (XElement child in element.Elements())
        {
            if (child.Name.LocalName == "key")
            {
                key = child.Value;
                continue;
            }

            if (key != null)
                result[key] = Parse(child);
            key = null;
        }

        return result;
    }
}

/// <summary>Runs a short-lived command-line tool and returns its standard output.</summary>
internal static class ProcessRunner
{
    /// <summary>The tool's standard output, or null when it could not be started or exited with an error.</summary>
    public static async Task<string?> RunAsync(string fileName, IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        ProcessStartInfo info = new(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (string argument in arguments)
            info.ArgumentList.Add(argument);

        try
        {
            using Process? process = Process.Start(info);
            if (process == null) return null;

            Task<string> output = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> error = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            await error;
            return process.ExitCode == 0 ? await output : null;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return null;
        }
    }
}
