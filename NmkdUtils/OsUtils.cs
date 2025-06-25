using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using static NmkdUtils.Data.OsData;
using static NmkdUtils.Logger;

namespace NmkdUtils
{
    public class OsUtils
    {
        public static bool AllowProcessCreation = true;
        public static readonly bool IsWindows;
        public static bool IsLinux => !IsWindows;
        public static readonly bool IsElevated;

        public static List<Process> SessionProcesses = new();

        static OsUtils()
        {
            IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            IsElevated = IsAdminOrSudo();
        }

        [DllImport("libc")]
        public static extern uint getuid();

        /// <summary> Checks if the program is running as Administrator (Windows) or sudo (Linux) </summary>
        public static bool IsAdminOrSudo()
        {
            if (IsWindows)
            {
                return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            }
            else
            {
                return getuid() == 0;
            }
        }

        public delegate void OutputDelegate(string s);

        public class RunConfig
        {
            public string Command { get; set; } = "";
            public bool DontWait { get; set; } = false;
            public bool PrintExitCode { get; set; } = false;
            public int PrintOutputLines { get; set; } = 0;
            public ProcessPriorityClass? Priority { get; set; } = null;
            public bool Utf8 { get; set; } = true;
            public Func<bool>? Killswitch { get; set; } = null;
            public int KillswitchCheckIntervalMs = 1000;
            public OutputDelegate? OnStdout;
            public OutputDelegate? OnStderr;
            public OutputDelegate? OnOutput;

            public RunConfig() { }

            public RunConfig(string cmd, int printOutputLines = 0, bool printExitCode = false)
            {
                Command = cmd;
                PrintOutputLines = printOutputLines;
                PrintExitCode = printExitCode;
            }
        }

        public class CommandResult
        {
            public string Output { get; set; } = "";
            public string StdOut { get; set; } = "";
            public string StdErr { get; set; } = "";
            public int ExitCode { get; set; } = 0;
            public TimeSpan RunTime { get; set; } = TimeSpan.FromSeconds(0);
            public string RunTimeStr => FormatUtils.Time(RunTime);
        }

        public static string Run(string command, int printOutputLines = 0, bool printExitCode = false)
        {
            return Run(new RunConfig(command, printOutputLines, printExitCode)).Output;
        }

        public static CommandResult RunCommandShell(string cmd, int printOutputLines = 0, bool printExitCode = false)
        {
            return Run(new RunConfig(cmd, printOutputLines, printExitCode));
        }

        public static CommandResult Run(RunConfig cfg)
        {
            if (!AllowProcessCreation)
                return new CommandResult { ExitCode = -1 };

            var sw = Stopwatch.StartNew();
            CommandResult? result = null;

            try
            {
                string tempScript = "";

                if (IsLinux)
                {
                    tempScript = Path.Combine(Path.GetTempPath(), $"{Path.GetTempFileName()}.sh");
                    File.WriteAllText(tempScript, cfg.Command);
                    File.SetUnixFileMode(tempScript, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                    cfg.Command = tempScript;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = !IsLinux ? "cmd.exe" : "/bin/bash",
                    Arguments = !IsLinux ? $"/S /C {cfg.Command.Wrap()}" : $"-c {cfg.Command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = cfg.Utf8 ? Encoding.UTF8 : null,
                    StandardErrorEncoding = cfg.Utf8 ? Encoding.UTF8 : null,
                };

                Log($"{startInfo.FileName} {startInfo.Arguments}", Level.Verbose);

                using var process = new Process { StartInfo = startInfo };
                var output = new StringBuilder();
                var stdout = new StringBuilder();
                var stderr = new StringBuilder();
                var outputClosed = new TaskCompletionSource<bool>();
                var errorClosed = new TaskCompletionSource<bool>();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        outputClosed.SetResult(true);
                    }
                    else
                    {
                        output.AppendLine(e.Data);
                        stdout.AppendLine(e.Data);
                        Log($"[OUT] {e.Data}", Level.Debug);
                        cfg.OnStdout?.Invoke(e.Data);
                        cfg.OnOutput?.Invoke(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        errorClosed.SetResult(true);
                    }
                    else
                    {
                        output.AppendLine(e.Data);
                        stderr.AppendLine(e.Data);
                        Log($"[ERR] {e.Data}", Level.Debug);
                        cfg.OnStderr?.Invoke(e.Data);
                        cfg.OnOutput?.Invoke(e.Data);
                    }
                };

                ProcessPriorityClass? previousParentPrio = cfg.Priority == null ? null : GetOwnProcessPriority();
                SetOwnProcessPriority(cfg.Priority); // The only reliable way of setting the new child proc's priority is by changing the parent's priority...
                process.Start();
                SessionProcesses.Add(process);
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                SetOwnProcessPriority(previousParentPrio); // ...and afterwards changing the parent's priority back to what it was

                if (!cfg.DontWait)
                {
                    if (cfg.Killswitch == null)
                    {
                        process.WaitForExit();
                    }
                    else
                    {
                        while (cfg.Killswitch() == false)
                        {
                            Thread.Sleep(cfg.KillswitchCheckIntervalMs);

                            if (process.HasExited)
                                break;
                        }

                        // Killswitch true
                        if (!process.HasExited)
                        {
                            Log("Killswitch true, killing process.", Level.Verbose);
                            process.Kill(true);
                        }
                    }
                }

                // Ensure output and error streams have finished processing
                Task.WhenAll(outputClosed.Task, errorClosed.Task).Wait();

                result = new CommandResult { Output = output.ToString(), StdOut = stdout.ToString(), StdErr = stderr.ToString(), ExitCode = process.ExitCode, RunTime = sw.Elapsed };

                if (tempScript.IsNotEmpty())
                {
                    IoUtils.Delete(tempScript);
                }

                string logMsg = cfg.PrintExitCode ? $"Finished (Code {result.ExitCode})." : "";

                if (cfg.PrintOutputLines > 0 && result.Output.IsNotEmpty())
                {
                    var lines = result.Output.SplitIntoLines();
                    string p = lines.Length > cfg.PrintOutputLines ? $"...{Environment.NewLine}" : "";
                    logMsg += $" Process Output:{Environment.NewLine}{p}{string.Join(Environment.NewLine, lines.TakeLast(cfg.PrintOutputLines))}";
                }

                if (logMsg.IsNotEmpty())
                {
                    Log(logMsg.Trim());
                }
            }
            catch (Exception ex)
            {
                Log(ex, "Error running command");
                result = new CommandResult { ExitCode = 1, RunTime = sw.Elapsed };
            }

            return result;
        }

