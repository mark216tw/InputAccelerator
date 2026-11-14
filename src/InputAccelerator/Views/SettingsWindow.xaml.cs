using InputAccelerator.Helpers;
using InputAccelerator.Interop;
using InputAccelerator.Services;
using InputAccelerator.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace InputAccelerator.Views;

public sealed partial class SettingsWindow : Window
{
    private const int DefaultWidthDip = 520;
    private const int DefaultHeightDip = 580;

    private readonly AppearanceService _appearance;
    private bool _isClosing;
    private bool _isSaving;
    private bool _needsReload = true;

    public SettingsWindow(SettingsViewModel viewModel, AppearanceService appearance)
    {
        InitializeComponent();
        ViewModel = viewModel;
        _appearance = appearance;

        Title = Strings.Get("SettingsWindowTitle", "設定");
        if (File.Exists(AppPaths.IconFile))
        {
            AppWindow.SetIcon(AppPaths.IconFile);
        }

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }

        RootGrid.RequestedTheme = _appearance.CurrentElementTheme;
        _appearance.Changed += OnAppearanceChanged;
        AppWindow.Closing += OnClosing;
    }

    public SettingsViewModel ViewModel { get; }

    public void ShowSettings(Window owner)
    {
        if (_needsReload)
        {
            ViewModel.Reload();
            _needsReload = false;
        }

        ConfigureWindowGeometry(owner);
        AppWindow.Show(activateWindow: true);
        Activate();
    }

    public void HideSettings()
    {
        AppWindow.Hide();
    }

    private void OnAppearanceChanged(object? sender, AppearanceChangedEventArgs e)
    {
        RootGrid.RequestedTheme = e.ElementTheme;
    }

    private void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        args.Cancel = true;
        _ = CloseSettingsAsync();
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (_isSaving)
        {
            return;
        }

        _isSaving = true;
        try
        {
            if (await ViewModel.SaveAsync())
            {
                _needsReload = true;
                HideSettings();
            }
            else
            {
                await ShowSaveFailedAsync();
            }
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async void OnCloseClick(object sender, RoutedEventArgs e)
    {
        await CloseSettingsAsync();
    }

    private async Task CloseSettingsAsync()
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
        try
        {
            if (ViewModel.HasUnsavedChanges && !await ConfirmDiscardAsync())
            {
                return;
            }

            _needsReload = true;
            HideSettings();
        }
        finally
        {
            _isClosing = false;
        }
    }

    private async Task<bool> ConfirmDiscardAsync()
    {
        var dialog = new ContentDialog
        {
            XamlRoot = RootGrid.XamlRoot,
            RequestedTheme = RootGrid.ActualTheme,
            Title = Strings.Get("SettingsDiscardTitle", "尚未儲存"),
            Content = new TextBlock
            {
                Text = Strings.Get("SettingsDiscardBody", "設定尚未儲存，要放棄變更嗎？"),
                TextWrapping = TextWrapping.Wrap,
            },
            PrimaryButtonText = Strings.Get("SettingsDiscardConfirm", "放棄變更"),
            CloseButtonText = Strings.Get("SettingsDiscardCancel", "繼續編輯"),
            DefaultButton = ContentDialogButton.Close,
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    private async Task ShowSaveFailedAsync()
    {
        var dialog = new ContentDialog
        {
            XamlRoot = RootGrid.XamlRoot,
            RequestedTheme = RootGrid.ActualTheme,
            Title = Strings.Get("SettingsSaveFailedTitle", "無法儲存設定"),
            Content = new TextBlock
            {
                Text = Strings.Get("SettingsSaveFailedBody", "請稍後再試。"),
                TextWrapping = TextWrapping.Wrap,
            },
            CloseButtonText = Strings.Get("SettingsSaveFailedClose", "關閉"),
            DefaultButton = ContentDialogButton.Close,
        };

        await dialog.ShowAsync();
    }

    private void ConfigureWindowGeometry(Window owner)
    {
        var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var dpi = NativeMethods.GetDpiForWindow(windowHandle);
        var scale = dpi == 0 ? 1.0 : dpi / 96.0;
        var width = (int)Math.Round(DefaultWidthDip * scale);
        var height = (int)Math.Round(DefaultHeightDip * scale);

        var ownerPosition = owner.AppWindow.Position;
        var ownerSize = owner.AppWindow.Size;
        var workArea = DisplayArea
            .GetFromWindowId(owner.AppWindow.Id, DisplayAreaFallback.Primary)
            .WorkArea;

        width = Math.Min(width, workArea.Width);
        height = Math.Min(height, workArea.Height);

        var x = ownerPosition.X + (ownerSize.Width - width) / 2;
        var y = ownerPosition.Y + (ownerSize.Height - height) / 2;
        var maximumX = Math.Max(workArea.X, workArea.X + workArea.Width - width);
        var maximumY = Math.Max(workArea.Y, workArea.Y + workArea.Height - height);
        x = Math.Clamp(x, workArea.X, maximumX);
        y = Math.Clamp(y, workArea.Y, maximumY);

        AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
    }
}
