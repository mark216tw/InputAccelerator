using System.Runtime.InteropServices;
using InputAccelerator.Helpers;
using InputAccelerator.Interop;
using Microsoft.Extensions.Logging;

namespace InputAccelerator.Services;

public sealed class TrayIconService : IDisposable
{
    private const uint CallbackMessage = NativeMethods.WmApp + 11;
    private const uint NinSelect = 0x0400;
    private const uint NinKeySelect = 0x0401;
    private const int MenuShowManager = 1;
    private const int MenuToggleOverlay = 2;
    private const int MenuExit = 3;
    private const int DefaultApplicationIcon = 32512;

    private readonly ILogger<TrayIconService> _logger;
    private MessageWindow? _messageWindow;
    private IntPtr _iconHandle;
    private bool _iconAdded;
    private bool _disposed;
    private bool _isOverlayEnabled;
    private uint _taskbarCreatedMessage;

    public TrayIconService(ILogger<TrayIconService> logger)
    {
        _logger = logger;
    }

    public event EventHandler? ShowManagerRequested;

    public event EventHandler? ToggleOverlayRequested;

    public event EventHandler? ExitRequested;

    public void Initialize()
    {
        if (_messageWindow is not null)
        {
            return;
        }

        _messageWindow = new MessageWindow(HandleMessage, "InputAccelerator.Tray");
        _taskbarCreatedMessage = NativeMethods.RegisterWindowMessage("TaskbarCreated");
        _iconHandle = LoadTrayIcon();
        AddIcon();
    }

    public void SetOverlayState(bool isEnabled)
    {
        _isOverlayEnabled = isEnabled;
    }

    public void ShowBalloon(string title, string message, bool warning = true)
    {
        if (_messageWindow is null)
        {
            return;
        }

        var data = CreateIconData();
        data.Flags = NativeMethods.NifInfo;
        data.InfoTitle = Truncate(title, 63);
        data.Info = Truncate(message, 255);
        data.InfoFlags = warning ? NativeMethods.NiifWarning : NativeMethods.NiifInfo;
        if (!NativeMethods.Shell_NotifyIcon(NativeMethods.NimModify, ref data))
        {
            _logger.LogWarning("無法顯示系統匣通知。");
        }
    }