        public static Process NewProcess(bool hidden, string filename = "cmd.exe", Action<string> logAction = null, bool redirectStdin = false, Encoding outputEnc = null)
        {
            var p = new Process();
            p.StartInfo.UseShellExecute = !hidden;
            p.StartInfo.RedirectStandardOutput = hidden;
            p.StartInfo.RedirectStandardError = hidden;
            p.StartInfo.CreateNoWindow = hidden;
            p.StartInfo.FileName = filename;
            p.StartInfo.RedirectStandardInput = redirectStdin;

            if (outputEnc != null)
            {
                p.StartInfo.StandardOutputEncoding = outputEnc;
                p.StartInfo.StandardErrorEncoding = outputEnc;
            }

            if (hidden && logAction != null)
            {
                p.OutputDataReceived += (sender, line) => { logAction(line.Data); };
                p.ErrorDataReceived += (sender, line) => { logAction(line.Data); };
            }

            return p;
        }

        /// <summary>
        /// Get all instances (including self, unless <paramref name="excludeSelf"/> is true) of <paramref name="exePath"/> (defaults to own process if empty).<br/>
        /// Should take no more than ~50ms on a modern machine.
        /// </summary>
        public static List<Process> GetExecutableInstances(string exePath = "", bool excludeSelf = false)
        {
            try
            {
                exePath = exePath.IsNotEmpty() ? exePath : $"{Environment.ProcessPath}";
                string exeName = Path.GetFileNameWithoutExtension(exePath);
                int ownPid = Environment.ProcessId;

                var list = Process.GetProcesses().Where(p =>
                {
                    try
                    {
                        if (p == null || p.SessionId == 0 || p.ProcessName.Low() != exeName.Low())
                            return false;

                        return Path.GetFullPath(p.MainModule?.FileName) == Path.GetFullPath(exePath) && (!excludeSelf || p.Id != ownPid);
                    }
                    catch
                    {
                        return false;
                    }
                }).ToList();

                return list;
            }
            catch (Exception ex)
            {
                Log(ex, "Error retrieving executable processes");
                return [];
            }
        }

        /// <summary> Get all instances of a program with the specified executable name. File extension is optional. </summary>
        public static List<Process> GetProgramInstances(string exeName = "")
        {
            try
            {
                exeName = Path.GetFileNameWithoutExtension(exeName);

                var list = Process.GetProcesses().Where(p =>
                {
                    try
                    {
                        if (p == null || p.SessionId == 0)
                            return false;

                        return p.ProcessName.Low() == exeName.Low();
                    }
                    catch
                    {
                        return false;
                    }
                }).ToList();

                return list;
            }
            catch (Exception ex)
            {
                Log(ex, "Error retrieving executable processes");
                return [];
            }
        }

