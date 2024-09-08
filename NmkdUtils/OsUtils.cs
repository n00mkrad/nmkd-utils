using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using static NmkdUtils.Logger;

namespace NmkdUtils
{
    public class OsUtils
    {
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
            public int PrintOutputLines { get; set; } = 0;
            public ProcessPriorityClass? Priority { get; set; } = null;
            public Func<bool>? Killswitch { get; set; } = null;
            public int KillswitchCheckIntervalMs = 1000;
            public OutputDelegate? OnStdout;
            public OutputDelegate? OnStderr;
            public OutputDelegate? OnOutput;

            public RunConfig() { }

            public RunConfig(string cmd, int printOutputLines = 0)
            {
                Command = cmd;
                PrintOutputLines = printOutputLines;
            }
        }

        public class CommandResult
        {
            public string Output { get; set; } = "";
            public string StdOut { get; set; } = "";
            public string StdErr { get; set; } = "";
            public int ExitCode { get; set; } = 0;
            public TimeSpan RunTime { get; set; }
        }

        public static string RunCommand(string command, int printOutputLines = 0)
        {
            return Run(new RunConfig(command, printOutputLines)).Output;
        }

        public static CommandResult RunCommandShell(string cmd, int printOutputLines = 0)
        {
            var cfg = new RunConfig(cmd, printOutputLines);
            return Run(cfg);
        }

        public static CommandResult Run(RunConfig cfg)
        {
            var sw = new NmkdStopwatch();
            CommandResult? result = null;

            try
            {
                string tempScript = "";

                if (IsLinux)
                {
                    tempScript = Path.Combine(Path.GetTempPath(), Path.GetTempFileName()) + ".sh";
                    File.WriteAllText(tempScript, cfg.Command);
                    File.SetUnixFileMode(tempScript, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                    cfg.Command = tempScript;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = !IsLinux ? "cmd.exe" : "/bin/bash",
                    Arguments = !IsLinux ? $"/c {cfg.Command}" : $"-c {cfg.Command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
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
                        Log($"[STDOUT] {e.Data}", Level.Debug);
                        cfg.OnStdout?.Invoke($"{e.Data}");
                        cfg.OnOutput?.Invoke($"{e.Data}");
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
                        Log($"[STDERR] {e.Data}", Level.Debug);
                        cfg.OnStderr?.Invoke($"{e.Data}");
                        cfg.OnOutput?.Invoke($"{e.Data}");
                    }
                };

                ProcessPriorityClass? previousParentPrio = cfg.Priority == null ? null : GetOwnProcessPriority();
                SetOwnProcessPriority(cfg.Priority); // The only reliable way of setting the new child proc's priority is by changing the parent's priority...
                process.Start();
                SessionProcesses.Add(process);
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                SetOwnProcessPriority(previousParentPrio); // ...and afterwards changing the parent's priority back to what it was

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

                // Ensure output and error streams have finished processing
                Task.WhenAll(outputClosed.Task, errorClosed.Task).Wait();

                result = new CommandResult { Output = output.ToString(), StdOut = stdout.ToString(), StdErr = stderr.ToString(), ExitCode = process.ExitCode, RunTime = sw.Elapsed };

                if (tempScript.IsNotEmpty())
                {
                    IoUtils.DeletePath(tempScript);
                }

                if (cfg.PrintOutputLines > 0)
                {
                    Log($"Finished (Code {result.ExitCode}). Output:{Environment.NewLine}...{Environment.NewLine}{string.Join(Environment.NewLine, result.Output.SplitIntoLines().TakeLast(cfg.PrintOutputLines))}");
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

        /// <summary> Get all instances (including self, unless <paramref name="excludeSelf"/> is true) of this executable. Should take no more than ~50ms on a modern machine </summary>
        public static List<Process> GetExecutableInstances(string exePath = "", bool excludeSelf = false)
        {
            try
            {
                string? currentExecutablePath = exePath.IsNotEmpty() ? exePath : Environment.ProcessPath;

                if (currentExecutablePath.IsEmpty())
                {
                    throw new Exception($"No executable path was specified/detected.");
                }

                int ownPid = Environment.ProcessId;

                return Process.GetProcesses().AsParallel().Where(p =>
                {
                    try
                    {
                        string processPath = Path.GetFullPath(p.MainModule?.FileName);
                        return processPath == Path.GetFullPath(currentExecutablePath) && (!excludeSelf || p.Id != ownPid);
                    }
                    catch
                    {
                        return false;
                    }
                }).ToList();
            }
            catch (Exception ex)
            {
                Log(ex, "Error retrieving executable processes");
                return new List<Process>();
            }
        }

        /// <summary> Count how many instances (including self, unless <paramref name="excludeSelf"/> is true) of this executable are running. </summary>
        public static int CountExecutableInstances(string exePath = "", bool excludeSelf = false)
        {
            return GetExecutableInstances(exePath, excludeSelf).Count;
        }

        /// <summary> Gets the value of environment variable <paramref name="name"/>, if it does not exist, it returns <paramref name="fallbackValue"/> </summary>
        public static string GetEnvVar(string name, string fallbackValue = "", bool userScope = false)
        {
            string? value = Environment.GetEnvironmentVariable(name);

            if (value.IsEmpty())
            {
                value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
            }

            if (value.IsEmpty())
            {
                return fallbackValue;
            }

            return value;
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
    }
}
