using System.Collections.Concurrent;

namespace NmkdUtils
{
    public class Logger
    {
        public class Entry
        {
            public string Message { get; set; } = "";
            public Logger.Level LogLevel { get; set; } = Level.Info;
            public int ShowTwiceTimeout { get; set; } = 0;
        }


        public static string LogsDir { get; private set; }
        public enum Level { Disabled, Debug, Verbose, Info, Warning, Error }
        public static Level ConsoleLogLevel = Level.Info;
        public static Level FileLogLevel = Level.Info;
        public static bool PrintLogLevel = true;
        public static bool PrintFullLevelNames = false;

        private static BlockingCollection<Entry> _logQueue = new();
        private static Thread _loggingThread;

        private static string _lastLogMessage = string.Empty;
        private static DateTime _lastLogTime = DateTime.MinValue;
        private static readonly object _logLock = new();


        private static readonly Dictionary<Level, ConsoleColor> _logLevelColors = new()
        {
            { Level.Debug, ConsoleColor.DarkBlue },
            { Level.Verbose, ConsoleColor.DarkGray },
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

        public delegate void LogHandler(string message);
        public static LogHandler? OnConsoleWritten;
        public static LogHandler? OnConsoleWrittenWithLvl;

        private static readonly int _maxLogTypeStrLen = Enum.GetNames(typeof(Level)).Max(name => name.Length);

        static Logger()
        {
            LogsDir = PathUtils.GetCommonSubdir(PathUtils.CommonDir.Logs);
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
                WriteLog(entry);
            }
        }

        public static void Print(object o)
        {
            Console.ResetColor();
            Console.WriteLine($"{o}");
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

        public static void Log(object o, Level level = Level.Info, int showTwiceTimeout = 0)
        {
            if (o is Exception)
            {
                Log((Exception)o, "");
                return;
            }

            if (level != Level.Disabled && (int)level >= (int)ConsoleLogLevel || (int)level >= (int)FileLogLevel)
            {
                var logEntry = new Entry { Message = $"{o}", LogLevel = level, ShowTwiceTimeout = showTwiceTimeout };
                _logQueue.Add(logEntry);
            }
        }

        public static void Log(Exception e, string note)
        {
            string trace = e.StackTrace;
            string location = FormatUtils.LastProjectStackItem(trace);
            Log($"{(location.IsEmpty() ? "" : $"[{location}] ")}[{e.GetType()}] {(note.IsEmpty() ? "" : $"{note} - ")}{e.Message}{Environment.NewLine}{FormatUtils.NicerStackTrace(trace)}", Level.Error);
        }

        public static void LogWrn(object o)
        {
            Log(o, Level.Warning);
        }

        public static void LogErr(object o)
        {
            Log(o, Level.Error);
        }

        public static void WriteLog(Entry entry)
        {
            string msg = entry.Message;
            Level level = entry.LogLevel;

            bool shouldLog;

            lock (_logLock)
            {
                shouldLog = !(msg == _lastLogMessage && (DateTime.Now - _lastLogTime).TotalMilliseconds < entry.ShowTwiceTimeout);

                if (shouldLog)
                {
                    _lastLogMessage = msg;
                    _lastLogTime = DateTime.Now;
                }
            }

            if (!shouldLog)
                return;

            _lastLogMessage = msg;
            _lastLogTime = DateTime.Now;

            if ((int)level >= (int)ConsoleLogLevel)
            {
                string msgNoPrefix = msg;
                var lines = msg.SplitIntoLines();
                string firstLinePrefix = PrintFullLevelNames ? $"[{level.ToString().Up().PadRight(_maxLogTypeStrLen, '.')}]" : $"[{_logLevelNames[level]}]";

                for (int i = 0; i < lines.Length; i++)
                {
                    string prefix = i == 0 ? firstLinePrefix : "".PadRight(firstLinePrefix.Length);
                    lines[i] = $"{prefix} {lines[i].Trim()}";
                }

                string output = string.Join(Environment.NewLine, lines);
                Console.ForegroundColor = _logLevelColors[level];
                Console.WriteLine(PrintLogLevel ? output : msgNoPrefix);
                Console.ResetColor();

                OnConsoleWritten?.Invoke(msg);
                OnConsoleWrittenWithLvl?.Invoke(output);
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

        public static void WaitForEmptyQueue()
        {
            Thread.Sleep(1);

            while (_logQueue.Count > 0)
            {
                Thread.Sleep(10);
            }
        }
    }

}
