using Microsoft.Win32.SafeHandles;

namespace EasyPanel.Daemon.JobObjects;

/// <summary>
/// Deterministic cleanup for the native job object handle — a raw IntPtr here would risk
/// a leaked handle leaving kill-on-close semantics dangling. AOT-safe: no reflection.
/// </summary>
internal sealed class JobObjectSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public JobObjectSafeHandle(IntPtr preexistingHandle) : base(ownsHandle: true)
    {
        SetHandle(preexistingHandle);
    }

    protected override bool ReleaseHandle() => JobObjectNativeMethods.CloseHandle(handle);
}
