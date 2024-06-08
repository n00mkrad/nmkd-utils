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


        static OsUtils ()
        {
            IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            IsElevated = IsAdminOrSudo();
        }

        [DllImport("libc")]
        public static extern uint getuid();

        /// <summary> Checks if the program is running as Administrator (Windows) or sudo (Linux) </summary>
        public static bool IsAdminOrSudo ()
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

        public static string RunCommand(string command, bool printCmd = false) // TODO: out int exitCode ...
        {
            return IsLinux ? RunCommandLinux(command, printCmd) : RunCommandWin(command, null, printCmd);
        }

        public static string RunCommandWithKillswitch(string command, Func<bool> killswitch, bool printCmd = false)
        {
            return IsLinux ? RunCommandLinux(command, printCmd) : RunCommandWin(command, killswitch, printCmd);
        }

        public static string RunCommandWin(string command, Func<bool>? killswitch = null, bool printCmd = false)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Log($"cmd.exe /c {command}", printCmd ? Level.Info : Level.Verbose);

                using var process = new Process { StartInfo = startInfo };
                var output = new StringBuilder();
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
                        Log($"[STDERR] {e.Data}", Level.Debug);
                    }
                };

                process.Start();
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

                return output.ToString();
            }
            catch (Exception ex)
            {
                Log(ex, "Error running command");
            }

            return "";
        }

        public static string RunCommandLinux(string command, bool printCmd = true)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using Process? process = Process.Start(startInfo);
                if (process != null)
                {
                    process.WaitForExit();
                    string output = process.StandardOutput.ReadToEnd();
                    Log(output, printCmd ? Level.Info : Level.Verbose);
                    return output;
                }
                else
                {
                    LogErr("Failed to start the process.");
                }
            }
            catch (Exception ex)
            {
                Log(ex, "Error running command");
            }

            return "";
        }

        public static string GetProcStdOut(Process process, bool includeStdErr = false, ProcessPriorityClass priority = ProcessPriorityClass.BelowNormal)
        {
            if (includeStdErr)
                process.StartInfo.Arguments += " 2>&1";

            process.Start();
            process.PriorityClass = priority;

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
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

        public static int CountExecutableInstances()
        {
            // Get the full path of the current executable
            var currentExecutablePath = Process.GetCurrentProcess().MainModule?.FileName;

            if (string.IsNullOrEmpty(currentExecutablePath))
            {
                LogWrn("Unable to determine the path of the current executable.");
                return 0;
            }

            // Normalize the path to ensure uniformity across checks (optional)
            currentExecutablePath = Path.GetFullPath(currentExecutablePath);

            // Count how many processes have the same executable path
            int count = 0;
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    string processPath = process.MainModule?.FileName;

                    if (!string.IsNullOrEmpty(processPath))
                    {
                        processPath = Path.GetFullPath(processPath);

                        if (processPath.Equals(currentExecutablePath, StringComparison.OrdinalIgnoreCase))
                        {
                            count++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle access denied and other exceptions
                    // This often happens if the process does not have permission to query certain system processes.
                    // Log($"Error accessing process: {ex.Message}");
                }
            }

            return count;
        }
    }
}
