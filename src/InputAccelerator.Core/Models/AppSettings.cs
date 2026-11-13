namespace InputAccelerator.Core.Models;

public sealed class AppSettings
{
    public bool IsOverlayEnabled { get; set; } = true;

    public InputMode InputMode { get; set; } = InputMode.SimulatedInput;

    public AppTheme Theme { get; set; } = AppTheme.System;

    public int FloatingBackgroundIndex { get; set; } = FloatingBackgroundPresets.FollowSystemIndex;

    public WindowPlacement? FloatingWindowPlacement { get; set; }
}