        /// <summary> Count how many instances (including self, unless <paramref name="excludeSelf"/> is true) of this executable are running. </summary>
        public static int CountExecutableInstances(string exePath = "", bool excludeSelf = false)
        {
            return GetExecutableInstances(exePath, excludeSelf).Count;
        }

        /// <summary> Gets the value of environment variable <paramref name="name"/>, if it does not exist, it returns <paramref name="fallbackValue"./> </summary>
        public static string GetEnvVar(string name, string fallbackValue = "", bool checkSysVars = true, bool checkUserVars = true, bool checkProcessVars = true)
        {
            string value = "";

            if (checkSysVars)
            {
                value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);
            }

            if (checkUserVars && value.IsEmpty())
            {
                value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
            }

            if (checkProcessVars && value.IsEmpty())
            {
                value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
            }

            return value.IsEmpty() ? fallbackValue : value;
        }

        public static ProcessPriorityClass GetOwnProcessPriority()
        {
            using Process self = Process.GetCurrentProcess();
            return self.PriorityClass;
        }

        public static void SetOwnProcessPriority(ProcessPriorityClass? priority = ProcessPriorityClass.BelowNormal)
        {
            if (priority == null)
                return;

            using Process self = Process.GetCurrentProcess();
            self.PriorityClass = priority == null ? ProcessPriorityClass.BelowNormal : (ProcessPriorityClass)priority;
            Log($"Process priority changed to {self.PriorityClass}", Level.Debug);
        }

        private static readonly ConcurrentDictionary<string, List<string>> _envExecutablesCache = [];

        public static List<string> GetEnvExecutable(string name, bool stopAfterFirst = true)
        {
            return _envExecutablesCache.GetOrAdd(name, GetEnvExecutablesNoCache(name, stopAfterFirst));
        }

        public static List<string> GetEnvExecutablesNoCache(string name, bool stopAfterFirst = true)
        {
            var exts = new string[] { ".exe", ".bat", ".cmd" };
            List<string> paths = [];
            List<string> dirs = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process).Split(';').Concat(Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine).Split(';')).Distinct().ToList();

            foreach (var dir in dirs)
            {
                foreach (var ext in exts)
                {
                    var path = Path.Combine(dir, $"{name}{ext}");

                    if (!File.Exists(path))
                        continue;

                    paths.Add(path);

                    if (stopAfterFirst)
                        return paths;
                }
            }

            return paths;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public enum WinMessage { Close, Minimize, Maximize, Restore, CloseX };

        public static void SendWinMessage(Process proc, WinMessage msg)
        {
            if (proc == null || msg == (WinMessage)(-1))
                return;

            if (msg == WinMessage.Close) SendMessage(proc.MainWindowHandle, WindowMsgs.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            else if (msg == WinMessage.Minimize) SendMessage(proc.MainWindowHandle, WindowMsgs.WM_SYSCOMMAND, (IntPtr)WindowMsgs.SC_MINIMIZE, IntPtr.Zero);
            else if (msg == WinMessage.Maximize) SendMessage(proc.MainWindowHandle, WindowMsgs.WM_SYSCOMMAND, (IntPtr)WindowMsgs.SC_MAXIMIZE, IntPtr.Zero);
            else if (msg == WinMessage.Restore) SendMessage(proc.MainWindowHandle, WindowMsgs.WM_SYSCOMMAND, (IntPtr)WindowMsgs.SC_RESTORE, IntPtr.Zero);
            else if (msg == WinMessage.CloseX) SendMessage(proc.MainWindowHandle, WindowMsgs.WM_SYSCOMMAND, (IntPtr)WindowMsgs.SC_CLOSE, IntPtr.Zero);
        }

        public static string GetExeFriendlyName(string exePath)
        {
            var info = FileVersionInfo.GetVersionInfo(exePath);
            var name = info.FileDescription;

            if (name.IsEmpty())
                name = info.ProductName;

            if (name.IsEmpty())
                name = Path.GetFileNameWithoutExtension(exePath);

            return name.Low().EndsWith(".exe") ? Path.GetFileNameWithoutExtension(name) : name;
        }

        [DllImport("User32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        public static TimeSpan GetIdleTime()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            GetLastInputInfo(ref lastInputInfo);

            uint lastInputTick = lastInputInfo.dwTime;
            uint currentTick = (uint)Environment.TickCount;

            return TimeSpan.FromMilliseconds(currentTick - lastInputTick);
        }
    }
}
