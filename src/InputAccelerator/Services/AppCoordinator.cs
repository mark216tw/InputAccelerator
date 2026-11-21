using InputAccelerator.Core.Services;
using InputAccelerator.Helpers;
using InputAccelerator.ViewModels;
using InputAccelerator.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace InputAccelerator.Services;

public sealed class AppCoordinator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MainViewModel _mainViewModel;
    private readonly EntryListViewModel _entryList;
    private readonly EntryRepository _entryRepository;
    private readonly SettingsService _settings;
    private readonly AppearanceService _appearance;
    private readonly TrayIconService _tray;
    private readonly SingleInstanceService _singleInstance;
    private readonly MemoryTrimService _memoryTrim;
    private readonly ILogger<AppCoordinator> _logger;
    private MainWindow? _mainWindow;
    private MainWindowPlacementManager? _mainWindowPlacementManager;
    private FloatingWindow? _floatingWindow;
    private FloatingWindowPlacementManager? _placementManager;
    private InputInjectionService? _injectionService;
    private bool _initialized;
    private bool _isExiting;

    public AppCoordinator(
        IServiceProvider serviceProvider,
        MainViewModel mainViewModel,
        EntryListViewModel entryList,
        EntryRepository entryRepository,
        SettingsService settings,
        AppearanceService appearance,
        TrayIconService tray,
        SingleInstanceService singleInstance,
        MemoryTrimService memoryTrim,
        ILogger<AppCoordinator> logger)
    {
        _serviceProvider = serviceProvider;
        _mainViewModel = mainViewModel;
        _entryList = entryList;
        _entryRepository = entryRepository;
        _settings = settings;
        _appearance = appearance;
        _tray = tray;
        _singleInstance = singleInstance;
        _memoryTrim = memoryTrim;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        var settingsTask = _settings.LoadAsync();
        var entriesTask = _entryRepository.LoadAsync();
        await Task.WhenAll(settingsTask, entriesTask);

        _entryList.Load(entriesTask.Result);
        _mainViewModel.LoadFromSettings(_settings.Current);
        _appearance.Apply(_settings.Current.Theme, _settings.Current.FloatingBackgroundIndex);

        _mainViewModel.OverlayVisibilityChanged += OnOverlayVisibilityChanged;
        _singleInstance.ActivationRequested += OnActivationRequested;

        _tray.ShowManagerRequested += OnShowManagerRequested;
        _tray.ToggleOverlayRequested += OnToggleOverlayRequested;
        _tray.ExitRequested += OnExitRequested;
        _tray.Initialize();
        _tray.SetOverlayState(_mainViewModel.IsOverlayEnabled);

        _mainViewModel.SelectedEntry = _entryList.Entries.FirstOrDefault();
        ShowMainWindow();

        if (_mainViewModel.IsOverlayEnabled)
        {
            var dispatcher = DispatcherQueue.GetForCurrentThread();
            if (dispatcher is not null)
            {
                dispatcher.TryEnqueue(DispatcherQueuePriority.Low, ShowInitialFloatingWindow);
            }
            else
            {
                ShowInitialFloatingWindow();
            }
        }

        _initialized = true;
        _logger.LogInformation("快速輸入已啟動。");
    }

    private void ShowInitialFloatingWindow()
    {
        if (_isExiting || !_mainViewModel.IsOverlayEnabled)
        {
            return;
        }

        var floatingWindow = EnsureFloatingWindow();
        floatingWindow.ShowWithoutActivation();
    }

    private void ShowMainWindow()
    {
        if (_isExiting)
        {
            return;
        }

        _memoryTrim.CancelPendingTrim();

        if (_mainWindow is null)
        {
            _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            _mainWindow.AppWindow.Closing += OnMainWindowClosing;
            _mainWindow.Closed += OnMainWindowClosed;

            _mainWindowPlacementManager = ActivatorUtilities.CreateInstance<MainWindowPlacementManager>(
                _serviceProvider,
                _mainWindow);
            _mainWindowPlacementManager.Attach();
        }

        _mainWindow.ShowManager();
    }

    private FloatingWindow EnsureFloatingWindow()
    {
        if (_floatingWindow is not null)
        {
            return _floatingWindow;
        }

        _injectionService = _serviceProvider.GetRequiredService<InputInjectionService>();
        _injectionService.InjectionFailed += OnInjectionFailed;

        _floatingWindow = _serviceProvider.GetRequiredService<FloatingWindow>();
        _floatingWindow.OpenMainWindowRequested += OnOpenMainWindowRequested;
        _floatingWindow.CollapseStateChanged += OnFloatingWindowCollapseStateChanged;
        _floatingWindow.AppWindow.Closing += OnFloatingWindowClosing;

        _placementManager = _serviceProvider.GetRequiredService<FloatingWindowPlacementManager>();
        _placementManager.Attach();

        return _floatingWindow;
    }

    private void OnOverlayVisibilityChanged(object? sender, bool isVisible)
    {
        _tray.SetOverlayState(isVisible);
        if (isVisible)
        {
            _memoryTrim.CancelPendingTrim();
            var floatingWindow = EnsureFloatingWindow();
            _placementManager?.EnsureVisible();
            floatingWindow.ShowWithoutActivation();
        }
        else
        {
            _floatingWindow?.HideBar();
            _memoryTrim.ScheduleTrim();
        }
    }

    private void OnFloatingWindowCollapseStateChanged(object? sender, bool isCollapsed)
    {
        if (isCollapsed)
        {
            _memoryTrim.ScheduleTrim();
        }
    }

    private void OnActivationRequested(object? sender, EventArgs e)
    {
        ShowMainWindow();
    }

    private void OnOpenMainWindowRequested(object? sender, EventArgs e)
    {
        ShowMainWindow();
    }

    private void OnShowManagerRequested(object? sender, EventArgs e)
    {
        ShowMainWindow();
    }

    private void OnToggleOverlayRequested(object? sender, EventArgs e)
    {
        _mainViewModel.IsOverlayEnabled = !_mainViewModel.IsOverlayEnabled;
    }

    private async void OnExitRequested(object? sender, EventArgs e)
    {
        await ExitAsync();
    }

    private void OnInjectionFailed(object? sender, EventArgs e)
    {
        _tray.ShowBalloon(
            Strings.Get("TrayInjectionFailedTitle", "無法輸入文字"),
            Strings.Get("TrayInjectionFailedMessage", "找不到可輸入的視窗，或目標程式權限較高。"));
    }

    private void OnMainWindowClosing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        if (_isExiting)
        {
            return;
        }

        if (_floatingWindow is not null)
        {
            _mainWindow?.CloseSettingsIfOpen();
            return;
        }

        args.Cancel = true;
        _ = _mainWindowPlacementManager?.FlushAsync();
        _mainWindow?.HideToTray();
        _memoryTrim.ScheduleTrim();
    }

    private void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        if (sender is MainWindow mainWindow)
        {
            mainWindow.AppWindow.Closing -= OnMainWindowClosing;
            mainWindow.Closed -= OnMainWindowClosed;
            if (ReferenceEquals(_mainWindow, mainWindow))
            {
                _mainWindowPlacementManager?.Dispose();
                _mainWindowPlacementManager = null;
                _mainWindow = null;
            }
        }

        if (!_isExiting)
        {
            _memoryTrim.ScheduleTrim();
        }
    }

    private void OnFloatingWindowClosing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        if (_isExiting)
        {
            return;
        }

        args.Cancel = true;
        _floatingWindow?.HideBar();
        _memoryTrim.ScheduleTrim();
    }

    public async Task ExitAsync()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;

        try
        {
            if (_mainWindowPlacementManager is not null)
            {
                await _mainWindowPlacementManager.FlushAsync();
            }

            if (_placementManager is not null)
            {
                await _placementManager.FlushAsync();
            }

            await _entryList.SaveNowAsync();
            await _settings.SaveAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "結束前儲存資料失敗。");
        }

        if (_injectionService is not null)
        {
            _injectionService.InjectionFailed -= OnInjectionFailed;
        }

        if (_floatingWindow is not null)
        {
            _floatingWindow.OpenMainWindowRequested -= OnOpenMainWindowRequested;
            _floatingWindow.CollapseStateChanged -= OnFloatingWindowCollapseStateChanged;
            _floatingWindow.AppWindow.Closing -= OnFloatingWindowClosing;
        }

        _memoryTrim.Dispose();
        _mainWindowPlacementManager?.Dispose();
        _placementManager?.Dispose();
        _tray.Dispose();
        _singleInstance.Dispose();

        _mainWindow?.Close();
        _floatingWindow?.Close();
        Application.Current.Exit();
    }
}
