using System.ComponentModel;
using System.Runtime.InteropServices;

namespace InputAccelerator.Interop;

internal sealed class MessageWindow : IDisposable
{
    private readonly NativeMethods.WndProc _windowProcedure;
    private readonly Func<IntPtr, uint, IntPtr, IntPtr, IntPtr> _messageHandler;
    private readonly string _className;
    private bool _disposed;

    public MessageWindow(Func<IntPtr, uint, IntPtr, IntPtr, IntPtr> messageHandler, string namePrefix)
    {
        ArgumentNullException.ThrowIfNull(messageHandler);
        _messageHandler = messageHandler;
        _windowProcedure = WindowProcedure;
        _className = $"{namePrefix}.{Guid.NewGuid():N}";

        var instance = NativeMethods.GetModuleHandle(null);
        var windowClass = new NativeMethods.WndClassEx
        {
            CbSize = (uint)Marshal.SizeOf<NativeMethods.WndClassEx>(),
            LpfnWndProc = _windowProcedure,
            HInstance = instance,
            LpszClassName = _className,
        };

        if (NativeMethods.RegisterClassEx(ref windowClass) == 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        Handle = NativeMethods.CreateWindowEx(
            extendedStyle: 0,
            className: _className,
            windowName: string.Empty,
            style: 0,
            x: 0,
            y: 0,
            width: 0,
            height: 0,
            parent: IntPtr.Zero,
            menu: IntPtr.Zero,
            instance: instance,
            parameter: IntPtr.Zero);

        if (Handle == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            NativeMethods.UnregisterClass(_className, instance);
            throw new Win32Exception(error);
        }
    }

    public IntPtr Handle { get; private set; }

    private IntPtr WindowProcedure(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            return _messageHandler(hWnd, message, wParam, lParam);
        }
        catch (Exception)
        {
            return NativeMethods.DefWindowProc(hWnd, message, wParam, lParam);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (Handle != IntPtr.Zero)
        {
            NativeMethods.DestroyWindow(Handle);
            Handle = IntPtr.Zero;
        }

        NativeMethods.UnregisterClass(_className, NativeMethods.GetModuleHandle(null));
    }
}
