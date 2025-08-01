﻿using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using static NmkdUtils.CodeUtils;

namespace NmkdUtils
{
    public class Logger
    {
        public class Entry
        {
            public string Message = "";
            public Level LogLevel = Level.Info;
            public bool Print = true;
            public bool WriteToFile = true;
            public ConsoleColor? CustomColor = null;
            public int ShowTwiceTimeout = 0;
            public string? ReplaceWildcard = null;
            public string FileSuffix = "";

            public Entry() { }
            public Entry(object message, Level? logLevel = null, bool? printToConsole = null, bool? writeToFile = null)
            {
                Message = $"{message}";
                LogLevel = logLevel ?? LogLevel;
                Print = printToConsole ?? Print;
                WriteToFile = writeToFile ?? WriteToFile;
            }
        }

        public static string LogsDir { get; private set; }
        private static readonly bool _debugger;
        public enum Level { None, Debug, Verbose, Info, Warning, Error, Force }
        public static Level ConsoleLogLevel = Level.Info;
        public static Level FileLogLevel = Level.Info;
        public static bool PrintLogLevel = true;
        public static bool PrintFullLevelNames = false;
        public static List<Level> DisabledLevels = [];

        private static BlockingCollection<Entry> _logQueue = new();
        private static Thread _loggingThread;

        public static string LastLogMsg = "";
        public static string LastLogMsgCon = "";
        public static string LastLogMsgFile = "";
        private static DateTime _lastLogTime = DateTime.MinValue;
        private static readonly object _logLock = new();


        private static readonly Dictionary<Level, ConsoleColor> _logLevelColors = new()
        {
            { Level.Debug, ConsoleColor.DarkBlue },
            { Level.Verbose, ConsoleColor.DarkGray },
            { Level.Info, ConsoleColor.Gray },
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
            Try(() => Console.OutputEncoding = Encoding.UTF8, catchAction: (ex) => Print($"Failed to set console output to UTF-8. {ex.Message}"));
            LogsDir = PathUtils.GetCommonSubdir(PathUtils.CommonDir.Logs);
            _debugger = Debugger.IsAttached;
            _loggingThread = new Thread(new ThreadStart(ProcessLogQueue)) { IsBackground = true };
            _loggingThread.Start();
        }

        /// <summary> Gracefully stop the background logging thread. </summary>
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

        /// <summary> Write <paramref name="o"/> directly to the console without logging. </summary>
        public static void Print(object o)
        {
            Console.ResetColor();
            Console.WriteLine($"{o}");
        }

        /// <summary> Log <paramref name="o"/> if <paramref name="condition"/> is true. </summary>
        public static void LogConditional(object o, bool condition, Level level = Level.Info)
        {
            if (condition)
            {
                Log(o, level);
            }
        }

        /// <summary> Log <paramref name="o"/> if <paramref name="condition"/> evaluates to true. </summary>
        public static void LogConditional(object o, Func<bool> condition, Level level = Level.Info) => Log(o, level, condition: condition);

        /// <summary>
        /// Enqueue a log entry (<paramref name="o"/> will be converted to string) with the provided log <paramref name="level"/>. <br/> <paramref name="showTwiceTimeout"/> (ms) defines the wait before an identical message can be logged again. <br/>
        /// <paramref name="condition"/> can be used to conditionally log the message, <paramref name="print"/> and <paramref name="toFile"/> can override printing to console and writing to the log file, ignoring <paramref name="level"/>. <br/>
        /// <paramref name="customColor"/> can give the log entry a custom color when printed to console (no effect in file). <br/>
        /// </summary>
        public static void Log(object o, Level level = Level.Info, int showTwiceTimeout = 0, string? replaceWildcard = null, Func<bool>? condition = null, bool? print = null, bool? toFile = null, ConsoleColor? customColor = null)
        {
            // Check if this is a Logger.Entry object since it should be enqueued, not turned into a string
            if (o is Entry entry)
            {
                _logQueue.Add(entry);
                return;
            }

            // Return if condition is given and not met
            if (condition != null && !condition())
                return;

            // Redirect to Exception log function if this is an exception object
            if (o is Exception exception)
            {
                Log(exception, "");
                return;
            }

            // Check log importance against current log levels
            bool importantEnough = level == Level.Force || ((int)level >= (int)ConsoleLogLevel || (int)level >= (int)FileLogLevel);

            if (level != Level.None && importantEnough)
            {
                var logEntry = new Entry { Message = $"{o}", LogLevel = level, ShowTwiceTimeout = showTwiceTimeout, ReplaceWildcard = replaceWildcard };
                SetIfNotNull(ref logEntry.Print, print);
                SetIfNotNull(ref logEntry.WriteToFile, toFile);
                SetIfNotNull(ref logEntry.CustomColor, customColor);
                _logQueue.Add(logEntry);
            }
        }

