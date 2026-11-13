using InputAccelerator.Core.Models;
using InputAccelerator.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace InputAccelerator.Services;

public sealed class AppearanceService : IDisposable
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly UISettings _uiSettings;
    private readonly ILogger<AppearanceService> _logger;
    private AppTheme _theme = AppTheme.System;
    private int _backgroundIndex = FloatingBackgroundPresets.FollowSystemIndex;
    private bool _disposed;

    public AppearanceService(ILogger<AppearanceService> logger)
    {
        _logger = logger;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _uiSettings = new UISettings();
        _uiSettings.ColorValuesChanged += OnSystemColorsChanged;
    }

    public event EventHandler<AppearanceChangedEventArgs>? Changed;

    public AppTheme Theme => _theme;

    public int BackgroundIndex => _backgroundIndex;

    public ElementTheme CurrentElementTheme => ResolveElementTheme(_theme, IsSystemDark());

    public void Apply(AppTheme theme, int backgroundIndex)
    {
        _theme = Enum.IsDefined(theme) ? theme : AppTheme.System;
        _backgroundIndex = FloatingBackgroundPresets.IsValidIndex(backgroundIndex)
            ? backgroundIndex
            : FloatingBackgroundPresets.FollowSystemIndex;
        RaiseChanged();
    }

    private void OnSystemColorsChanged(UISettings sender, object args)
    {
        if (_theme != AppTheme.System)
        {
            return;
        }

        var enqueued = _dispatcherQueue.TryEnqueue(() =>
        {
            if (_disposed || _theme != AppTheme.System)
            {
                return;
            }

            RaiseChanged();
        });

        if (!enqueued)
        {
            _logger.LogWarning("無法將系統主題變更派送到 UI 執行緒。");
        }
    }

    private void RaiseChanged()
    {
        var elementTheme = ResolveElementTheme(_theme, IsSystemDark());
        var palette = CreatePalette(elementTheme, _backgroundIndex);
        Changed?.Invoke(this, new AppearanceChangedEventArgs(elementTheme, palette));
    }

    private bool IsSystemDark()
    {
        var background = _uiSettings.GetColorValue(UIColorType.Background);
        return background.R + background.G + background.B < 384;
    }

    private static ElementTheme ResolveElementTheme(AppTheme theme, bool systemDark)
    {
        return theme switch
        {
            AppTheme.Light => ElementTheme.Light,
            AppTheme.Dark => ElementTheme.Dark,
            _ => systemDark ? ElementTheme.Dark : ElementTheme.Light,
        };
    }

    private static FloatingPalette CreatePalette(ElementTheme theme, int backgroundIndex)
    {
        if (FloatingBackgroundPresets.Get(backgroundIndex) is { } preset)
        {
            var background = ColorUtilities.Parse(preset.BackgroundColor);
            var foreground = ColorUtilities.Parse(preset.ForegroundColor);
            var chipOverlay = preset.IsDark
                ? Color.FromArgb(0x24, 0xFF, 0xFF, 0xFF)
                : Color.FromArgb(0x0F, 0x00, 0x00, 0x00);
            var chipStroke = preset.IsDark
                ? Color.FromArgb(0x3D, 0xFF, 0xFF, 0xFF)
                : Color.FromArgb(0x24, 0x00, 0x00, 0x00);

            return new FloatingPalette(
                background: CreateBrush(background),
                windowBorder: CreateBrush(ColorUtilities.WithAlpha(foreground, 0x3D)),
                foreground: CreateBrush(foreground),
                mutedForeground: CreateBrush(ColorUtilities.WithAlpha(foreground, 0xB3)),
                chipBackground: CreateBrush(chipOverlay),
                chipBorder: CreateBrush(chipStroke));
        }

        var isDark = theme == ElementTheme.Dark;
        var autoBackground = isDark
            ? Color.FromArgb(0xFF, 0x20, 0x20, 0x20)
            : Color.FromArgb(0xFF, 0xF3, 0xF3, 0xF3);
        var autoForeground = isDark
            ? Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0xFF, 0x1B, 0x1B, 0x1B);
        var autoChipBackground = isDark
            ? Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0x0F, 0x00, 0x00, 0x00);
        var autoChipBorder = isDark
            ? Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)
            : Color.FromArgb(0x24, 0x00, 0x00, 0x00);

        return new FloatingPalette(
            background: CreateBrush(autoBackground),
            windowBorder: CreateBrush(isDark
                ? Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)
                : Color.FromArgb(0x1F, 0x00, 0x00, 0x00)),
            foreground: CreateBrush(autoForeground),
            mutedForeground: CreateBrush(ColorUtilities.WithAlpha(autoForeground, 0xB3)),
            chipBackground: CreateBrush(autoChipBackground),
            chipBorder: CreateBrush(autoChipBorder));
    }

    private static SolidColorBrush CreateBrush(Color color)
    {
        return new SolidColorBrush(color);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _uiSettings.ColorValuesChanged -= OnSystemColorsChanged;
    }
}

public sealed class AppearanceChangedEventArgs : EventArgs
{
    public AppearanceChangedEventArgs(ElementTheme elementTheme, FloatingPalette palette)
    {
        ElementTheme = elementTheme;
        Palette = palette;
    }

    public ElementTheme ElementTheme { get; }

    public FloatingPalette Palette { get; }
}

public sealed class FloatingPalette
{
    public FloatingPalette(
        Brush background,
        Brush windowBorder,
        Brush foreground,
        Brush mutedForeground,
        Brush chipBackground,
        Brush chipBorder)
    {
        WindowBackground = background;
        WindowBorder = windowBorder;
        Foreground = foreground;
        MutedForeground = mutedForeground;
        ChipBackground = chipBackground;
        ChipBorder = chipBorder;
    }

    public Brush WindowBackground { get; }

    public Brush WindowBorder { get; }

    public Brush Foreground { get; }

    public Brush MutedForeground { get; }

    public Brush ChipBackground { get; }

    public Brush ChipBorder { get; }
}
