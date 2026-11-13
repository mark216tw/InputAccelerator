using InputAccelerator.Core.Models;
using InputAccelerator.Helpers;
using InputAccelerator.Interop;
using InputAccelerator.Services;
using InputAccelerator.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Graphics;

namespace InputAccelerator.Views;

public sealed partial class FloatingWindow : Window
{
    private const int CollapsedSizeDip = 44;

    private static readonly TimeSpan TopmostReassertInterval = TimeSpan.FromSeconds(2);

    private readonly ILogger<FloatingWindow> _logger;
    private readonly FloatingWindowInterop _interop;
    private readonly AppearanceService _appearance;
    private readonly DispatcherQueueTimer _topmostTimer;
    private readonly IntPtr _windowHandle;
    private bool _isSending;
    private bool _isDragging;
    private bool _isResizing;
    private WindowPlacement? _expandedPlacement;
    private NativeMethods.Point _dragStartCursor;
    private PointInt32 _dragStartWindowPosition;
    private NativeMethods.Point _resizeStartCursor;
    private PointInt32 _resizeStartWindowPosition;
    private SizeInt32 _resizeStartWindowSize;

    public FloatingWindow(
        FloatingBarViewModel viewModel,
        AppearanceService appearance,
        ILogger<FloatingWindow> logger)
    {
        InitializeComponent();
        ViewModel = viewModel;
        _appearance = appearance;
        _logger = logger;
        ChipHost.ItemsSource = ViewModel.Items;
        ViewModel.Items.CollectionChanged += (_, _) => UpdateEmptyState();
        UpdateEmptyState();
        _interop = new FloatingWindowInterop();

        Title = Strings.Get("AppName", "快速輸入");
        if (File.Exists(AppPaths.IconFile))
        {
            AppWindow.SetIcon(AppPaths.IconFile);
        }

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(hasBorder: false, hasTitleBar: false);
            presenter.IsResizable = true;
            presenter.IsAlwaysOnTop = true;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }

        AppWindow.IsShownInSwitchers = false;

        _windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        _interop.Setup(_windowHandle);

        var dispatcher = DispatcherQueue.GetForCurrentThread();
        _topmostTimer = dispatcher.CreateTimer();
        _topmostTimer.Interval = TopmostReassertInterval;
        _topmostTimer.IsRepeating = true;
        _topmostTimer.Tick += OnTopmostTimerTick;
        RootGrid.AddHandler(
            UIElement.PointerPressedEvent,
            new PointerEventHandler(OnRootPointerPressed),
            handledEventsToo: true);

        var dragHandleText = Strings.Get("DragHandleText", "拖曳移動視窗");
        ToolTipService.SetToolTip(DragHandle, dragHandleText);
        AutomationProperties.SetName(DragHandle, dragHandleText);

        var resizeGripText = Strings.Get("ResizeGripText", "拖曳調整大小");
        ToolTipService.SetToolTip(ResizeGrip, resizeGripText);
        AutomationProperties.SetName(ResizeGrip, resizeGripText);

        var collapseText = Strings.Get("FloatingCollapseButton", "縮小浮動視窗");
        ToolTipService.SetToolTip(CollapseButton, collapseText);
        AutomationProperties.SetName(CollapseButton, collapseText);

        var restoreText = Strings.Get("FloatingRestoreButton", "還原浮動視窗");
        ToolTipService.SetToolTip(CollapsedBlock, restoreText);
        AutomationProperties.SetName(CollapsedBlock, restoreText);