    private IntPtr HandleMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == _taskbarCreatedMessage && _taskbarCreatedMessage != 0)
        {
            _iconAdded = false;
            AddIcon();
            return IntPtr.Zero;
        }

        if (message != CallbackMessage)
        {
            return NativeMethods.DefWindowProc(hWnd, message, wParam, lParam);
        }

        var notification = (uint)(lParam.ToInt64() & 0xFFFF);
        switch (notification)
        {
            case NativeMethods.WmContextMenu:
            case NativeMethods.WmRButtonUp:
                ShowContextMenu();
                break;
            case NinSelect:
            case NinKeySelect:
            case NativeMethods.WmLButtonDblClk:
                ShowManagerRequested?.Invoke(this, EventArgs.Empty);
                break;
        }

        return IntPtr.Zero;
    }

    private void ShowContextMenu()
    {
        var menu = NativeMethods.CreatePopupMenu();
        if (menu == IntPtr.Zero)
        {
            return;
        }

        try
        {
            NativeMethods.AppendMenu(
                menu,
                NativeMethods.MfString,
                new UIntPtr(MenuShowManager),
                Strings.Get("TrayShowManager", "顯示主視窗"));

            NativeMethods.AppendMenu(
                menu,
                NativeMethods.MfString | (_isOverlayEnabled ? NativeMethods.MfChecked : 0),
                new UIntPtr(MenuToggleOverlay),
                Strings.Get("TrayToggleOverlay", "啟用浮動視窗"));

            NativeMethods.AppendMenu(menu, NativeMethods.MfSeparator, UIntPtr.Zero, null);

            NativeMethods.AppendMenu(
                menu,
                NativeMethods.MfString,
                new UIntPtr(MenuExit),
                Strings.Get("TrayExit", "結束"));

            if (!NativeMethods.GetCursorPos(out var cursor))
            {
                cursor = new NativeMethods.Point();
            }

            if (_messageWindow is not null)
            {
                NativeMethods.SetForegroundWindow(_messageWindow.Handle);
            }

            var command = NativeMethods.TrackPopupMenu(
                menu,
                NativeMethods.TpmReturnCmd | NativeMethods.TpmRightButton | NativeMethods.TpmNoNotify,
                cursor.X,
                cursor.Y,
                0,
                _messageWindow?.Handle ?? IntPtr.Zero,
                IntPtr.Zero);

            if (_messageWindow is not null)
            {
                NativeMethods.PostMessage(_messageWindow.Handle, NativeMethods.WmNull, IntPtr.Zero, IntPtr.Zero);
            }

            switch (command)
            {
                case MenuShowManager:
                    ShowManagerRequested?.Invoke(this, EventArgs.Empty);
                    break;
                case MenuToggleOverlay:
                    ToggleOverlayRequested?.Invoke(this, EventArgs.Empty);
                    break;
                case MenuExit:
                    ExitRequested?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
        finally
        {
            NativeMethods.DestroyMenu(menu);
        }
    }

    private IntPtr LoadTrayIcon()
    {
        var smallWidth = NativeMethods.GetSystemMetrics(NativeMethods.SmCxSmIcon);
        var smallHeight = NativeMethods.GetSystemMetrics(NativeMethods.SmCySmIcon);
        var icon = NativeMethods.LoadImage(
            IntPtr.Zero,
            AppPaths.IconFile,
            NativeMethods.ImageIcon,
            smallWidth,
            smallHeight,
            NativeMethods.LrLoadFromFile | NativeMethods.LrDefaultSize);

        if (icon == IntPtr.Zero)
        {
            icon = NativeMethods.LoadIcon(IntPtr.Zero, new IntPtr(DefaultApplicationIcon));
        }

        return icon;
    }

    private void AddIcon()
    {
        if (_messageWindow is null || _iconAdded)
        {
            return;
        }

        var data = CreateIconData();
        data.Flags = NativeMethods.NifMessage | NativeMethods.NifIcon | NativeMethods.NifTip;
        if (!NativeMethods.Shell_NotifyIcon(NativeMethods.NimAdd, ref data))
        {
            _logger.LogWarning("無法加入系統匣圖示。");
            return;
        }

        _iconAdded = true;
        data.TimeoutOrVersion = NativeMethods.NotifyIconVersion4;
        NativeMethods.Shell_NotifyIcon(NativeMethods.NimSetVersion, ref data);
    }

    private NativeMethods.NotifyIconData CreateIconData()
    {
        return new NativeMethods.NotifyIconData
        {
            Size = (uint)Marshal.SizeOf<NativeMethods.NotifyIconData>(),
            WindowHandle = _messageWindow?.Handle ?? IntPtr.Zero,
            Id = 1,
            CallbackMessage = CallbackMessage,
            IconHandle = _iconHandle,
            Tip = Truncate(Strings.Get("TrayTooltip", "快速輸入"), 127),
            Info = string.Empty,
            InfoTitle = string.Empty,
        };
    }

    private static string Truncate(string value, int maximumLength)
    {
        return value.Length <= maximumLength ? value : value[..maximumLength];
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_messageWindow is not null)
        {
            if (_iconAdded)
            {
                var data = CreateIconData();
                NativeMethods.Shell_NotifyIcon(NativeMethods.NimDelete, ref data);
                _iconAdded = false;
            }

            _messageWindow.Dispose();
            _messageWindow = null;
        }

        if (_iconHandle != IntPtr.Zero)
        {
            NativeMethods.DestroyIcon(_iconHandle);
            _iconHandle = IntPtr.Zero;
        }
    }
}
