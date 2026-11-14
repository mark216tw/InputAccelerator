using InputAccelerator.Core.Models;

namespace InputAccelerator.Core.Services;

public static class HotkeyParser
{
    public const int MaxSequenceSteps = 8;

    private const char SequenceSeparator = ',';
    private const int VirtualKeyShift = 0x10;
    private const int VirtualKeyControl = 0x11;
    private const int VirtualKeyMenu = 0x12;
    private const int VirtualKeyLeftWindows = 0x5B;
    private const int VirtualKeyRightWindows = 0x5C;
    private const int VirtualKeyLeftShift = 0xA0;
    private const int VirtualKeyRightShift = 0xA1;
    private const int VirtualKeyLeftControl = 0xA2;
    private const int VirtualKeyRightControl = 0xA3;
    private const int VirtualKeyLeftMenu = 0xA4;
    private const int VirtualKeyRightMenu = 0xA5;

    private static readonly HotkeyModifiers AllModifiers =
        HotkeyModifiers.Control | HotkeyModifiers.Shift | HotkeyModifiers.Alt | HotkeyModifiers.Windows;

    private static readonly Dictionary<string, HotkeyModifiers> ModifierAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Ctrl"] = HotkeyModifiers.Control,
        ["Control"] = HotkeyModifiers.Control,
        ["Shift"] = HotkeyModifiers.Shift,
        ["Alt"] = HotkeyModifiers.Alt,
        ["Win"] = HotkeyModifiers.Windows,
        ["Windows"] = HotkeyModifiers.Windows,
    };

    private static readonly Dictionary<string, KeyInfo> KeysByName = CreateKeyMap();
    private static readonly Dictionary<int, KeyInfo> KeysByVirtualKey = CreateVirtualKeyMap();

    public static bool TryParse(string? text, out HotkeySequence sequence)
    {
        sequence = HotkeySequence.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var parts = text.Split(SequenceSeparator, StringSplitOptions.TrimEntries);
        if (parts.Length > MaxSequenceSteps)
        {
            return false;
        }

        var steps = new HotkeyChord[parts.Length];
        for (var index = 0; index < parts.Length; index++)
        {
            if (string.IsNullOrEmpty(parts[index]) || !TryParseChord(parts[index], out steps[index]))
            {
                return false;
            }
        }

        sequence = new HotkeySequence(steps);
        return true;
    }

    public static bool TryParseChord(string? text, out HotkeyChord chord)
    {
        chord = default;
        if (string.IsNullOrWhiteSpace(text) || text.Contains(SequenceSeparator))
        {
            return false;
        }

        var parts = text.Split('+', StringSplitOptions.TrimEntries);
        var modifiers = HotkeyModifiers.None;
        KeyInfo? key = null;
        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part))
            {
                return false;
            }

            if (ModifierAliases.TryGetValue(part, out var modifier))
            {
                if ((modifiers & modifier) != 0)
                {
                    return false;
                }

                modifiers |= modifier;
                continue;
            }

            if (key is not null || !KeysByName.TryGetValue(part, out var keyInfo))
            {
                return false;
            }

            key = keyInfo;
        }

        if (key is null)
        {
            return false;
        }

        chord = new HotkeyChord(modifiers, key.VirtualKey);
        return true;
    }

    public static bool TryCreate(HotkeyModifiers modifiers, int virtualKey, out HotkeyChord chord)
    {
        chord = default;
        if ((modifiers & ~AllModifiers) != 0)
        {
            return false;
        }

        if (!KeysByVirtualKey.ContainsKey(virtualKey))
        {
            return false;
        }

        chord = new HotkeyChord(modifiers, virtualKey);
        return true;
    }

    public static string Format(HotkeyChord chord)
    {
        if (!KeysByVirtualKey.TryGetValue(chord.VirtualKey, out var key))
        {
            return string.Empty;
        }

        var segments = new List<string>(5);
        if ((chord.Modifiers & HotkeyModifiers.Control) != 0)
        {
            segments.Add("Ctrl");
        }

        if ((chord.Modifiers & HotkeyModifiers.Alt) != 0)
        {
            segments.Add("Alt");
        }

        if ((chord.Modifiers & HotkeyModifiers.Shift) != 0)
        {
            segments.Add("Shift");
        }

        if ((chord.Modifiers & HotkeyModifiers.Windows) != 0)
        {
            segments.Add("Win");
        }

        segments.Add(key.Name);
        return string.Join('+', segments);
    }

    public static string Format(HotkeySequence sequence)
    {
        ArgumentNullException.ThrowIfNull(sequence);
        if (sequence.IsEmpty)
        {
            return string.Empty;
        }

        var steps = new string[sequence.Count];
        for (var index = 0; index < sequence.Count; index++)
        {
            steps[index] = Format(sequence.Steps[index]);
        }

        return string.Join(", ", steps);
    }

    public static bool IsValid(string? text)
    {
        return TryParse(text, out _);
    }

    public static bool TryGetKeyInfo(int virtualKey, out string name, out bool isExtended)
    {
        if (KeysByVirtualKey.TryGetValue(virtualKey, out var key))
        {
            name = key.Name;
            isExtended = key.IsExtended;
            return true;
        }

        name = string.Empty;
        isExtended = false;
        return false;
    }

    public static bool IsModifierKey(int virtualKey)
    {
        return virtualKey is VirtualKeyShift
            or VirtualKeyControl
            or VirtualKeyMenu
            or VirtualKeyLeftWindows
            or VirtualKeyRightWindows
            or VirtualKeyLeftShift
            or VirtualKeyRightShift
            or VirtualKeyLeftControl
            or VirtualKeyRightControl
            or VirtualKeyLeftMenu
            or VirtualKeyRightMenu;
    }

    private static Dictionary<string, KeyInfo> CreateKeyMap()
    {
        var map = new Dictionary<string, KeyInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in EnumerateKeys())
        {
            map[key.Name] = key;
            foreach (var alias in key.Aliases)
            {
                map[alias] = key;
            }
        }

        return map;
    }

    private static Dictionary<int, KeyInfo> CreateVirtualKeyMap()
    {
        var map = new Dictionary<int, KeyInfo>();
        foreach (var key in EnumerateKeys())
        {
            map[key.VirtualKey] = key;
        }

        return map;
    }

    private static IEnumerable<KeyInfo> EnumerateKeys()
    {
        for (var virtualKey = 0x41; virtualKey <= 0x5A; virtualKey++)
        {
            yield return new KeyInfo(virtualKey, ((char)virtualKey).ToString(), IsExtended: false, []);
        }

        for (var virtualKey = 0x30; virtualKey <= 0x39; virtualKey++)
        {
            yield return new KeyInfo(virtualKey, ((char)virtualKey).ToString(), IsExtended: false, []);
        }

        for (var index = 1; index <= 24; index++)
        {
            yield return new KeyInfo(0x6F + index, $"F{index}", IsExtended: false, []);
        }

        for (var index = 0; index <= 9; index++)
        {
            yield return new KeyInfo(0x60 + index, $"Numpad{index}", IsExtended: false, []);
        }

        yield return new KeyInfo(0x08, "Backspace", IsExtended: false, ["Back"]);
        yield return new KeyInfo(0x09, "Tab", IsExtended: false, []);
        yield return new KeyInfo(0x0D, "Enter", IsExtended: false, ["Return"]);
        yield return new KeyInfo(0x13, "Pause", IsExtended: false, []);
        yield return new KeyInfo(0x14, "CapsLock", IsExtended: false, []);
        yield return new KeyInfo(0x1B, "Esc", IsExtended: false, ["Escape"]);
        yield return new KeyInfo(0x20, "Space", IsExtended: false, ["Spacebar"]);
        yield return new KeyInfo(0x21, "PageUp", IsExtended: true, ["PgUp"]);
        yield return new KeyInfo(0x22, "PageDown", IsExtended: true, ["PgDn"]);
        yield return new KeyInfo(0x23, "End", IsExtended: true, []);
        yield return new KeyInfo(0x24, "Home", IsExtended: true, []);
        yield return new KeyInfo(0x25, "Left", IsExtended: true, []);
        yield return new KeyInfo(0x26, "Up", IsExtended: true, []);
        yield return new KeyInfo(0x27, "Right", IsExtended: true, []);
        yield return new KeyInfo(0x28, "Down", IsExtended: true, []);
        yield return new KeyInfo(0x2C, "PrintScreen", IsExtended: true, ["PrtSc"]);
        yield return new KeyInfo(0x2D, "Insert", IsExtended: true, ["Ins"]);
        yield return new KeyInfo(0x2E, "Delete", IsExtended: true, ["Del"]);
        yield return new KeyInfo(0x6A, "NumpadMultiply", IsExtended: false, ["NumpadStar"]);
        yield return new KeyInfo(0x6B, "NumpadAdd", IsExtended: false, ["NumpadPlus"]);
        yield return new KeyInfo(0x6D, "NumpadSubtract", IsExtended: false, ["NumpadMinus"]);
        yield return new KeyInfo(0x6E, "NumpadDecimal", IsExtended: false, []);
        yield return new KeyInfo(0x6F, "NumpadDivide", IsExtended: true, ["NumpadSlash"]);
        yield return new KeyInfo(0x90, "NumLock", IsExtended: true, []);
        yield return new KeyInfo(0x91, "ScrollLock", IsExtended: false, []);
        yield return new KeyInfo(0xBA, "Semicolon", IsExtended: false, []);
        yield return new KeyInfo(0xBB, "Equal", IsExtended: false, []);
        yield return new KeyInfo(0xBC, "Comma", IsExtended: false, []);
        yield return new KeyInfo(0xBD, "Minus", IsExtended: false, []);
        yield return new KeyInfo(0xBE, "Period", IsExtended: false, []);
        yield return new KeyInfo(0xBF, "Slash", IsExtended: false, []);
        yield return new KeyInfo(0xC0, "Backtick", IsExtended: false, ["Grave"]);
        yield return new KeyInfo(0xDB, "LeftBracket", IsExtended: false, []);
        yield return new KeyInfo(0xDC, "Backslash", IsExtended: false, []);
        yield return new KeyInfo(0xDD, "RightBracket", IsExtended: false, []);
        yield return new KeyInfo(0xDE, "Quote", IsExtended: false, ["Apostrophe"]);
    }

    private sealed record KeyInfo(int VirtualKey, string Name, bool IsExtended, string[] Aliases);
}