        _appearance.Changed += OnAppearanceChanged;
        Closed += OnClosed;
    }

    public event EventHandler<bool>? CollapseStateChanged;

    public FloatingBarViewModel ViewModel { get; }

    public IntPtr WindowHandle => _windowHandle;

    public bool IsCollapsed { get; private set; }

    public void ApplyPlacement(WindowPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(placement);
        AppWindow.MoveAndResize(new RectInt32(placement.X, placement.Y, placement.Width, placement.Height));
    }

    public void ShowWithoutActivation()
    {
        AppWindow.Show(activateWindow: false);
        _interop.EnsureTopmost();
        if (!_topmostTimer.IsRunning)
        {
            _topmostTimer.Start();
        }
    }

    public void HideBar()
    {
        _topmostTimer.Stop();
        AppWindow.Hide();
    }

    private void OnAppearanceChanged(object? sender, AppearanceChangedEventArgs e)
    {
        ApplyAppearance(e);
    }

    private void ApplyAppearance(AppearanceChangedEventArgs appearance)
    {
        ArgumentNullException.ThrowIfNull(appearance);
        var palette = appearance.Palette;

        RootGrid.RequestedTheme = appearance.ElementTheme;
        RootGrid.Background = palette.WindowBackground;
        WindowBorder.Background = palette.WindowBackground;
        WindowBorder.BorderBrush = palette.WindowBorder;
        DragIndicator.Background = palette.MutedForeground;
        ResizeGripPath.Stroke = palette.MutedForeground;
        EmptyStateText.Foreground = palette.Foreground;
        CollapseIcon.Foreground = palette.MutedForeground;
        CollapsedIcon.Foreground = palette.Foreground;
        CollapsedBlock.Background = palette.ChipBackground;
        CollapsedBlock.BorderBrush = palette.ChipBorder;
        ViewModel.ApplyPalette(palette);
    }

    private async void OnEntryClick(object sender, RoutedEventArgs e)
    {
        if (_isSending ||
            sender is not FrameworkElement { DataContext: FloatingEntryItemViewModel item })
        {
            return;
        }

        _isSending = true;
        try
        {
            await ViewModel.SendAsync(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "送出文字失敗。");
        }
        finally
        {
            _isSending = false;
        }
    }

    private void OnCollapseClick(object sender, RoutedEventArgs e)
    {
        if (IsCollapsed)
        {
            return;
        }

        var position = AppWindow.Position;
        var size = AppWindow.Size;
        _expandedPlacement = new WindowPlacement
        {
            X = position.X,
            Y = position.Y,
            Width = size.Width,
            Height = size.Height,
        };

        IsCollapsed = true;
        _interop.SetCollapsed(true);
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
        }

        CollapseStateChanged?.Invoke(this, true);

        var dpi = NativeMethods.GetDpiForWindow(_windowHandle);
        var scale = dpi == 0 ? 1.0 : dpi / 96.0;
        var collapsedSize = Math.Max(1, (int)Math.Round(CollapsedSizeDip * scale));
        var x = _expandedPlacement.X + _expandedPlacement.Width - collapsedSize;
        var y = _expandedPlacement.Y;

        AppWindow.MoveAndResize(new RectInt32(x, y, collapsedSize, collapsedSize));
        _interop.EnsureTopmost();
        UpdateCollapsedVisualState();
    }

    private void OnRestoreClick(object sender, RoutedEventArgs e)
    {
        if (!IsCollapsed)
        {
            return;
        }

        IsCollapsed = false;
        _interop.SetCollapsed(false);
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = true;
        }

        UpdateCollapsedVisualState();

        if (_expandedPlacement is { } placement)
        {
            AppWindow.MoveAndResize(new RectInt32(placement.X, placement.Y, placement.Width, placement.Height));
        }

        _interop.EnsureTopmost();
        CollapseStateChanged?.Invoke(this, false);
    }

    private void UpdateCollapsedVisualState()
    {
        ExpandedView.Visibility = IsCollapsed ? Visibility.Collapsed : Visibility.Visible;
        CollapsedBlock.Visibility = IsCollapsed ? Visibility.Visible : Visibility.Collapsed;
        ResizeGrip.Visibility = IsCollapsed ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnDragHandlePointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!NativeMethods.GetCursorPos(out _dragStartCursor))
        {
            return;
        }

        _dragStartWindowPosition = AppWindow.Position;
        _isDragging = DragHandle.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnDragHandlePointerMoved(object sender, PointerRoutedEventArgs e)
    {
        UpdateDragPosition();
        e.Handled = true;
    }

    private void OnDragHandlePointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        UpdateDragPosition();
        _isDragging = false;
        DragHandle.ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }

    private void UpdateDragPosition()
    {
        if (!_isDragging || !NativeMethods.GetCursorPos(out var cursor))
        {
            return;
        }

        var x = _dragStartWindowPosition.X + cursor.X - _dragStartCursor.X;
        var y = _dragStartWindowPosition.Y + cursor.Y - _dragStartCursor.Y;
        NativeMethods.SetWindowPos(
            _windowHandle,
            NativeMethods.HwndTopmost,
            x,
            y,
            0,
            0,
            NativeMethods.SwpNoSize | NativeMethods.SwpNoActivate);
    }

    private void OnDragHandlePointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        _isDragging = false;
    }

    private void OnResizeGripPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!NativeMethods.GetCursorPos(out _resizeStartCursor))
        {
            return;
        }

        _resizeStartWindowPosition = AppWindow.Position;
        _resizeStartWindowSize = AppWindow.Size;
        _isResizing = ResizeGrip.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnResizeGripPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        UpdateResize();
        e.Handled = true;
    }

    private void OnResizeGripPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isResizing)
        {
            return;
        }

        UpdateResize();
        _isResizing = false;
        ResizeGrip.ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }

    private void OnResizeGripPointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        _isResizing = false;
    }

    private void UpdateResize()
    {
        if (!_isResizing || !NativeMethods.GetCursorPos(out var cursor))
        {
            return;
        }

        var dpi = NativeMethods.GetDpiForWindow(_windowHandle);
        var scale = dpi == 0 ? 1.0 : dpi / 96.0;
        var minimumWidth = Math.Max(1, (int)Math.Round(140 * scale));
        var minimumHeight = Math.Max(1, (int)Math.Round(80 * scale));
        var width = Math.Max(
            minimumWidth,
            _resizeStartWindowSize.Width + cursor.X - _resizeStartCursor.X);
        var height = Math.Max(
            minimumHeight,
            _resizeStartWindowSize.Height + cursor.Y - _resizeStartCursor.Y);

        NativeMethods.SetWindowPos(
            _windowHandle,
            NativeMethods.HwndTopmost,
            _resizeStartWindowPosition.X,
            _resizeStartWindowPosition.Y,
            width,
            height,
            NativeMethods.SwpNoActivate);
    }

    private void OnTopmostTimerTick(DispatcherQueueTimer sender, object args)
    {
        _interop.EnsureTopmost();
    }

    private void OnRootPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _interop.EnsureTopmost();
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        _appearance.Changed -= OnAppearanceChanged;
        _topmostTimer.Stop();
        _topmostTimer.Tick -= OnTopmostTimerTick;
        RootGrid.RemoveHandler(UIElement.PointerPressedEvent, new PointerEventHandler(OnRootPointerPressed));
        _interop.Dispose();
    }

    private void UpdateEmptyState()
    {
        EmptyStateText.Visibility = ViewModel.HasEntries ? Visibility.Collapsed : Visibility.Visible;
    }
}
