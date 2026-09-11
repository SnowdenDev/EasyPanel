namespace EasyPanel.Daemon.Infrastructure;

public sealed record SystemStatsSnapshot(
    string HostName,
    int LogicalProcessorCount,
    long TotalPhysicalMemoryMegabytes,
    long AvailableMemoryMegabytes,
    double CpuUsagePercent);

/// <summary>
/// Machine-wide stats reported on every heartbeat, shown on the node detail page. CPU
/// usage is delta-based against the previous GetSystemTimes sample — the very first call
/// after startup has nothing to diff against and reports 0%, which self-corrects on the
/// next heartbeat tick.
/// </summary>
public sealed class SystemStatsCollector
{
    private ulong _previousIdle;
    private ulong _previousKernel;
    private ulong _previousUser;
    private bool _hasPreviousSample;

    public SystemStatsSnapshot GetSnapshot()
    {
        var memory = GetMemory();
        var cpuUsagePercent = GetCpuUsagePercent();

        return new SystemStatsSnapshot(
            Environment.MachineName,
            Environment.ProcessorCount,
            memory.TotalMegabytes,
            memory.AvailableMegabytes,
            cpuUsagePercent);
    }

    private static (long TotalMegabytes, long AvailableMegabytes) GetMemory()
    {
        var status = new SystemStatsNativeMethods.MEMORYSTATUSEX
        {
            dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf<SystemStatsNativeMethods.MEMORYSTATUSEX>(),
        };

        if (!SystemStatsNativeMethods.GlobalMemoryStatusEx(ref status))
        {
            return (0, 0);
        }

        const ulong bytesPerMegabyte = 1024 * 1024;
        return ((long)(status.ullTotalPhys / bytesPerMegabyte), (long)(status.ullAvailPhys / bytesPerMegabyte));
    }

    private double GetCpuUsagePercent()
    {
        if (!SystemStatsNativeMethods.GetSystemTimes(out var idle, out var kernel, out var user))
        {
            return 0;
        }

        var currentIdle = idle.ToTicks();
        var currentKernel = kernel.ToTicks();
        var currentUser = user.ToTicks();

        if (!_hasPreviousSample)
        {
            _previousIdle = currentIdle;
            _previousKernel = currentKernel;
            _previousUser = currentUser;
            _hasPreviousSample = true;
            return 0;
        }

        var idleDelta = currentIdle - _previousIdle;
        // lpKernelTime from GetSystemTimes includes idle time — total busy+idle time is
        // kernelDelta + userDelta, not kernelDelta + userDelta + idleDelta.
        var totalDelta = (currentKernel - _previousKernel) + (currentUser - _previousUser);

        _previousIdle = currentIdle;
        _previousKernel = currentKernel;
        _previousUser = currentUser;

        if (totalDelta == 0)
        {
            return 0;
        }

        var busyDelta = totalDelta - idleDelta;
        return Math.Clamp(busyDelta * 100.0 / totalDelta, 0, 100);
    }
}
