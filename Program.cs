
#define USE_DEBUGGER
#define SHOW_COPYRIGHT
// #define REDIRECT_STD
// #define USE_UTF8

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ForceWT
{
    class Program
    {
        static void Main(string[] args)
        {

            // get application name
            string applicationName = "powershell";
            if (args.Length > 0)
                applicationName = Path.GetFileNameWithoutExtension(args[0]).ToLower();

            // launch WindowsTerminal if we are launched standalone
            if (IsStandalone())
            {
                // format arguments
                // -w 0  => opens in already opened instance (only in WT Preview!)
                // -d  => set working directory
                // application name  =>  cmd or ps
                // GetCommandLine() Gets unformated win32 commandline
                string arguments = $"-w 0 -d \"{Environment.CurrentDirectory}\" {applicationName} {GetCommandLine().Replace("/c", "/k").Replace("\"", "\\\"")}";
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "wt.exe",
                    Arguments = arguments,
                    UseShellExecute = false
                });
#if DEBUG
                Console.WriteLine(arguments);
                Console.ReadLine();
#endif
                return;
            }

#if SHOW_COPYRIGHT
            // copyright
            Console.WriteLine("Force Windows Terminal");
            Console.WriteLine($"© {DateTime.Now.ToString("yyyy")} Lukas Kurz alias ShortDevelopment");
            Console.WriteLine();
#endif

#if DEBUG
            // output args
            Console.WriteLine($"Args: {FormatArgs(args)}");
            Console.WriteLine($"IsStandalone: {IsStandalone()}");
            Console.WriteLine();
#endif

#if USE_UTF8
            // set encoding to utf-8 ⚠ Font may change ❗
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;
#endif

            // set console title
            switch (applicationName.ToLower())
            {
                case "powershell":
                    Console.Title = "Windows PowerShell";
                    break;
                case "cmd":
                    Console.Title = "Eingabeaufforderung";
                    break;
                default:
                    throw new ArgumentException("Unkown application!");
            }

            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
#if REDIRECT_STD
            // redirect handles
            si.hStdError = GetStdHandle(STD.OUTPUT_HANDLE);
            si.hStdOutput = GetStdHandle(STD.OUTPUT_HANDLE);
            si.hStdInput = GetStdHandle(STD.INPUT_HANDLE);
            si.dwFlags = STARTF_USESTDHANDLES;
#endif

            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

            // start process in debugging mode to ignore "Image File Execution"
            uint flags = DEBUG_ONLY_THIS_PROCESS;
#if !USE_DEBUGGER
            flags = 0;
#endif
            if (!CreateProcess(null, (args.Length > 0) ? GetCommandLine() : applicationName, IntPtr.Zero, IntPtr.Zero, false, flags, IntPtr.Zero, null, ref si, out pi))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

#if USE_DEBUGGER
            // dummy debugger loop
            DEBUG_EVENT debugEvent = new DEBUG_EVENT();
            bool isDebugging = true;
            while (isDebugging && WaitForDebugEvent(ref debugEvent, INFINITE))
            {
                // exit debugger loop as soon as process terminates
                if (debugEvent.dwDebugEventCode == EXIT_PROCESS_DEBUG_EVENT)
                    isDebugging = false;

                // always continue ...
                ContinueStatus dwContinueStatus = ContinueStatus.DBG_CONTINUE;

                // ... execpt we have an exception!
                // if we don't do this powershell will crash!
                if (debugEvent.dwDebugEventCode == EXCEPTION_DEBUG_EVENT)
                    dwContinueStatus = ContinueStatus.DBG_EXCEPTION_NOT_HANDLED;

                // continue debugger loop
                ContinueDebugEvent(debugEvent.dwProcessId, debugEvent.dwThreadId, dwContinueStatus);
            }
#endif

            // wait for process to terminate
            WaitForSingleObject(pi.hProcess, INFINITE);