        /// <summary> Log an <see cref="Exception"/> with optional note and stack trace. </summary>
        public static void Log(Exception e, string note = "", bool printTrace = true, bool condition = true)
            => Log(FormatUtils.Exception(e, note, printTrace), Level.Error, condition: () => condition);

        /// <summary> Shortcut for <see cref="Log"/> with <see cref="Level.Warning"/>. </summary>
        public static void LogWrn(object o, Func<bool>? condition = null, bool? print = null, bool? toFile = null)
            => Log(o, Level.Warning, condition: condition, print: print, toFile: toFile);

        /// <summary> Shortcut for <see cref="Log"/> with <see cref="Level.Error"/>. </summary>
        public static void LogErr(object o, Func<bool>? condition = null, bool? print = null, bool? toFile = null)
            => Log(o, Level.Error, condition: condition, print: print, toFile: toFile);

        public static void WriteLog(Entry entry)
        {
            string msg = entry.Message;
            Level level = entry.LogLevel;
            bool shouldLog;

            lock (_logLock)
            {
                bool printOrWrite = entry.Print || entry.WriteToFile; // Don't log if both console printing and file writing is disabled (nothing would happen)
                bool levelEnabled = !DisabledLevels.Contains(level); // Don't log if this log level is disabled
                bool duplicateTooFast = entry.ShowTwiceTimeout > 0 && msg == LastLogMsg && (DateTime.Now - _lastLogTime).TotalMilliseconds < entry.ShowTwiceTimeout; // Don't log if the msg is identical to the previous & ShowTwiceTimeout hasn't passed
                shouldLog = printOrWrite && levelEnabled && !duplicateTooFast;

                if (shouldLog)
                {
                    LastLogMsg = msg;
                    _lastLogTime = DateTime.Now;
                }
            }

            if (!shouldLog)
                return;

            string msgNoPrefix = msg;
            var lines = msg.SplitIntoLines();
            var linesPrint = new List<string>(lines); // Copy lines to a new list for printing

            void PrefixLines(List<string> linesList, string prefix, bool trim = false)
            {
                for (int i = 0; i < linesList.Count; i++)
                {
                    string linePfx = i == 0 ? prefix : "".PadRight(prefix.Length);
                    linesList[i] = trim ? $"{linePfx} {linesList[i].Trim()}" : $"{linePfx} {linesList[i]}";
                }
            }

            if (entry.Print && ConsoleLogLevel != Level.None && (int)level >= (int)ConsoleLogLevel) // Check if message should be printed to console & level is not None & loglevel is high enough
            {
                string firstLinePrefix = PrintFullLevelNames ? $"[{level.ToString().Up().PadRight(_maxLogTypeStrLen, '.')}]" : $"[{_logLevelNames[level]}]";
                PrefixLines(linesPrint, firstLinePrefix, trim: false);
                string output = linesPrint.Join("\n");
                string text = PrintLogLevel ? output : msgNoPrefix;
                Console.ForegroundColor = entry.CustomColor ?? _logLevelColors[level]; // Set custom color if given, otherwise use color based on log level
                bool replace = entry.ReplaceWildcard != null && LastLogMsgCon.MatchesWildcard(entry.ReplaceWildcard) && entry.Message.MatchesWildcard(entry.ReplaceWildcard);
                Debug.WriteLineIf(_debugger, text);
                CliUtils.Write(text, replace, resetColAfter: true);
                LastLogMsgCon = msg;
                OnConsoleWritten?.Invoke(msg);
                OnConsoleWrittenWithLvl?.Invoke(output);
            }

            if (entry.WriteToFile && (int)level >= (int)FileLogLevel) // Check if message should be written to file & if loglevel is high enough
            {
                var now = DateTime.Now;
                var linesFile = new List<string>(lines); // Copy lines to a new list for file logging
                string firstLinePrefix = $"[{now.ToString("yyyy-MM-dd HH:mm:ss")}] [{_logLevelNames[level]}]";
                PrefixLines(linesFile, firstLinePrefix, trim: true);
                entry.FileSuffix.AppendIf("_debug", _debugger);
                TryWriteToFile(Path.Combine(LogsDir, $"{now.ToString("yyyy-MM-dd")}{entry.FileSuffix}.txt"), linesFile.Join(Environment.NewLine));
            }
        }

        private static void TryWriteToFile(string path, string text, int delayMs = 100, int retries = 10)
        {
            for (int i = 0; i < retries; i++)
            {
                if (Try(() => File.AppendAllLines(path, [text]), logEx: false))
                    return;

                Thread.Sleep(i >= 5 ? delayMs : delayMs * 2); // Wait longer if we're retrying a lot
            }

            Console.WriteLine($"Failed to write to log file after {retries} retries! ({path})");
        }

        public static void WaitForEmptyQueue()
        {
            Thread.Sleep(10);

            while (_logQueue.Count > 0)
            {
                Thread.Sleep(10);
            }

            Thread.Sleep(40);
        }
    }
}