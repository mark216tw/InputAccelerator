namespace InputAccelerator.Core.Models;

public sealed record FloatingBackgroundPreset(
    string Key,
    string BackgroundColor,
    string ForegroundColor,
    bool IsDark);

public static class FloatingBackgroundPresets
{
    public const int FollowSystemIndex = 0;

    public static IReadOnlyList<FloatingBackgroundPreset> All { get; } =
    [
        new("FloatingBackgroundGraphite", "#374151", "#F9FAFB", IsDark: true),
        new("FloatingBackgroundMidnight", "#1E3A8A", "#EFF6FF", IsDark: true),
        new("FloatingBackgroundTeal", "#0F766E", "#ECFDF5", IsDark: true),
        new("FloatingBackgroundForest", "#166534", "#F0FDF4", IsDark: true),
        new("FloatingBackgroundViolet", "#5B21B6", "#F5F3FF", IsDark: true),
        new("FloatingBackgroundRose", "#9F1239", "#FFF1F2", IsDark: true),
        new("FloatingBackgroundEyeCare", "#FEF3C7", "#3F2D00", IsDark: false),
        new("FloatingBackgroundSky", "#DBEAFE", "#1E3A8A", IsDark: false),
        new("FloatingBackgroundMint", "#D1FAE5", "#064E3B", IsDark: false),
        new("FloatingBackgroundGray", "#E5E7EB", "#111827", IsDark: false),
        new("FloatingBackgroundBlush", "#FCE7F3", "#831843", IsDark: false),
        new("FloatingBackgroundLavender", "#EDE9FE", "#4C1D95", IsDark: false),
    ];

    public static int MaxIndex => All.Count;

    public static FloatingBackgroundPreset? Get(int index)
    {
        return index > FollowSystemIndex && index <= All.Count ? All[index - 1] : null;
    }

    public static bool IsValidIndex(int index)
    {
        return index >= FollowSystemIndex && index <= All.Count;
    }
}
