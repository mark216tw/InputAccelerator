namespace InputAccelerator.Core.Services;

public readonly record struct HotkeyInputBatch(IReadOnlyList<HotkeyInputUnit> Units, int DelayAfterMilliseconds);
