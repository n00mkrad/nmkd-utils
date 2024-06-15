using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using static NmkdUtils.Logger;

namespace NmkdUtils
{
    public class OsUtils
    {
        public static readonly bool IsWindows;
        public static bool IsLinux => !IsWindows;
        public static readonly bool IsElevated;


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

        public class CommandResult
        {
            public string Output { get; set; } = "";
            public string StdOut { get; set; } = "";
            public string StdErr { get; set; } = "";
            public int ExitCode { get; set; }
            public TimeSpan RunTime { get; set; }
        }

        public static string RunCommand(string command) // TODO: out int exitCode ...
        {
            return RunCommandShell(command).Output;
        }

        public static string RunCommandWithKillswitch(string command, Func<bool> killswitch, bool printCmd = false)
        {
            return RunCommandShell(command, killswitch: killswitch).Output;
        }

        public static CommandResult RunCommandShell(string command, ProcessPriorityClass priority = ProcessPriorityClass.BelowNormal, Func<bool>? killswitch = null)
        {
            var sw = new NmkdStopwatch();
            CommandResult? result = null;

            try
            {
                string tempScript = "";

                if (IsLinux)
                {
                    tempScript = Path.Combine(Path.GetTempPath(), Path.GetTempFileName()) + ".sh";
                    File.WriteAllText(tempScript, command);
                    File.SetUnixFileMode(tempScript, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                    command = tempScript;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = !IsLinux ? "cmd.exe" : "/bin/bash",
                    Arguments = !IsLinux ? $"/c {command}" : $"-c {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
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
                    }
                };

                process.Start();
                process.PriorityClass = priority;
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (killswitch == null)
                {
                    process.WaitForExit();
                }
                else
                {
                    while (killswitch() == false)
                    {
                        Thread.Sleep(1000);

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

                result = new CommandResult { Output = output.ToString(), StdOut = stdout.ToString(), StdErr = stderr.ToString(), RunTime = sw.Elapsed };

                if (tempScript.IsNotEmpty())
                {
                    IoUtils.DeletePath(tempScript);
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

        /// <summary> Count how many instances (including self) of this executable are running </summary>
        public static int CountExecutableInstances(string exePath = "", bool quiet = true)
        {
            try
            {
                string currentExecutablePath = exePath.IsNotEmpty() ? exePath : Environment.ProcessPath;

                return Process.GetProcesses().AsParallel().WithDegreeOfParallelism(Environment.ProcessorCount).Select(p => {
                    try
                    {
                        return Path.GetFullPath(p.MainModule?.FileName);
                    }
                    catch
                    {
                        if (!quiet)
                        {
                            Log($"Error accessing process {p.Id}");
                        }

                        return null;
                    }
                }).Count(path => path != null && new FileInfo(path).FullName.Low() == new FileInfo(currentExecutablePath).FullName.Low());
            }
            catch (Exception ex)
            {
                Log(ex, "Error counting executable instances");
                return 0;
            }
        }

        public static string GetEnvVar(string name, string fallbackValue = "")
        {
            string? value = Environment.GetEnvironmentVariable(name);

            if (value.IsEmpty())
            {
                return fallbackValue;
            }

            return value;
        }

        public static void SetOwnProcessPriority(ProcessPriorityClass priority = ProcessPriorityClass.BelowNormal)
        {
            using Process self = Process.GetCurrentProcess();
            self.PriorityClass = priority;
        }
    }
}
