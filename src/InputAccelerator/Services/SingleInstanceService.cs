using InputAccelerator.Interop;

namespace InputAccelerator.Services;

public sealed class SingleInstanceService : IDisposable
{
    private const string MutexName = @"Local\InputAccelerator.SingleInstance";
    private const string ActivationMessageName = "InputAccelerator.Activate";

    private Mutex? _mutex;
    private MessageWindow? _messageWindow;
    private uint _activationMessage;
    private bool _disposed;

    public event EventHandler? ActivationRequested;

    public bool TryAcquire()
    {
        if (_mutex is not null)
        {
            return true;
        }

        var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (!createdNew)
        {
            mutex.Dispose();
            return false;
        }

        _mutex = mutex;
        _activationMessage = NativeMethods.RegisterWindowMessage(ActivationMessageName);
        _messageWindow = new MessageWindow(HandleMessage, "InputAccelerator.SingleInstance");
        return true;
    }

    public static void SignalExistingInstance()
    {
        var message = NativeMethods.RegisterWindowMessage(ActivationMessageName);
        if (message != 0)
        {
            NativeMethods.AllowSetForegroundWindow(-1);
            NativeMethods.PostMessage(NativeMethods.HwndBroadcast, message, IntPtr.Zero, IntPtr.Zero);
        }
    }

    private IntPtr HandleMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message != 0 && message == _activationMessage)
        {
            ActivationRequested?.Invoke(this, EventArgs.Empty);
            return IntPtr.Zero;
        }

        return NativeMethods.DefWindowProc(hWnd, message, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _messageWindow?.Dispose();
        _messageWindow = null;

        if (_mutex is not null)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
            }

            _mutex.Dispose();
            _mutex = null;
        }
    }
}
