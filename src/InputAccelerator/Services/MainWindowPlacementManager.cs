using InputAccelerator.Core.Models;
using InputAccelerator.Core.Services;
using InputAccelerator.Interop;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;

namespace InputAccelerator.Services;

public sealed class MainWindowPlacementManager : IDisposable
{
    private readonly MainWindow _window;
    private readonly SettingsService _settings;
    private readonly ILogger<MainWindowPlacementManager> _logger;
    private readonly DispatcherQueueTimer _saveTimer;
    private WindowPlacement? _normalPlacement;
    private bool _suppressChanges;
    private bool _disposed;

    public MainWindowPlacementManager(
        MainWindow window,
        SettingsService settings,
        ILogger<MainWindowPlacementManager> logger)
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
    }

    public async Task FlushAsync()
    {
        _saveTimer.Stop();
        await SaveCurrentPlacementAsync().ConfigureAwait(true);
    }

    private void ApplySavedPlacement()
    {
        var saved = _settings.Current.MainWindowPlacement;
        var normalized = WindowPlacementValidator.Normalize(
            saved,
            DisplayAreaEnumerator.Enumerate(),
            Scale(MainWindow.MinimumWidthDip),
            Scale(MainWindow.MinimumHeightDip),
            Scale(MainWindow.DefaultWidthDip),
            Scale(MainWindow.DefaultHeightDip));

        _normalPlacement = normalized.Clone();
        ApplyPlacement(normalized);

        if (_settings.Current.MainWindowMaximized &&
            _window.AppWindow.Presenter is OverlappedPresenter { IsMaximizable: true } presenter)
        {
            _suppressChanges = true;
            try
            {
                presenter.Maximize();
            }
            finally
            {
                _suppressChanges = false;
            }
        }
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

    private void OnWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (_suppressChanges || _disposed)
        {
            return;
        }

        var state = (sender.Presenter as OverlappedPresenter)?.State ?? OverlappedPresenterState.Restored;
        if (state == OverlappedPresenterState.Minimized)
        {
            return;
        }

        if (state == OverlappedPresenterState.Maximized)
        {
            if (!args.DidPresenterChange && !args.DidSizeChange && !args.DidPositionChange)
            {
                return;
            }

            _settings.UpdateMainWindowPlacement(_normalPlacement ?? GetCurrentPlacement(), true);
            ScheduleSave();
            return;
        }

        if (!args.DidPositionChange && !args.DidSizeChange && !args.DidPresenterChange)
        {
            return;
        }

        var placement = GetCurrentPlacement();
        _normalPlacement = placement;
        _settings.UpdateMainWindowPlacement(placement, false);

        _logger.LogDebug(
            "主視窗變更：位置 {X},{Y}，大小 {Width}x{Height}。",
            placement.X,
            placement.Y,
            placement.Width,
            placement.Height);

        ScheduleSave();
    }

    private void ScheduleSave()
    {
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
            var state = (_window.AppWindow.Presenter as OverlappedPresenter)?.State
                ?? OverlappedPresenterState.Restored;
            var isMaximized = state == OverlappedPresenterState.Maximized;
            var placement = isMaximized
                ? _normalPlacement ?? GetCurrentPlacement()
                : GetCurrentPlacement();

            _normalPlacement = placement;
            _settings.UpdateMainWindowPlacement(placement, isMaximized);
            await _settings.SaveAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "儲存主視窗位置失敗。");
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
        var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(_window);
        var dpi = NativeMethods.GetDpiForWindow(windowHandle);
        var scale = dpi == 0 ? 1.0 : dpi / 96.0;
        return Math.Max(1, (int)Math.Round(dip * scale));
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
    }
}
