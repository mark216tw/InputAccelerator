using InputAccelerator.Interop;

namespace InputAccelerator.Services;

public sealed class ForegroundWindowTracker : IDisposable
{
    private readonly NativeMethods.WinEventDelegate _callback;
    private readonly IntPtr _hook;
    private IntPtr _lastExternalWindow;
    private bool _disposed;

    public ForegroundWindowTracker()
    {
        _callback = OnForegroundChanged;
        _hook = NativeMethods.SetWinEventHook(
            NativeMethods.EventSystemForeground,
            NativeMethods.EventSystemForeground,
            IntPtr.Zero,
            _callback,
            processId: 0,
            threadId: 0,
            NativeMethods.WineventOutOfContext | NativeMethods.WineventSkipOwnProcess);

        var current = NativeMethods.GetForegroundWindow();
        if (IsExternalWindow(current))
        {
            _lastExternalWindow = current;
        }
    }

    public IntPtr LastExternalWindow => Volatile.Read(ref _lastExternalWindow);

    public IntPtr ResolveInjectionTarget()
    {
        var current = NativeMethods.GetForegroundWindow();
        if (IsExternalWindow(current))
        {
            return current;
        }

        var last = LastExternalWindow;
        return IsExternalWindow(last) ? last : IntPtr.Zero;
    }

    public static bool IsExternalWindow(IntPtr window)
    {
        if (window == IntPtr.Zero || !NativeMethods.IsWindow(window))
        {
            return false;
        }

        NativeMethods.GetWindowThreadProcessId(window, out var processId);
        return processId != 0 && processId != NativeMethods.GetCurrentProcessId();
    }

    private void OnForegroundChanged(
        IntPtr eventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint eventThread,
        uint eventTime)
    {
        if (eventType == NativeMethods.EventSystemForeground && IsExternalWindow(hwnd))
        {
            Volatile.Write(ref _lastExternalWindow, hwnd);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_hook != IntPtr.Zero)
        {
            NativeMethods.UnhookWinEvent(_hook);
        }
    }
}
