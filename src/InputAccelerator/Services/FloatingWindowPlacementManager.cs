using InputAccelerator.Core.Models;
using InputAccelerator.Core.Services;
using InputAccelerator.Interop;
using InputAccelerator.Views;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace InputAccelerator.Services;

public sealed class FloatingWindowPlacementManager : IDisposable
{
    private const int MinimumWidthDip = 140;
    private const int MinimumHeightDip = 80;
    private const int DefaultWidthDip = 220;
    private const int DefaultHeightDip = 340;

    private readonly FloatingWindow _window;
    private readonly SettingsService _settings;
    private readonly ILogger<FloatingWindowPlacementManager> _logger;
    private readonly DispatcherQueueTimer _saveTimer;
    private WindowPlacement? _expandedPlacement;
    private bool _suppressChanges;
    private bool _isCollapsed;
    private bool _disposed;

    public FloatingWindowPlacementManager(
        FloatingWindow window,
        SettingsService settings,
        ILogger<FloatingWindowPlacementManager> logger)
    {
        _window = window;
        _settings = settings;
        _logger = logger;

        var dispatcher = DispatcherQueue.GetForCurrentThread();
        _saveTimer = dispatcher.CreateTimer();
        _saveTimer.Interval = TimeSpan.FromMilliseconds(500);
        _saveTimer.IsRepeating = false;
        _saveTimer.Tick += OnSaveTimerTick;
    }

    public void Attach()
    {
        ApplySavedPlacement();
        _window.AppWindow.Changed += OnWindowChanged;
        _window.CollapseStateChanged += OnCollapseStateChanged;
    }

    public void EnsureVisible()
    {
        if (_isCollapsed)
        {
            return;
        }

        var placement = WindowPlacementValidator.Normalize(
            GetCurrentPlacement(),
            GetDisplayAreas(),
            Scale(MinimumWidthDip),
            Scale(MinimumHeightDip),
            Scale(DefaultWidthDip),
            Scale(DefaultHeightDip));

        ApplyPlacement(placement);
    }

    public async Task FlushAsync()
    {
        _saveTimer.Stop();
        await SaveCurrentPlacementAsync().ConfigureAwait(true);
    }

    private void ApplySavedPlacement()
    {
        var saved = _settings.Current.FloatingWindowPlacement;
        var normalized = WindowPlacementValidator.Normalize(
            saved,
            GetDisplayAreas(),
            Scale(MinimumWidthDip),
            Scale(MinimumHeightDip),
            Scale(DefaultWidthDip),
            Scale(DefaultHeightDip));

        ApplyPlacement(normalized);
    }

    private void ApplyPlacement(WindowPlacement placement)
    {
        _suppressChanges = true;
        try
        {
            _window.ApplyPlacement(placement);
        }
        finally
        {
            _suppressChanges = false;
        }
    }

    private void OnCollapseStateChanged(object? sender, bool collapsed)
    {
        if (collapsed)
        {
            _saveTimer.Stop();
            _expandedPlacement = GetCurrentPlacement();
            _isCollapsed = true;
            _ = SaveCurrentPlacementAsync();
        }
        else
        {
            _isCollapsed = false;
            EnsureVisible();
        }
    }

    private void OnWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (_suppressChanges || _isCollapsed || (!args.DidPositionChange && !args.DidSizeChange))
        {
            return;
        }

        var placement = GetCurrentPlacement();
        _logger.LogDebug(
            "浮動視窗變更：位置 {X},{Y}，大小 {Width}x{Height}，位置變更={PositionChanged}，大小變更={SizeChanged}。",
            placement.X,
            placement.Y,
            placement.Width,
            placement.Height,
            args.DidPositionChange,
            args.DidSizeChange);

        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private async void OnSaveTimerTick(DispatcherQueueTimer sender, object args)
    {
        await SaveCurrentPlacementAsync();
    }

    private async Task SaveCurrentPlacementAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            var placement = _isCollapsed
                ? _expandedPlacement ?? GetCurrentPlacement()
                : GetCurrentPlacement();
            _settings.UpdateFloatingWindowPlacement(placement);
            await _settings.SaveAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "儲存浮動視窗位置失敗。");
        }
    }

    private WindowPlacement GetCurrentPlacement()
    {
        var position = _window.AppWindow.Position;
        var size = _window.AppWindow.Size;
        return new WindowPlacement
        {
            X = position.X,
            Y = position.Y,
            Width = size.Width,
            Height = size.Height,
        };
    }

    private int Scale(int dip)
    {
        var dpi = NativeMethods.GetDpiForWindow(_window.WindowHandle);
        var scale = dpi == 0 ? 1.0 : dpi / 96.0;
        return Math.Max(1, (int)Math.Round(dip * scale));
    }

    private static IReadOnlyList<DisplayAreaInfo> GetDisplayAreas()
    {
        var monitors = new List<(DisplayAreaInfo Area, bool IsPrimary)>();

        bool Enumerate(IntPtr monitor, IntPtr deviceContext, ref NativeMethods.Rect monitorRect, IntPtr data)
        {
            var info = new NativeMethods.MonitorInfo
            {
                Size = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MonitorInfo>(),
            };

            if (NativeMethods.GetMonitorInfo(monitor, ref info))
            {
                monitors.Add((
                    new DisplayAreaInfo(
                        new PixelRect(
                            info.Monitor.Left,
                            info.Monitor.Top,
                            info.Monitor.Width,
                            info.Monitor.Height),
                        new PixelRect(
                            info.Work.Left,
                            info.Work.Top,
                            info.Work.Width,
                            info.Work.Height)),
                    (info.Flags & NativeMethods.MonitorInfoPrimary) != 0));
            }

            return true;
        }

        NativeMethods.MonitorEnumProc callback = Enumerate;
        NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
        GC.KeepAlive(callback);

        var areas = new List<DisplayAreaInfo>(monitors.Count);
        foreach (var monitor in monitors)
        {
            if (monitor.IsPrimary)
            {
                areas.Add(monitor.Area);
            }
        }

        foreach (var monitor in monitors)
        {
            if (!monitor.IsPrimary)
            {
                areas.Add(monitor.Area);
            }
        }

        if (areas.Count == 0)
        {
            var width = Math.Max(1, NativeMethods.GetSystemMetrics(NativeMethods.SmCxScreen));
            var height = Math.Max(1, NativeMethods.GetSystemMetrics(NativeMethods.SmCyScreen));
            areas.Add(new DisplayAreaInfo(
                new PixelRect(0, 0, width, height),
                new PixelRect(0, 0, width, height)));
        }

        return areas;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _saveTimer.Stop();
        _saveTimer.Tick -= OnSaveTimerTick;
        _window.AppWindow.Changed -= OnWindowChanged;
        _window.CollapseStateChanged -= OnCollapseStateChanged;
    }
}
