using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace SafarSuite.ControlDesk.Infrastructure.Security.MachineSecrets;

[SupportedOSPlatform("windows")]
internal sealed class WindowsRestorePrivilegeScope : IDisposable
{
    private const uint TokenAdjustPrivileges = 0x0020;
    private const uint TokenQuery = 0x0008;
    private const uint SePrivilegeEnabled = 0x00000002;
    private const int ErrorNotAllAssigned = 1300;

    private readonly SafeAccessTokenHandle _token;
    private readonly TokenPrivileges _previousState;
    private bool _disposed;

    private WindowsRestorePrivilegeScope(
        SafeAccessTokenHandle token,
        TokenPrivileges previousState)
    {
        _token = token;
        _previousState = previousState;
    }

    public static WindowsRestorePrivilegeScope Enable()
    {
        if (!OpenProcessToken(
                Process.GetCurrentProcess().Handle,
                TokenAdjustPrivileges | TokenQuery,
                out var token))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        try
        {
            if (!LookupPrivilegeValue(null, "SeRestorePrivilege", out var luid))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var requestedState = new TokenPrivileges
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = SePrivilegeEnabled
            };

            if (!AdjustTokenPrivileges(
                    token,
                    disableAllPrivileges: false,
                    ref requestedState,
                    Marshal.SizeOf<TokenPrivileges>(),
                    out var previousState,
                    out _))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var error = Marshal.GetLastWin32Error();

            if (error == ErrorNotAllAssigned)
            {
                throw new UnauthorizedAccessException(
                    "The elevated machine-secret lifecycle requires SeRestorePrivilege.");
            }

            if (error != 0)
            {
                throw new Win32Exception(error);
            }

            return new WindowsRestorePrivilegeScope(token, previousState);
        }
        catch
        {
            token.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        var previousState = _previousState;
        _ = AdjustTokenPrivileges(
            _token,
            disableAllPrivileges: false,
            ref previousState,
            0,
            out _,
            out _);
        _token.Dispose();
    }

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenProcessToken(
        IntPtr processHandle,
        uint desiredAccess,
        out SafeAccessTokenHandle tokenHandle);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LookupPrivilegeValue(
        string? systemName,
        string name,
        out Luid luid);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AdjustTokenPrivileges(
        SafeAccessTokenHandle tokenHandle,
        [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges,
        ref TokenPrivileges newState,
        int bufferLength,
        out TokenPrivileges previousState,
        out int returnLength);

    [StructLayout(LayoutKind.Sequential)]
    private struct Luid
    {
        public uint LowPart;

        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TokenPrivileges
    {
        public uint PrivilegeCount;

        public Luid Luid;

        public uint Attributes;
    }
}
