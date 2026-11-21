using System.Collections.Specialized;
using InputAccelerator.Core.Models;
using InputAccelerator.Helpers;
using InputAccelerator.Interop;
using InputAccelerator.Services;
using InputAccelerator.ViewModels;
using InputAccelerator.Views;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Storage.Pickers;
using Windows.Graphics;

namespace InputAccelerator;

public sealed partial class MainWindow : Window
{
    public const int DefaultWidthDip = 820;
    public const int DefaultHeightDip = 740;
    public const int MinimumWidthDip = 600;
    public const int MinimumHeightDip = 540;

    private readonly ILogger<MainWindow> _logger;
    private readonly AppearanceService _appearance;
    private readonly Func<SettingsWindow> _settingsWindowFactory;
    private readonly MemoryTrimService _memoryTrim;
    private readonly MinimumSizeInterop _minimumSizeInterop = new(MinimumWidthDip, MinimumHeightDip);
    private SettingsWindow? _settingsWindow;

    public MainWindow(
        MainViewModel viewModel,
        AppearanceService appearance,
        Func<SettingsWindow> settingsWindowFactory,
        MemoryTrimService memoryTrim,
        ILogger<MainWindow> logger)
    {
        InitializeComponent();
        ViewModel = viewModel;
        _appearance = appearance;
        _settingsWindowFactory = settingsWindowFactory;
        _memoryTrim = memoryTrim;
        _logger = logger;
        ViewModel.Entries.Entries.CollectionChanged += OnEntriesCollectionChanged;
        ViewModel.Entries.BatchUpdateCompleted += OnEntriesBatchUpdateCompleted;
        UpdateEmptyState();

        Title = Strings.Get("AppName", "快速輸入");
        if (File.Exists(AppPaths.IconFile))
        {
            AppWindow.SetIcon(AppPaths.IconFile);
        }

        RootGrid.RequestedTheme = _appearance.CurrentElementTheme;
        _minimumSizeInterop.Setup(WinRT.Interop.WindowNative.GetWindowHandle(this));
        _appearance.Changed += OnAppearanceChanged;
        Closed += OnClosed;
    }

    public MainViewModel ViewModel { get; }

    private void OnAppearanceChanged(object? sender, AppearanceChangedEventArgs e)
    {
        RootGrid.RequestedTheme = e.ElementTheme;
    }

