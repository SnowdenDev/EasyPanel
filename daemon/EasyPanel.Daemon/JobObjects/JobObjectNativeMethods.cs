using System.Runtime.InteropServices;

namespace EasyPanel.Daemon.JobObjects;

/// <summary>
/// LibraryImport (source-generated marshalling), not classic DllImport — Native AOT
/// removes the JIT-time marshalling stubs DllImport's default marshalling relies on.
/// See the Native AOT section of docs/architecture.md.
/// </summary>
internal static partial class JobObjectNativeMethods
{
    [LibraryImport("kernel32.dll", EntryPoint = "CreateJobObjectW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    internal static partial IntPtr CreateJobObjectW(IntPtr lpJobAttributes, string? lpName);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

    [LibraryImport("kernel32.dll", EntryPoint = "SetInformationJobObject", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetExtendedLimitInformation(
        IntPtr hJob,
        JobObjectInfoClass jobObjectInfoClass,
        ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION lpJobObjectInfo,
        int cbJobObjectInfoLength);

    [LibraryImport("kernel32.dll", EntryPoint = "SetInformationJobObject", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetCpuRateControlInformation(
        IntPtr hJob,
        JobObjectInfoClass jobObjectInfoClass,
        ref JOBOBJECT_CPU_RATE_CONTROL_INFORMATION lpJobObjectInfo,
        int cbJobObjectInfoLength);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool TerminateJobObject(IntPtr hJob, uint uExitCode);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool CloseHandle(IntPtr hObject);
}
