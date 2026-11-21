using System.Runtime.InteropServices;
using InputAccelerator.Core.Models;

namespace InputAccelerator.Interop;

internal static class DisplayAreaEnumerator
{
    public static IReadOnlyList<DisplayAreaInfo> Enumerate()
    {
        var monitors = new List<(DisplayAreaInfo Area, bool IsPrimary)>();

        bool EnumerateMonitor(IntPtr monitor, IntPtr deviceContext, ref NativeMethods.Rect monitorRect, IntPtr data)
        {
            var info = new NativeMethods.MonitorInfo
            {
                Size = (uint)Marshal.SizeOf<NativeMethods.MonitorInfo>(),
            };

            if (NativeMethods.GetMonitorInfo(monitor, ref info))
            {
                monitors.Add((
                    new DisplayAreaInfo(
                        new PixelRect(
                            info.Monitor.Left,
                            info.Monitor.Top,
                            info.Monitor.Width,
                            info.Monitor.Height),
                        new PixelRect(
                            info.Work.Left,
                            info.Work.Top,
                            info.Work.Width,
                            info.Work.Height)),
                    (info.Flags & NativeMethods.MonitorInfoPrimary) != 0));
            }

            return true;
        }

        NativeMethods.MonitorEnumProc callback = EnumerateMonitor;
        NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
        GC.KeepAlive(callback);

        var areas = new List<DisplayAreaInfo>(monitors.Count);
        foreach (var monitor in monitors)
        {
            if (monitor.IsPrimary)
            {
                areas.Add(monitor.Area);
            }
        }

        foreach (var monitor in monitors)
        {
            if (!monitor.IsPrimary)
            {
                areas.Add(monitor.Area);
            }
        }

        if (areas.Count == 0)
        {
            var width = Math.Max(1, NativeMethods.GetSystemMetrics(NativeMethods.SmCxScreen));
            var height = Math.Max(1, NativeMethods.GetSystemMetrics(NativeMethods.SmCyScreen));
            areas.Add(new DisplayAreaInfo(
                new PixelRect(0, 0, width, height),
                new PixelRect(0, 0, width, height)));
        }

        return areas;
    }
}
