namespace InputAccelerator.Core.Models;

public sealed class AppSettings
{
    public bool IsOverlayEnabled { get; set; } = true;

    public bool AutoEnterAfterSelection { get; set; }

    public InputMode InputMode { get; set; } = InputMode.SimulatedInput;

    public AppTheme Theme { get; set; } = AppTheme.System;

    public int FloatingBackgroundIndex { get; set; } = FloatingBackgroundPresets.FollowSystemIndex;

    public int FloatingAutoCollapseSeconds { get; set; } = FloatingAutoCollapsePresets.DefaultSeconds;

    public WindowPlacement? FloatingWindowPlacement { get; set; }
}