    private void OnEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!ViewModel.Entries.IsBatchUpdating)
        {
            UpdateEmptyState();
        }
    }

    private void OnEntriesBatchUpdateCompleted(object? sender, EventArgs e)
    {
        UpdateEmptyState();
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        CloseSettingsIfOpen();
        _appearance.Changed -= OnAppearanceChanged;
        ViewModel.Entries.Entries.CollectionChanged -= OnEntriesCollectionChanged;
        ViewModel.Entries.BatchUpdateCompleted -= OnEntriesBatchUpdateCompleted;
        Closed -= OnClosed;
        _minimumSizeInterop.Dispose();
        Bindings.StopTracking();
    }

    public void ApplyPlacement(WindowPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(placement);
        AppWindow.MoveAndResize(new RectInt32(placement.X, placement.Y, placement.Width, placement.Height));
    }

    public void ShowManager()
    {
        var presenter = AppWindow.Presenter as OverlappedPresenter;
        var wasMaximized = presenter?.State == OverlappedPresenterState.Maximized;

        AppWindow.Show();

        var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        NativeMethods.ShowWindow(windowHandle, wasMaximized ? NativeMethods.SwShow : NativeMethods.SwRestore);
        NativeMethods.SetWindowPos(
            windowHandle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpShowWindow);
        if (wasMaximized)
        {
            presenter?.Maximize();
        }

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
        CloseSettingsIfOpen();
        AppWindow.Hide();
    }

    public void CloseSettingsIfOpen()
    {
        if (_settingsWindow is { } settingsWindow)
        {
            _settingsWindow = null;
            settingsWindow.ForceClose();
        }
    }

    private void OnMoreClick(object sender, RoutedEventArgs e)
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        var settingsWindow = _settingsWindowFactory();
        settingsWindow.Closed += OnSettingsWindowClosed;
        _settingsWindow = settingsWindow;
        settingsWindow.ShowSettings(this);
    }

    private void OnSettingsWindowClosed(object sender, WindowEventArgs args)
    {
        if (sender is SettingsWindow settingsWindow)
        {
            settingsWindow.Closed -= OnSettingsWindowClosed;
            if (ReferenceEquals(_settingsWindow, settingsWindow))
            {
                _settingsWindow = null;
            }
        }

        _memoryTrim.ScheduleTrim();
    }

    private async void OnAddClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new EditEntryDialog(
                Strings.Get("EditEntryDialog.AddTitle", "新增項目"),
                EntryKind.Text,
                string.Empty,
                hotkey: null,
                label: null)
            {
                XamlRoot = RootGrid.XamlRoot,
                RequestedTheme = RootGrid.ActualTheme,
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var entry = ViewModel.AddEntry(
                    dialog.SelectedKind,
                    dialog.EntryText,
                    dialog.EntryHotkey,
                    dialog.EntryLabel);
                EntryList.ScrollIntoView(entry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "新增項目失敗。");
        }
    }

    private async void OnImportClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker(AppWindow.Id)
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                CommitButtonText = Strings.Get("ImportPickerCommitButtonText", "匯入"),
            };
            picker.FileTypeFilter.Add(".json");

            var file = await picker.PickSingleFileAsync();
            if (file is null || string.IsNullOrWhiteSpace(file.Path))
            {
                return;
            }

            var importResult = await ViewModel.Entries.ReadImportFileAsync(file.Path);
            if (importResult.Entries.Count == 0)
            {
                await ShowMessageAsync(
                    Strings.Get("ImportEmpty.Title", "沒有可匯入的項目"),
                    Strings.Get("ImportEmpty.Body", "檔案中沒有有效的文字或按鍵組合項目。"));
                return;
            }

            var bodyFormat = Strings.Get(
                "ImportDialog.Body",
                "已讀取 {0} 筆有效項目，{1} 筆無效項目會略過。\n\n要附加到現有清單，還是先清空目前清單再匯入？");
            var body = string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                bodyFormat,
                importResult.Entries.Count,
                importResult.SkippedCount);
            var dialog = new ContentDialog
            {
                XamlRoot = RootGrid.XamlRoot,
                RequestedTheme = RootGrid.ActualTheme,
                Title = Strings.Get("ImportDialog.Title", "匯入項目清單"),
                Content = new TextBlock { Text = body, TextWrapping = TextWrapping.Wrap },
                PrimaryButtonText = Strings.Get("ImportDialog.AppendButtonText", "附加到現有清單"),
                SecondaryButtonText = Strings.Get("ImportDialog.ReplaceButtonText", "取代目前清單"),
                CloseButtonText = Strings.Get("ImportDialog.CancelButtonText", "取消"),
                DefaultButton = ContentDialogButton.Close,
            };

            var choice = await dialog.ShowAsync();
            if (choice == ContentDialogResult.None)
            {
                return;
            }

            var mode = choice == ContentDialogResult.Primary
                ? EntryImportMode.Append
                : EntryImportMode.Replace;
            var firstImportedIndex = mode == EntryImportMode.Replace
                ? 0
                : ViewModel.Entries.Entries.Count;
            var importedCount = await ViewModel.Entries.ImportAsync(importResult, mode);

            if (ViewModel.Entries.Entries.Count > firstImportedIndex)
            {
                var firstImported = ViewModel.Entries.Entries[firstImportedIndex];
                ViewModel.SelectedEntry = firstImported;
                EntryList.ScrollIntoView(firstImported);
            }
            else
            {
                ViewModel.SelectedEntry = null;
            }

            var resultBody = importResult.SkippedCount > 0
                ? Strings.Format(
                    "ImportResult.BodyWithSkipped",
                    importedCount,
                    importResult.SkippedCount)
                : Strings.Format("ImportResult.Body", importedCount);
            await ShowMessageAsync(
                Strings.Get("ImportResult.Title", "匯入完成"),
                resultBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "匯入項目清單失敗。");
            await ShowMessageAsync(
                Strings.Get("ImportFailed.Title", "無法匯入項目"),
                Strings.Get("ImportFailed.Body", "檔案格式不正確或無法讀取，請確認後再試一次。"));
        }
    }

    private async void OnExportClick(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.Entries.HasEntries)
        {
            return;
        }

        try
        {
            var picker = new FileSavePicker(AppWindow.Id)
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = "input-accelerator-entries",
                DefaultFileExtension = ".json",
                CommitButtonText = Strings.Get("ExportPickerCommitButtonText", "匯出"),
            };
            picker.FileTypeChoices.Add(
                Strings.Get("JsonFileTypeDisplayName", "JSON 檔"),
                new List<string> { ".json" });

            var file = await picker.PickSaveFileAsync();
            if (file is null || string.IsNullOrWhiteSpace(file.Path))
            {
                return;
            }

            var count = ViewModel.Entries.Entries.Count;
            await ViewModel.Entries.ExportAsync(file.Path);
            await ShowMessageAsync(
                Strings.Get("ExportResult.Title", "匯出完成"),
                Strings.Format("ExportResult.Body", count, file.Path));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "匯出項目清單失敗。");
            await ShowMessageAsync(
                Strings.Get("ExportFailed.Title", "無法匯出項目"),
                Strings.Get("ExportFailed.Body", "無法寫入檔案，請確認儲存位置後再試一次。"));
        }
    }

    private async Task ShowMessageAsync(string title, string body)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = RootGrid.XamlRoot,
            RequestedTheme = RootGrid.ActualTheme,
            Title = title,
            Content = new TextBlock { Text = body, TextWrapping = TextWrapping.Wrap },
            CloseButtonText = Strings.Get("TransferDialog.CloseButtonText", "關閉"),
        };

        await dialog.ShowAsync();
    }

    private async void OnEditClick(object sender, RoutedEventArgs e)
    {
        await EditSelectedEntryAsync();
    }

    private async void OnEntryListDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source || !IsInsideListItem(source))
        {
            return;
        }

        await EditSelectedEntryAsync();
    }

    private async Task EditSelectedEntryAsync()
    {
        if (ViewModel.SelectedEntry is not { } selected)
        {
            return;
        }

        try
        {
            var dialog = new EditEntryDialog(
                Strings.Get("EditEntryDialog.EditTitle", "編輯項目"),
                selected.Kind,
                selected.Content,
                selected.Hotkey,
                selected.Label)
            {
                XamlRoot = RootGrid.XamlRoot,
                RequestedTheme = RootGrid.ActualTheme,
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                ViewModel.UpdateEntry(
                    selected,
                    dialog.SelectedKind,
                    dialog.EntryText,
                    dialog.EntryHotkey,
                    dialog.EntryLabel);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "編輯項目失敗。");
        }
    }

    private static bool IsInsideListItem(DependencyObject source)
    {
        for (DependencyObject? current = source; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is ListViewItem)
            {
                return true;
            }
        }

        return false;
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
                Title = Strings.Get("DeleteDialog.Title", "刪除項目"),
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
