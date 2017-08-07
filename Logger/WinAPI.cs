using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Logger
{
    static class WinAPI
    {
        public const int ERROR_UNKNOWN = -2147467259;
        public const int ERROR_ACCESS_DENIED = 5;
        public const int ERROR_SERVICE_EXISTS = 0x431;

        public static int CreateProcessInSession(uint sessionID, string fileName, string workingDirectory, string commandLine)
        {
            IntPtr userToken = default(IntPtr);

            try
            {
                bool success = WinAPI.WTSQueryUserToken((uint)sessionID, out userToken);  // Returns primary user token (as opposed to impersonation token)


                if (!success)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Exception($"Failed to query user token, error code: {error}");
                }
                else
                {
                    return CreateProcessAsUser(userToken, true, fileName, workingDirectory, commandLine);
                }
            }
            finally
            {
                if (userToken != default(IntPtr))
                    WinAPI.CloseHandle(userToken);
            }
        }

        public static int CreateProcessAsUser(IntPtr hUserToken, bool tokenIsPrimary, string fileName, string workingDirectory, string commandLine, string desktop=@"WinSta0\Default")
        {
            IntPtr hDupedToken = IntPtr.Zero;
            WinAPI.PROCESS_INFORMATION pi = new WinAPI.PROCESS_INFORMATION();
            
            try
            {
                bool success;

                WinAPI.SECURITY_ATTRIBUTES sa = new WinAPI.SECURITY_ATTRIBUTES();

                sa.Length = Marshal.SizeOf(sa);

                if (!tokenIsPrimary)
                {
                    success = WinAPI.DuplicateTokenEx(
                          hUserToken,
                          WinAPI.GENERIC_ALL_ACCESS,
                          ref sa,
                          (int)WinAPI.SECURITY_IMPERSONATION_LEVEL.SecurityIdentification,
                          (int)WinAPI.TOKEN_TYPE.TokenPrimary,
                          ref hDupedToken
                       );

                    if (!success)
                    {
                        throw new Exception("DuplicateTokenEx failed");
                    }
                }


                WinAPI.STARTUPINFO si = new WinAPI.STARTUPINFO();
                si.cb = Marshal.SizeOf(si);
                si.lpDesktop = @"WinSta0\Default";

                success = WinAPI.CreateProcessAsUser(
                    tokenIsPrimary ? hUserToken : hDupedToken,
                    fileName,
                    string.Join(" ", Path.GetFileName(fileName), commandLine),
                    ref sa, ref sa,
                    false, 0, IntPtr.Zero,
                    workingDirectory, ref si, ref pi
                );

                if (!success)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Exception($"CreateProcessAsUser failed with error code: {error}");
                }

                return pi.dwProcessID;
            }
            finally
            {
                if (pi.hProcess != IntPtr.Zero)
                    WinAPI.CloseHandle(pi.hProcess);

                if (pi.hThread != IntPtr.Zero)
                    WinAPI.CloseHandle(pi.hThread);

                if (hDupedToken != IntPtr.Zero)
                    WinAPI.CloseHandle(hDupedToken);
            }
        }

        [DllImport("wtsapi32.dll", SetLastError=true)]
        public static extern bool WTSQueryUserToken(UInt32 sessionId, out IntPtr Token);

        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public Int32 dwProcessID;
            public Int32 dwThreadID;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public Int32 Length;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        public enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }

        public enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        public const int GENERIC_ALL_ACCESS = 0x10000000;

        [DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUser", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)
        ]
        public static extern bool
           CreateProcessAsUser(IntPtr hToken, string lpApplicationName, string lpCommandLine,
                               ref SECURITY_ATTRIBUTES lpProcessAttributes, ref SECURITY_ATTRIBUTES lpThreadAttributes,
                               bool bInheritHandle, Int32 dwCreationFlags, IntPtr lpEnvrionment,
                               string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo,
                               ref PROCESS_INFORMATION lpProcessInformation);

        [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx")]
        public static extern bool
           DuplicateTokenEx(IntPtr hExistingToken, Int32 dwDesiredAccess,
                            ref SECURITY_ATTRIBUTES lpThreadAttributes,
                            Int32 ImpersonationLevel, Int32 dwTokenType,
                            ref IntPtr phNewToken);
    }
}
