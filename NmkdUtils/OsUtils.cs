using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using static NmkdUtils.Logger;

namespace NmkdUtils
{
    public class OsUtils
    {
        public static bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsLinux => !IsWindows;

        public static string RunCommand(string command, bool printCmd = true)
        {
            return IsLinux ? RunCommandLinux(command, printCmd) : RunCommandWin(command, null, printCmd);
        }

        public static string RunCommandWithKillswitch(string command, Func<bool> killswitch, bool printCmd = true)
        {
            return IsLinux ? RunCommandLinux(command, printCmd) : RunCommandWin(command, killswitch, printCmd);
        }

        public static string RunCommandWin(string command, Func<bool>? killswitch = null, bool printCmd = true)
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
                        Log($"[STDOUT] {e.Data}", Level.Verbose);
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
                        Log($"[STDERR] {e.Data}", Level.Verbose);
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
    }
}
