namespace LoupixDeck.Plugin.FinalCutPro;

/// <summary>
/// Translates one shortcut entry of a Final Cut Pro command set into the key names
/// <c>System.KeyCombination</c> understands, e.g. <c>Win+Shift+B</c>.
/// </summary>
/// <remarks>
/// An entry carries the key in <c>characterString</c> (or <c>character</c>) and the modifiers either as a
/// string naming them (<c>modifiers</c>) or as an <c>NSEvent</c> modifier bit mask (<c>modifierMask</c>).
/// Both forms are read, the string first, as CommandPost does.
/// </remarks>
internal static class ShortcutKeys
{
    private const long ShiftMask = 1 << 17;
    private const long ControlMask = 1 << 18;
    private const long OptionMask = 1 << 19;
    private const long CommandMask = 1 << 20;
    private const long NumericPadMask = 1 << 21;

    /// <summary>AppKit's names for the keys that type no character.</summary>
    private static readonly Dictionary<string, string> NamedKeys = new(StringComparer.Ordinal)
    {
        ["NSUpArrowFunctionKey"] = "Up",
        ["NSDownArrowFunctionKey"] = "Down",
        ["NSLeftArrowFunctionKey"] = "Left",
        ["NSRightArrowFunctionKey"] = "Right",
        ["NSDeleteFunctionKey"] = "Del",
        ["NSHomeFunctionKey"] = "Home",
        ["NSEndFunctionKey"] = "End",
        ["NSPageUpFunctionKey"] = "PageUp",
        ["NSPageDownFunctionKey"] = "PageDown",
        ["NSCarriageReturnCharacter"] = "Enter",
        ["NSClearLineFunctionKey"] = "NumLock"
    };

    /// <summary>Characters typed without Shift, named by their US-layout key.</summary>
    private static readonly Dictionary<char, string> PlainCharacters = new()
    {
        [' '] = "Space", ['\r'] = "Enter", ['\n'] = "Enter", ['\t'] = "Tab", ['\u001B'] = "Esc",
        ['\b'] = "Backspace", ['\u007F'] = "Backspace",
        ['`'] = "Grave", ['-'] = "Minus", ['='] = "Equals", ['['] = "LeftBracket", [']'] = "RightBracket",
        [';'] = "Semicolon", ['\''] = "Quote", ['\\'] = "Backslash", [','] = "Comma", ['.'] = "Period",
        ['/'] = "Slash"
    };

    /// <summary>Characters typed with Shift on a US layout, and the key that types them.</summary>
    private static readonly Dictionary<char, string> ShiftedCharacters = new()
    {
        ['~'] = "Grave", ['!'] = "1", ['@'] = "2", ['#'] = "3", ['$'] = "4", ['%'] = "5", ['^'] = "6",
        ['&'] = "7", ['*'] = "8", ['('] = "9", [')'] = "0", ['_'] = "Minus", ['+'] = "Equals",
        ['{'] = "LeftBracket", ['}'] = "RightBracket", [':'] = "Semicolon", ['"'] = "Quote",
        ['|'] = "Backslash", ['<'] = "Comma", ['>'] = "Period", ['?'] = "Slash"
    };

    /// <summary>Keypad characters and their key names.</summary>
    private static readonly Dictionary<char, string> KeypadCharacters = new()
    {
        ['0'] = "Num0", ['1'] = "Num1", ['2'] = "Num2", ['3'] = "Num3", ['4'] = "Num4", ['5'] = "Num5",
        ['6'] = "Num6", ['7'] = "Num7", ['8'] = "Num8", ['9'] = "Num9", ['*'] = "NumMultiply",
        ['+'] = "NumPlus", ['/'] = "NumDivide", ['-'] = "NumMinus", ['.'] = "NumDecimal",
        ['\r'] = "NumEnter", ['\u0003'] = "NumEnter"
    };

    /// <summary>The key combination of <paramref name="entry"/>, or null when it holds no usable key.</summary>
    public static string? ToCombination(IReadOnlyDictionary<string, object?> entry)
    {
        string? character = entry.GetValueOrDefault("characterString") as string
                            ?? entry.GetValueOrDefault("character") as string;
        if (string.IsNullOrEmpty(character)) return null;

        bool command, shift, option, control, keypad;
        if (entry.GetValueOrDefault("modifiers") is string modifiers)
        {
            command = modifiers.Contains("command", StringComparison.OrdinalIgnoreCase);
            shift = modifiers.Contains("shift", StringComparison.OrdinalIgnoreCase);
            option = modifiers.Contains("option", StringComparison.OrdinalIgnoreCase);
            control = modifiers.Contains("control", StringComparison.OrdinalIgnoreCase);
            keypad = modifiers.Contains("keypad", StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            long mask = entry.GetValueOrDefault("modifierMask") is long value ? value : 0;
            command = (mask & CommandMask) != 0;
            shift = (mask & ShiftMask) != 0;
            option = (mask & OptionMask) != 0;
            control = (mask & ControlMask) != 0;
            keypad = (mask & NumericPadMask) != 0;
        }

        string? key = KeyName(character, keypad, ref shift);
        if (key == null) return null;

        List<string> keys = [];
        if (control) keys.Add("Ctrl");
        if (option) keys.Add("Alt");
        if (shift) keys.Add("Shift");
        // LoupixDeck sends Win as Command on a Mac.
        if (command) keys.Add("Win");
        keys.Add(key);
        return string.Join('+', keys);
    }

    private static string? KeyName(string character, bool keypad, ref bool shift)
    {
        if (NamedKeys.TryGetValue(character, out string? named)) return named;

        // NSF1FunctionKey … NSF20FunctionKey.
        if (character.StartsWith("NSF", StringComparison.Ordinal) &&
            character.EndsWith("FunctionKey", StringComparison.Ordinal) &&
            int.TryParse(character.AsSpan(3, character.Length - 3 - "FunctionKey".Length), out int function) &&
            function is >= 1 and <= 20)
        {
            return $"F{function}";
        }

        if (character.Length != 1) return null;

        char c = character[0];
        if (keypad && KeypadCharacters.TryGetValue(c, out string? pad)) return pad;
        if (PlainCharacters.TryGetValue(c, out string? plain)) return plain;
        // A letter's case carries no meaning: Shift is always listed in the modifiers (as CommandPost reads it).
        if (c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9') return char.ToUpperInvariant(c).ToString();

        if (ShiftedCharacters.TryGetValue(c, out string? shifted))
        {
            shift = true;
            return shifted;
        }

        // Any other character (a non-US layout key): the host types it by character.
        return char.IsControl(c) || c == '+' ? null : c.ToString();
    }
}
