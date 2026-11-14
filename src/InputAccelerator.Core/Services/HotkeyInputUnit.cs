namespace InputAccelerator.Core.Services;

public readonly record struct HotkeyInputUnit(ushort VirtualKey, bool IsKeyUp, bool IsExtended);
