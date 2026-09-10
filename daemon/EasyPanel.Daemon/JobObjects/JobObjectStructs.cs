using System.Runtime.InteropServices;

namespace EasyPanel.Daemon.JobObjects;

/// <summary>Win32 JOB_OBJECT_LIMIT_* flags relevant here — see docs/architecture.md.</summary>
internal static class JobObjectLimitFlags
{
    public const uint KillOnJobClose = 0x2000;
    public const uint ProcessMemory = 0x0100;
}

internal static class JobObjectCpuRateControlFlags
{
    public const uint Enable = 0x1;
    public const uint HardCap = 0x4;
}

[StructLayout(LayoutKind.Sequential)]
internal struct JOBOBJECT_BASIC_LIMIT_INFORMATION
{
    public long PerProcessUserTimeLimit;
    public long PerJobUserTimeLimit;
    public uint LimitFlags;
    public nuint MinimumWorkingSetSize;
    public nuint MaximumWorkingSetSize;
    public uint ActiveProcessLimit;
    public nuint Affinity;
    public uint PriorityClass;
    public uint SchedulingClass;
}

[StructLayout(LayoutKind.Sequential)]
internal struct IO_COUNTERS
{
    public ulong ReadOperationCount;
    public ulong WriteOperationCount;
    public ulong OtherOperationCount;
    public ulong ReadTransferCount;
    public ulong WriteTransferCount;
    public ulong OtherTransferCount;
}

/// <summary>
/// Blittable by construction (primitive/nuint fields only) — this is what lets
/// LibraryImport marshal it under Native AOT with no custom marshaller. See the
/// Native AOT section of docs/architecture.md.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
{
    public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
    public IO_COUNTERS IoInfo;
    public nuint ProcessMemoryLimit;
    public nuint JobMemoryLimit;
    public nuint PeakProcessMemoryUsed;
    public nuint PeakJobMemoryUsed;
}

/// <summary>
/// The real Win32 struct has a union here (CpuRate / Weight / MinRate+MaxRate) — we only
/// ever use the hard-cap CpuRate mode, so a single uint field covers the layout we need.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct JOBOBJECT_CPU_RATE_CONTROL_INFORMATION
{
    public uint ControlFlags;
    public uint CpuRate;
}
