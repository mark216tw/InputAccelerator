using InputAccelerator.Core.Models;

namespace InputAccelerator.Core.Services;

public readonly record struct HotkeyInputUnit(ushort VirtualKey, bool IsKeyUp, bool IsExtended);

public static class HotkeyInputComposer
{
    public const ushort VirtualKeyShift = 0x10;

    public const ushort VirtualKeyControl = 0x11;

    public const ushort VirtualKeyMenu = 0x12;

    public const ushort VirtualKeyLeftWindows = 0x5B;

    public static IReadOnlyList<HotkeyInputUnit> Compose(HotkeyChord chord)
    {
        if (!HotkeyParser.TryGetKeyInfo(chord.VirtualKey, out _, out var isExtended))
        {
            return Array.Empty<HotkeyInputUnit>();
        }

        var modifiers = GetModifierKeys(chord.Modifiers);
        var units = new List<HotkeyInputUnit>(modifiers.Count * 2 + 2);
        foreach (var modifier in modifiers)
        {
            units.Add(new HotkeyInputUnit(modifier.VirtualKey, IsKeyUp: false, modifier.IsExtended));
        }

        var key = (ushort)chord.VirtualKey;
        units.Add(new HotkeyInputUnit(key, IsKeyUp: false, isExtended));
        units.Add(new HotkeyInputUnit(key, IsKeyUp: true, isExtended));

        for (var index = modifiers.Count - 1; index >= 0; index--)
        {
            units.Add(new HotkeyInputUnit(modifiers[index].VirtualKey, IsKeyUp: true, modifiers[index].IsExtended));
        }

        return units;
    }

    private static List<ModifierKey> GetModifierKeys(HotkeyModifiers modifiers)
    {
        var keys = new List<ModifierKey>(4);
        if ((modifiers & HotkeyModifiers.Control) != 0)
        {
            keys.Add(new ModifierKey(VirtualKeyControl, IsExtended: false));
        }

        if ((modifiers & HotkeyModifiers.Alt) != 0)
        {
            keys.Add(new ModifierKey(VirtualKeyMenu, IsExtended: false));
        }

        if ((modifiers & HotkeyModifiers.Shift) != 0)
        {
            keys.Add(new ModifierKey(VirtualKeyShift, IsExtended: false));
        }

        if ((modifiers & HotkeyModifiers.Windows) != 0)
        {
            keys.Add(new ModifierKey(VirtualKeyLeftWindows, IsExtended: true));
        }

        return keys;
    }

    private readonly record struct ModifierKey(ushort VirtualKey, bool IsExtended);
}
