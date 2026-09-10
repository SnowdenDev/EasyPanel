using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EasyPanel.Daemon.JobObjects;

/// <summary>
/// Wraps a launched process in a Windows Job Object. KillOnJobClose is always set — that's
/// the core "no orphaned children" guarantee this whole product relies on — CPU/RAM limits
/// are optional on top of it. See the Native AOT section of docs/architecture.md.
/// </summary>
internal sealed class ManagedJobObject : IDisposable
{
    private readonly JobObjectSafeHandle _handle;

    private ManagedJobObject(JobObjectSafeHandle handle)
    {
        _handle = handle;
    }

    public static ManagedJobObject Create(int? cpuLimitPercent, long? memoryLimitBytes)
    {
        var rawHandle = JobObjectNativeMethods.CreateJobObjectW(IntPtr.Zero, lpName: null);
        if (rawHandle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateJobObjectW failed.");
        }

        var jobObject = new ManagedJobObject(new JobObjectSafeHandle(rawHandle));

        jobObject.SetExtendedLimits(memoryLimitBytes);

        if (cpuLimitPercent is { } percent)
        {
            jobObject.SetCpuLimit(percent);
        }

        return jobObject;
    }

    private void SetExtendedLimits(long? memoryLimitBytes)
    {
        var limitFlags = JobObjectLimitFlags.KillOnJobClose;
        if (memoryLimitBytes is { } bytes)
        {
            limitFlags |= JobObjectLimitFlags.ProcessMemory;
        }

        var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION { LimitFlags = limitFlags },
            ProcessMemoryLimit = memoryLimitBytes is { } limit ? (nuint)limit : 0,
        };

        var handle = _handle.DangerousGetHandle();
        var succeeded = JobObjectNativeMethods.SetExtendedLimitInformation(
            handle,
            JobObjectInfoClass.ExtendedLimitInformation,
            ref info,
            Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>());

        if (!succeeded)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "SetInformationJobObject (extended limits) failed.");
        }
    }

    private void SetCpuLimit(int cpuLimitPercent)
    {
        // CpuRate is expressed in units of 1/10000 of a CPU (i.e. 10000 == 100%).
        var info = new JOBOBJECT_CPU_RATE_CONTROL_INFORMATION
        {
            ControlFlags = JobObjectCpuRateControlFlags.Enable | JobObjectCpuRateControlFlags.HardCap,
            CpuRate = (uint)(cpuLimitPercent * 100),
        };

        var handle = _handle.DangerousGetHandle();
        var succeeded = JobObjectNativeMethods.SetCpuRateControlInformation(
            handle,
            JobObjectInfoClass.CpuRateControlInformation,
            ref info,
            Marshal.SizeOf<JOBOBJECT_CPU_RATE_CONTROL_INFORMATION>());

        if (!succeeded)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "SetInformationJobObject (CPU rate control) failed.");
        }
    }

    /// <summary>
    /// Must be called before the process can run away from us — assigning a process that
    /// has already spawned children of its own would leave those children unmanaged.
    /// </summary>
    public void AssignProcess(Process process)
    {
        var processHandle = process.SafeHandle;
        var addRefSucceeded = false;

        try
        {
            processHandle.DangerousAddRef(ref addRefSucceeded);

            var succeeded = JobObjectNativeMethods.AssignProcessToJobObject(_handle.DangerousGetHandle(), processHandle.DangerousGetHandle());
            if (!succeeded)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "AssignProcessToJobObject failed.");
            }
        }
        finally
        {
            if (addRefSucceeded)
            {
                processHandle.DangerousRelease();
            }
        }
    }

    /// <summary>Kills every process still in the job, in one call, regardless of how many there are.</summary>
    public void TerminateAll(uint exitCode = 1)
    {
        if (!JobObjectNativeMethods.TerminateJobObject(_handle.DangerousGetHandle(), exitCode) && !_handle.IsClosed)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "TerminateJobObject failed.");
        }
    }

    public void Dispose() => _handle.Dispose();
}
