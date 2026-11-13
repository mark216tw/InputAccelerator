using InputAccelerator.Helpers;
using InputAccelerator.Interop;
using InputAccelerator.Services;
using InputAccelerator.ViewModels;
using InputAccelerator.Views;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace InputAccelerator;

public sealed partial class MainWindow : Window
{
    private const int DefaultWidthDip = 820;
    private const int DefaultHeightDip = 740;
    private const int MinimumWidthDip = 600;
    private const int MinimumHeightDip = 540;

    private readonly ILogger<MainWindow> _logger;
    private readonly AppearanceService _appearance;
    private readonly SettingsWindow _settingsWindow;
    private readonly MinimumSizeInterop _minimumSizeInterop = new(MinimumWidthDip, MinimumHeightDip);

    public MainWindow(
        MainViewModel viewModel,
        AppearanceService appearance,
        SettingsWindow settingsWindow,
        ILogger<MainWindow> logger)
    {
        InitializeComponent();
        ViewModel = viewModel;
        _appearance = appearance;
        _settingsWindow = settingsWindow;
        _logger = logger;
        ViewModel.Entries.Entries.CollectionChanged += (_, _) => UpdateEmptyState();
        UpdateEmptyState();

        Title = Strings.Get("AppName", "輸入加速");
        if (File.Exists(AppPaths.IconFile))
        {
            AppWindow.SetIcon(AppPaths.IconFile);
        }

        ConfigureWindowGeometry();
        _minimumSizeInterop.Setup(WinRT.Interop.WindowNative.GetWindowHandle(this));
        _appearance.Changed += OnAppearanceChanged;
        Closed += (_, _) =>
        {
            _appearance.Changed -= OnAppearanceChanged;
            _minimumSizeInterop.Dispose();
        };
    }

    public MainViewModel ViewModel { get; }

    private void OnAppearanceChanged(object? sender, AppearanceChangedEventArgs e)
    {
        RootGrid.RequestedTheme = e.ElementTheme;
    }

    public void ShowManager()
    {
        AppWindow.Show();

        var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        NativeMethods.ShowWindow(windowHandle, NativeMethods.SwRestore);
        NativeMethods.SetWindowPos(
            windowHandle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpShowWindow);
        ForceForeground(windowHandle);
        Activate();
    }

    private static void ForceForeground(IntPtr windowHandle)
    {
        var foregroundWindow = NativeMethods.GetForegroundWindow();
        var foregroundThread = foregroundWindow == IntPtr.Zero
            ? 0
            : NativeMethods.GetWindowThreadProcessId(foregroundWindow, out _);
        var currentThread = NativeMethods.GetCurrentThreadId();
        var attached = foregroundThread != 0 &&
                       foregroundThread != currentThread &&
                       NativeMethods.AttachThreadInput(currentThread, foregroundThread, attach: true);

        try
        {
            NativeMethods.SetForegroundWindow(windowHandle);
        }
        finally
        {
            if (attached)
            {
                NativeMethods.AttachThreadInput(currentThread, foregroundThread, attach: false);
            }
        }
    }

    public void HideToTray()
    {
        _settingsWindow.HideSettings();
        AppWindow.Hide();
    }

    private void ConfigureWindowGeometry()
    {
        var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var dpi = NativeMethods.GetDpiForWindow(windowHandle);
        var scale = dpi == 0 ? 1.0 : dpi / 96.0;
        var width = (int)Math.Round(DefaultWidthDip * scale);
        var height = (int)Math.Round(DefaultHeightDip * scale);

        var display = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var workArea = display.WorkArea;
        var x = workArea.X + Math.Max(0, (workArea.Width - width) / 2);
        var y = workArea.Y + Math.Max(0, (workArea.Height - height) / 2);

        AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
    }

    private void OnMoreClick(object sender, RoutedEventArgs e)
    {
        _settingsWindow.ShowSettings(this);
    }

    private async void OnAddClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new EditEntryDialog(Strings.Get("EditEntryDialog.AddTitle", "新增文字"), string.Empty)
            {
                XamlRoot = RootGrid.XamlRoot,
                RequestedTheme = RootGrid.ActualTheme,
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var entry = ViewModel.AddEntry(dialog.EntryText);
                EntryList.ScrollIntoView(entry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "新增文字失敗。");
        }
    }

    private async void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedEntry is not { } selected)
        {
            return;
        }

        try
        {
            var dialog = new EditEntryDialog(Strings.Get("EditEntryDialog.EditTitle", "編輯文字"), selected.Content)
            {
                XamlRoot = RootGrid.XamlRoot,
                RequestedTheme = RootGrid.ActualTheme,
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                ViewModel.UpdateEntry(selected, dialog.EntryText);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "編輯文字失敗。");
        }
    }

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedEntry is not { } selected)
        {
            return;
        }

        try
        {
            var format = Strings.Get("DeleteDialog.Body", "確定要刪除「{0}」嗎？");
            var body = string.Format(System.Globalization.CultureInfo.CurrentCulture, format, selected.Preview);
            var dialog = new ContentDialog
            {
                XamlRoot = RootGrid.XamlRoot,
                RequestedTheme = RootGrid.ActualTheme,
                Title = Strings.Get("DeleteDialog.Title", "刪除文字"),
                Content = new TextBlock { Text = body, TextWrapping = TextWrapping.Wrap },
                PrimaryButtonText = Strings.Get("DeleteDialog.PrimaryButtonText", "刪除"),
                CloseButtonText = Strings.Get("DeleteDialog.CloseButtonText", "取消"),
                DefaultButton = ContentDialogButton.Close,
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                ViewModel.RemoveEntry(selected);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除文字失敗。");
        }
    }

    private void OnMoveUpClick(object sender, RoutedEventArgs e)
    {
        ViewModel.MoveSelected(-1);
    }

    private void OnMoveDownClick(object sender, RoutedEventArgs e)
    {
        ViewModel.MoveSelected(1);
    }

    private void UpdateEmptyState()
    {
        EmptyStatePanel.Visibility = ViewModel.Entries.HasEntries ? Visibility.Collapsed : Visibility.Visible;
    }
}
