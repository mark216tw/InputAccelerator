using System.Runtime.InteropServices;

namespace InputAccelerator.Interop;

internal sealed class MinimumSizeInterop : IDisposable
{
    private readonly NativeMethods.SubclassProc _subclassProcedure;
    private readonly UIntPtr _subclassId = new(2);
    private readonly int _minimumWidthDip;
    private readonly int _minimumHeightDip;
    private IntPtr _windowHandle;
    private bool _disposed;

    public MinimumSizeInterop(int minimumWidthDip, int minimumHeightDip)
    {
        _minimumWidthDip = minimumWidthDip;
        _minimumHeightDip = minimumHeightDip;
        _subclassProcedure = WindowProcedure;
    }

    public void Setup(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            throw new ArgumentException("視窗控制代碼無效。", nameof(windowHandle));
        }

        _windowHandle = windowHandle;
        NativeMethods.SetWindowSubclass(windowHandle, _subclassProcedure, _subclassId, IntPtr.Zero);
    }

    private IntPtr WindowProcedure(
        IntPtr hWnd,
        uint message,
        IntPtr wParam,
        IntPtr lParam,
        UIntPtr uIdSubclass,
        IntPtr dwRefData)
    {
        if (message == NativeMethods.WmGetMinMaxInfo && lParam != IntPtr.Zero)
        {
            var dpi = NativeMethods.GetDpiForWindow(_windowHandle);
            var scale = dpi == 0 ? 1.0 : dpi / 96.0;
            var info = Marshal.PtrToStructure<NativeMethods.MinMaxInfo>(lParam);
            info.MinTrackSize.X = Math.Max(info.MinTrackSize.X, (int)Math.Round(_minimumWidthDip * scale));
            info.MinTrackSize.Y = Math.Max(info.MinTrackSize.Y, (int)Math.Round(_minimumHeightDip * scale));
            Marshal.StructureToPtr(info, lParam, fDeleteOld: false);
        }

        return NativeMethods.DefSubclassProc(hWnd, message, wParam, lParam);
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
