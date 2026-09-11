using System.Runtime.InteropServices;

namespace EasyPanel.Daemon.Infrastructure;

/// <summary>
/// LibraryImport (source-generated marshalling), not classic DllImport — Native AOT
/// removes the JIT-time marshalling stubs DllImport's default marshalling relies on. Same
/// approach as JobObjectNativeMethods.
/// </summary>
internal static partial class SystemStatsNativeMethods
{
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

    [StructLayout(LayoutKind.Sequential)]
    internal struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;

        internal readonly ulong ToTicks() => ((ulong)dwHighDateTime << 32) | dwLowDateTime;
    }
}
