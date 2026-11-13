using InputAccelerator.Helpers;
using InputAccelerator.Interop;
using InputAccelerator.Services;
using InputAccelerator.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace InputAccelerator.Views;

public sealed partial class SettingsWindow : Window
{
    private const int DefaultWidthDip = 520;
    private const int DefaultHeightDip = 400;

    private readonly AppearanceService _appearance;

    public SettingsWindow(MainViewModel viewModel, AppearanceService appearance)
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

    public MainViewModel ViewModel { get; }

    public void ShowSettings(Window owner)
    {
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
        AppWindow.Hide();
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

        var x = ownerPosition.X + (ownerSize.Width - width) / 2;
        var y = ownerPosition.Y + (ownerSize.Height - height) / 2;
        var maximumX = Math.Max(workArea.X, workArea.X + workArea.Width - width);
        var maximumY = Math.Max(workArea.Y, workArea.Y + workArea.Height - height);
        x = Math.Clamp(x, workArea.X, maximumX);
        y = Math.Clamp(y, workArea.Y, maximumY);

        AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
    }
}