#if DEBUG
            // output process exit code
            Console.WriteLine();
            uint exitCode;
            GetExitCodeProcess(pi.hProcess, out exitCode);
            Console.WriteLine($"ExitCode: {exitCode}");
#endif

        }

        protected static string FormatArgs(string[] args, bool includeFirst = true)
        {
            if (!includeFirst)
            {
                var list = args.ToList();
                if (args.Count() > 0)
                    list.RemoveAt(0);
                args = list.ToArray();
            }
            return string.Join(" ", args.Select((arg) => $"\"{arg}\""));
        }

        protected static string GetCommandLine()
        {            
            string cmd = Marshal.PtrToStringAuto(GetCommandLineNative());
            string location = GetAssemblyLocation();
            cmd = cmd.Replace($"\"{location}\"", "");
            cmd = cmd.Replace($"{location}", "");
            return cmd.Trim();
        }

        protected static string GetAssemblyLocation()
        {
            return typeof(Program).Assembly.Location;
        }

        protected static bool IsStandalone()
        {
            IntPtr hwnd = GetConsoleWindow();
            uint pId;
            GetWindowThreadProcessId(hwnd, out pId);
            return Process.GetCurrentProcess().ProcessName == Process.GetProcessById((int)pId).ProcessName;
        }

        #region Win API

        #region Security
        [StructLayout(LayoutKind.Sequential)]
        protected struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }
        #endregion

        #region STD Handle
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(STD nStdHandle);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        protected static extern bool SetStdHandle(STD nStdHandle, IntPtr hHandle);
        protected enum STD
        {
            INPUT_HANDLE = -10,
            OUTPUT_HANDLE = -11,
            ERROR_HANDLE = -12
        }
        #endregion

        #region CreateProcess

        const int STARTF_USESTDHANDLES = 0x00000100;

        const int CREATE_NO_WINDOW = 0x08000000;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        protected static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);
        // This also works with CharSet.Ansi as long as the calling function uses the same character set.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        protected struct STARTUPINFOEX
        {
            public STARTUPINFO StartupInfo;
            public IntPtr lpAttributeList;
        }

        // This also works with CharSet.Ansi as long as the calling function uses the same character set.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        protected struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
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
        protected struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }
        #endregion

        #region Debug
        const int DEBUG_PROCESS = 0x00000001;
        const int DEBUG_ONLY_THIS_PROCESS = 0x00000002;

        const int EXIT_PROCESS_DEBUG_EVENT = 5;
        const int EXCEPTION_DEBUG_EVENT = 1;

        [DllImport("kernel32.dll", EntryPoint = "WaitForDebugEvent")]
        protected static extern bool WaitForDebugEvent(ref DEBUG_EVENT lpDebugEvent, uint dwMilliseconds);

        [StructLayout(LayoutKind.Sequential)]
        public struct DEBUG_EVENT
        {
            public uint dwDebugEventCode;
            public int dwProcessId;
            public int dwThreadId;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 86, ArraySubType = UnmanagedType.U1)]
            byte[] debugInfo;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ContinueDebugEvent(int dwProcessId, int dwThreadId, ContinueStatus dwContinueStatus);
        public enum ContinueStatus : uint
        {
            DBG_CONTINUE = 0x00010002,
            DBG_EXCEPTION_NOT_HANDLED = 0x80010001,
            DBG_REPLY_LATER = 0x40010001
        }
        #endregion

        #region ExitCode
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        protected static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);
        #endregion

        #region Wait
        [DllImport("kernel32.dll", SetLastError = true)]
        protected static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);
        const UInt32 INFINITE = 0xFFFFFFFF;
        #endregion

        #region Console Window
        [DllImport("kernel32.dll")]
        protected static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        #endregion

        #endregion

        #region Command Line
        [DllImport("kernel32.dll", EntryPoint = "GetCommandLine", CharSet = CharSet.Auto)]
        private static extern System.IntPtr GetCommandLineNative();
        #endregion

    }
}
