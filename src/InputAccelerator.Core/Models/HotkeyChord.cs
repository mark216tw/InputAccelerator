namespace InputAccelerator.Core.Models;

public readonly record struct HotkeyChord(HotkeyModifiers Modifiers, int VirtualKey);
