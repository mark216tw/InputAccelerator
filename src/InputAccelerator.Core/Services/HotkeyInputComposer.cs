using InputAccelerator.Core.Models;

namespace InputAccelerator.Core.Services;

public static class HotkeyInputComposer
{
    public const ushort VirtualKeyShift = 0x10;

    public const ushort VirtualKeyControl = 0x11;

    public const ushort VirtualKeyMenu = 0x12;

    public const ushort VirtualKeyLeftWindows = 0x5B;

    public const int InterStepDelayMilliseconds = 150;

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

    public static IReadOnlyList<HotkeyInputBatch> ComposeSequence(HotkeySequence sequence)
    {
        ArgumentNullException.ThrowIfNull(sequence);
        if (sequence.IsEmpty)
        {
            return Array.Empty<HotkeyInputBatch>();
        }

        var extendedFlags = new bool[sequence.Count];
        for (var index = 0; index < sequence.Count; index++)
        {
            if (!HotkeyParser.TryGetKeyInfo(sequence.Steps[index].VirtualKey, out _, out extendedFlags[index]))
            {
                return Array.Empty<HotkeyInputBatch>();
            }
        }

        var heldModifiers = new List<ModifierKey>(4);
        var batches = new List<HotkeyInputBatch>(sequence.Count);
        for (var index = 0; index < sequence.Count; index++)
        {
            var chord = sequence.Steps[index];
            var requiredModifiers = GetModifierKeys(chord.Modifiers);
            var nextModifiers = index + 1 < sequence.Count
                ? GetModifierKeys(sequence.Steps[index + 1].Modifiers)
                : new List<ModifierKey>(0);

            var units = new List<HotkeyInputUnit>(8);

            ReleaseModifiers(heldModifiers, requiredModifiers, units);

            foreach (var modifier in requiredModifiers)
            {
                if (!heldModifiers.Contains(modifier))
                {
                    units.Add(new HotkeyInputUnit(modifier.VirtualKey, IsKeyUp: false, modifier.IsExtended));
                    heldModifiers.Add(modifier);
                }
            }

            var key = (ushort)chord.VirtualKey;
            units.Add(new HotkeyInputUnit(key, IsKeyUp: false, extendedFlags[index]));
            units.Add(new HotkeyInputUnit(key, IsKeyUp: true, extendedFlags[index]));

            ReleaseModifiers(heldModifiers, nextModifiers, units);

            var delay = index + 1 < sequence.Count ? InterStepDelayMilliseconds : 0;
            batches.Add(new HotkeyInputBatch(units, delay));
        }

        return batches;
    }

    private static void ReleaseModifiers(
        List<ModifierKey> heldModifiers,
        List<ModifierKey> keepModifiers,
        List<HotkeyInputUnit> units)
    {
        for (var index = heldModifiers.Count - 1; index >= 0; index--)
        {
            var modifier = heldModifiers[index];
            if (keepModifiers.Contains(modifier))
            {
                continue;
            }

            units.Add(new HotkeyInputUnit(modifier.VirtualKey, IsKeyUp: true, modifier.IsExtended));
            heldModifiers.RemoveAt(index);
        }
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
