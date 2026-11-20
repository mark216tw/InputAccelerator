using System.Runtime;
using InputAccelerator.Interop;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

namespace InputAccelerator.Services;

public sealed class MemoryTrimService : IDisposable
{
    private static readonly TimeSpan TrimDelay = TimeSpan.FromMilliseconds(1500);

    private readonly DispatcherQueue _dispatcher;
    private readonly DispatcherQueueTimer _timer;
    private readonly ILogger<MemoryTrimService> _logger;
    private int _isTrimming;
    private bool _disposed;

    public MemoryTrimService(ILogger<MemoryTrimService> logger)
    {
        _logger = logger;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        _timer = _dispatcher.CreateTimer();
        _timer.Interval = TrimDelay;
        _timer.IsRepeating = false;
        _timer.Tick += OnTimerTick;
    }

    public void ScheduleTrim()
    {
        if (_disposed)
        {
            return;
        }

        if (_dispatcher.HasThreadAccess)
        {
            RestartTimer();
        }
        else
        {
            _dispatcher.TryEnqueue(RestartTimer);
        }
    }

    public void CancelPendingTrim()
    {
        if (_disposed)
        {
            return;
        }

        if (_dispatcher.HasThreadAccess)
        {
            _timer.Stop();
        }
        else
        {
            _dispatcher.TryEnqueue(() =>
            {
                if (!_disposed)
                {
                    _timer.Stop();
                }
            });
        }
    }

    private void RestartTimer()
    {
        if (_disposed)
        {
            return;
        }

        _timer.Stop();
        _timer.Start();
    }

    private void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        if (_disposed || Interlocked.CompareExchange(ref _isTrimming, 1, 0) != 0)
        {
            return;
        }

        _ = Task.Run(TrimCore);
    }

    private void TrimCore()
    {
        try
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);

            if (!NativeMethods.SetProcessWorkingSetSizeEx(
                NativeMethods.GetCurrentProcess(),
                new IntPtr(-1),
                new IntPtr(-1),
                flags: 0))
            {
                _logger.LogDebug("無法縮減程序工作集。");
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "背景記憶體整理失敗。");
        }
        finally
        {
            Volatile.Write(ref _isTrimming, 0);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }
}
