using InputAccelerator.Core.Services;
using InputAccelerator.Helpers;
using InputAccelerator.ViewModels;
using InputAccelerator.Views;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace InputAccelerator.Services;

public sealed class AppCoordinator
{
    private readonly MainWindow _mainWindow;
    private readonly FloatingWindow _floatingWindow;
    private readonly MainViewModel _mainViewModel;
    private readonly EntryListViewModel _entryList;
    private readonly EntryRepository _entryRepository;
    private readonly SettingsService _settings;
    private readonly AppearanceService _appearance;
    private readonly TrayIconService _tray;
    private readonly SingleInstanceService _singleInstance;
    private readonly InputInjectionService _injectionService;
    private readonly FloatingWindowPlacementManager _placementManager;
    private readonly ILogger<AppCoordinator> _logger;
    private bool _initialized;
    private bool _isExiting;

    public AppCoordinator(
        MainWindow mainWindow,
        FloatingWindow floatingWindow,
        MainViewModel mainViewModel,
        EntryListViewModel entryList,
        EntryRepository entryRepository,
        SettingsService settings,
        AppearanceService appearance,
        TrayIconService tray,
        SingleInstanceService singleInstance,
        InputInjectionService injectionService,
        FloatingWindowPlacementManager placementManager,
        ILogger<AppCoordinator> logger)
    {
        _mainWindow = mainWindow;
        _floatingWindow = floatingWindow;
        _mainViewModel = mainViewModel;
        _entryList = entryList;
        _entryRepository = entryRepository;
        _settings = settings;
        _appearance = appearance;
        _tray = tray;
        _singleInstance = singleInstance;
        _injectionService = injectionService;
        _placementManager = placementManager;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _settings.LoadAsync();
        _entryList.Load(await _entryRepository.LoadAsync());
        _mainViewModel.LoadFromSettings(_settings.Current);
        _appearance.Apply(_settings.Current.Theme, _settings.Current.FloatingBackgroundIndex);

        _mainViewModel.OverlayVisibilityChanged += OnOverlayVisibilityChanged;
        _floatingWindow.OpenMainWindowRequested += OnOpenMainWindowRequested;
        _singleInstance.ActivationRequested += OnActivationRequested;
        _injectionService.InjectionFailed += OnInjectionFailed;

        _tray.ShowManagerRequested += OnShowManagerRequested;
        _tray.ToggleOverlayRequested += OnToggleOverlayRequested;
        _tray.ExitRequested += OnExitRequested;
        _tray.Initialize();
        _tray.SetOverlayState(_mainViewModel.IsOverlayEnabled);

        _placementManager.Attach();

        _mainWindow.AppWindow.Closing += OnMainWindowClosing;
        _floatingWindow.AppWindow.Closing += OnFloatingWindowClosing;

        _mainViewModel.SelectedEntry = _entryList.Entries.FirstOrDefault();
        _mainWindow.ShowManager();

        if (_mainViewModel.IsOverlayEnabled)
        {
            _floatingWindow.ShowWithoutActivation();
        }

        _initialized = true;
        _logger.LogInformation("快速輸入已啟動。");
    }

    private void OnOverlayVisibilityChanged(object? sender, bool isVisible)
    {
        _tray.SetOverlayState(isVisible);
        if (isVisible)
        {
            _placementManager.EnsureVisible();
            _floatingWindow.ShowWithoutActivation();
        }
        else
        {
            _floatingWindow.HideBar();
        }
    }

    private void OnActivationRequested(object? sender, EventArgs e)
    {
        _mainWindow.ShowManager();
    }

    private void OnOpenMainWindowRequested(object? sender, EventArgs e)
    {
        _mainWindow.ShowManager();
    }

    private void OnShowManagerRequested(object? sender, EventArgs e)
    {
        _mainWindow.ShowManager();
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

        args.Cancel = true;
        _mainWindow.HideToTray();
    }

    private void OnFloatingWindowClosing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        if (_isExiting)
        {
            return;
        }

        args.Cancel = true;
        _floatingWindow.HideBar();
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
            await _placementManager.FlushAsync();
            await _entryList.SaveNowAsync();
            await _settings.SaveAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "結束前儲存資料失敗。");
        }

        _placementManager.Dispose();
        _tray.Dispose();
        _singleInstance.Dispose();

        _mainWindow.Close();
        _floatingWindow.Close();
        Application.Current.Exit();
    }
}
