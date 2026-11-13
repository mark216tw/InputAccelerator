using InputAccelerator.Core.Models;
using InputAccelerator.Helpers;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace InputAccelerator.ViewModels;

public sealed class FloatingBackgroundOption
{
    private FloatingBackgroundOption(int index, string label, Brush previewBrush)
    {
        Index = index;
        Label = label;
        PreviewBrush = previewBrush;
    }

    public int Index { get; }

    public string Label { get; }

    public Brush PreviewBrush { get; }

    public static IReadOnlyList<FloatingBackgroundOption> CreateAll()
    {
        var options = new List<FloatingBackgroundOption>(FloatingBackgroundPresets.All.Count + 1)
        {
            new(
                FloatingBackgroundPresets.FollowSystemIndex,
                Strings.Get("FloatingBackgroundAuto", "跟隨系統"),
                CreateAutoPreviewBrush()),
        };

        for (var index = 0; index < FloatingBackgroundPresets.All.Count; index++)
        {
            var preset = FloatingBackgroundPresets.All[index];
            options.Add(new FloatingBackgroundOption(
                index + 1,
                Strings.Get(preset.Key, preset.Key),
                new SolidColorBrush(ColorUtilities.Parse(preset.BackgroundColor))));
        }

        return options;
    }

    private static Brush CreateAutoPreviewBrush()
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
        };
        brush.GradientStops.Add(new GradientStop
        {
            Color = Color.FromArgb(0xFF, 0xF3, 0xF3, 0xF3),
            Offset = 0,
        });
        brush.GradientStops.Add(new GradientStop
        {
            Color = Color.FromArgb(0xFF, 0x20, 0x20, 0x20),
            Offset = 1,
        });
        return brush;
    }
}
