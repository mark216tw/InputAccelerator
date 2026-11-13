using System.Runtime.InteropServices;

namespace InputAccelerator.Interop;

internal sealed class FloatingWindowInterop : IDisposable
{
    private const int MinimumWidthDip = 140;
    private const int MinimumHeightDip = 80;
    private const int CollapsedMinimumWidthDip = 40;
    private const int CollapsedMinimumHeightDip = 40;

    private readonly NativeMethods.SubclassProc _subclassProcedure;
    private readonly UIntPtr _subclassId = new(1);
    private IntPtr _windowHandle;
    private bool _isCollapsed;
    private bool _disposed;

    public FloatingWindowInterop()
    {
        _subclassProcedure = WindowProcedure;
    }

    public void SetCollapsed(bool isCollapsed)
    {
        _isCollapsed = isCollapsed;
    }

    public void EnsureTopmost()
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.SetWindowPos(
            _windowHandle,
            NativeMethods.HwndTopmost,
            0,
            0,
            0,
            0,
            NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpNoActivate);
    }

    public void Setup(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            throw new ArgumentException("視窗控制代碼無效。", nameof(windowHandle));
        }

        _windowHandle = windowHandle;

        var extendedStyle = NativeMethods.GetWindowLongPtr(windowHandle, NativeMethods.GwlExStyle).ToInt64();
        extendedStyle |= NativeMethods.WsExTopmost | NativeMethods.WsExToolWindow | NativeMethods.WsExNoActivate;
        extendedStyle &= ~NativeMethods.WsExAppWindow;
        NativeMethods.SetWindowLongPtr(windowHandle, NativeMethods.GwlExStyle, new IntPtr(extendedStyle));

        EnsureTopmost();

        var cornerPreference = NativeMethods.DwmwcpRound;
        NativeMethods.DwmSetWindowAttribute(
            windowHandle,
            NativeMethods.DwmwaWindowCornerPreference,
            ref cornerPreference,
            Marshal.SizeOf<int>());

        NativeMethods.SetWindowSubclass(
            windowHandle,
            _subclassProcedure,
            _subclassId,
            IntPtr.Zero);
    }

    private IntPtr WindowProcedure(
        IntPtr hWnd,
        uint message,
        IntPtr wParam,
        IntPtr lParam,
        UIntPtr uIdSubclass,
        IntPtr dwRefData)
    {
        switch (message)
        {
            case NativeMethods.WmWindowPosChanging:
                KeepTopmost(lParam);
                break;
            case NativeMethods.WmNcCalcSize:
                if (wParam != IntPtr.Zero)
                {
                    return IntPtr.Zero;
                }

                break;
            case NativeMethods.WmMouseActivate:
                return new IntPtr(NativeMethods.MaNoActivate);
            case NativeMethods.WmGetMinMaxInfo:
                ApplyMinimumTrackSize(lParam);
                break;
        }

        return NativeMethods.DefSubclassProc(hWnd, message, wParam, lParam);
    }

    private static void KeepTopmost(IntPtr lParam)
    {
        if (lParam == IntPtr.Zero)
        {
            return;
        }

        var position = Marshal.PtrToStructure<NativeMethods.WindowPos>(lParam);
        if ((position.Flags & NativeMethods.SwpNoZOrder) != 0 ||
            position.HwndInsertAfter == NativeMethods.HwndTopmost)
        {
            return;
        }

        position.HwndInsertAfter = NativeMethods.HwndTopmost;
        Marshal.StructureToPtr(position, lParam, fDeleteOld: false);
    }

    private void ApplyMinimumTrackSize(IntPtr lParam)
    {
        if (lParam == IntPtr.Zero)
        {
            return;
        }

        var dpi = NativeMethods.GetDpiForWindow(_windowHandle);
        var scale = dpi == 0 ? 1.0 : dpi / 96.0;
        var minimumWidthDip = _isCollapsed ? CollapsedMinimumWidthDip : MinimumWidthDip;
        var minimumHeightDip = _isCollapsed ? CollapsedMinimumHeightDip : MinimumHeightDip;
        var minimum = Marshal.PtrToStructure<NativeMethods.MinMaxInfo>(lParam);
        minimum.MinTrackSize.X = Math.Max(minimum.MinTrackSize.X, (int)Math.Round(minimumWidthDip * scale));
        minimum.MinTrackSize.Y = Math.Max(minimum.MinTrackSize.Y, (int)Math.Round(minimumHeightDip * scale));
        Marshal.StructureToPtr(minimum, lParam, fDeleteOld: false);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_windowHandle != IntPtr.Zero)
        {
            NativeMethods.RemoveWindowSubclass(_windowHandle, _subclassProcedure, _subclassId);
            _windowHandle = IntPtr.Zero;
        }
    }
}
