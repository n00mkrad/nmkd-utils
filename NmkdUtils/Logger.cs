using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace NmkdUtils
{
    public class Logger
    {
        public static string LogsDir { get => Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "Logs")).FullName; }
        public enum Level { Debug, Verbose, Info, Warning, Error }
        public static Level ConsoleLogLevel = Level.Info;
        public static Level FileLogLevel = Level.Info;
        public static bool PrintFullLevelNames = false;

        private static BlockingCollection<(string, Level)> _logQueue = new BlockingCollection<(string, Level)>();
        private static Thread _loggingThread;

        private static readonly Dictionary<Level, ConsoleColor> _logLevelColors = new()
        {
            { Level.Debug, ConsoleColor.DarkGray },
            { Level.Verbose, ConsoleColor.Gray },
            { Level.Info, ConsoleColor.White },
            { Level.Warning, ConsoleColor.Yellow },
            { Level.Error, ConsoleColor.Red },
        };

        private static readonly Dictionary<Level, string> _logLevelNames = new()
        {
            { Level.Debug, "DBG" },
            { Level.Verbose, "VRB" },
            { Level.Info, "INF" },
            { Level.Warning, "WRN" },
            { Level.Error, "ERR" },
        };

        private static readonly int _maxLogTypeStrLen = Enum.GetNames(typeof(Level)).Max(name => name.Length);

        static Logger()
        {
            _loggingThread = new Thread(new ThreadStart(ProcessLogQueue)) { IsBackground = true };
            _loggingThread.Start();
        }

        public static void StopLogging()
        {
            _logQueue.CompleteAdding();
            _loggingThread.Join(); // Ensure the thread exits by joining it.
        }

        private static void ProcessLogQueue()
        {
            foreach (var entry in _logQueue.GetConsumingEnumerable())
            {
                WriteLog(entry.Item1, entry.Item2);
            }
        }

        public static void LogConditional(object o, bool condition, Level level = Level.Info)
        {
            if (condition)
            {
                Log(o, level);
            }
        }

        public static void LogConditional(object o, Func<bool> condition, Level level = Level.Info)
        {
            if (condition())
            {
                Log(o, level);
            }
        }

        public static void Log(object o, Level level = Level.Info)
        {
            if ((int)level >= (int)ConsoleLogLevel || (int)level >= (int)FileLogLevel)
            {
                _logQueue.Add(($"{o}", level));
            }
        }

        public static void Log(Exception e, string note)
        {
            Log($"{(note.IsEmpty() ? "" : $"{note} - ")}{e.Message}{Environment.NewLine}{e.StackTrace}", Level.Error);
        }

        public static void LogWrn(object o)
        {
            Log(o, Level.Warning);
        }

        public static void LogErr(object o)
        {
            Log(o, Level.Error);
        }

        public static void WriteLog(string msg, Level level)
        {
            if ((int)level >= (int)ConsoleLogLevel)
            {
                var lines = msg.SplitIntoLines();
                string firstLinePrefix = PrintFullLevelNames ? $"[{level.ToString().Up().PadRight(_maxLogTypeStrLen, '.')}]" : $"[{_logLevelNames[level]}]";

                for (int i = 0; i < lines.Length; i++)
                {
                    string prefix = i == 0 ? firstLinePrefix : "".PadRight(firstLinePrefix.Length);
                    lines[i] = $"{prefix} {lines[i].Trim()}";
                }

                Console.ForegroundColor = _logLevelColors[level];
                Console.WriteLine(string.Join(Environment.NewLine, lines));
                Console.ResetColor();
            }

            if ((int)level >= (int)FileLogLevel)
            {
                var now = DateTime.Now;
                string time = now.ToString("yyyy-MM-dd HH:mm:ss");
                string day = now.ToString("yyyy-MM-dd");

                var lines = msg.SplitIntoLines();
                string firstLinePrefix = $"[{time}] [{_logLevelNames[level]}]";

                for (int i = 0; i < lines.Length; i++)
                {
                    string prefix = i == 0 ? firstLinePrefix : "".PadRight(firstLinePrefix.Length);
                    lines[i] = $"{prefix} {lines[i].Trim()}";
                }

                TryWriteToFile(Path.Combine(LogsDir, $"{day}.txt"), string.Join(Environment.NewLine, lines));
            }
        }

        private static void TryWriteToFile(string path, string text, int delayMs = 20, int retries = 10)
        {
            try
            {
                File.AppendAllLines(path, text.AsList());
            }
            catch
            {
                if (retries < 1)
                {
                    Console.WriteLine($"Failed to write to log file and out of retries! ({path})");
                    return;
                }

                Thread.Sleep(delayMs);
                TryWriteToFile(path, text, delayMs, retries - 1);
            }
        }
    }
}
