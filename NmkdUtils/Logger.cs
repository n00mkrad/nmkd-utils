using System.Diagnostics;
using System.Text;
using System.Threading.Channels;
using static NmkdUtils.CodeUtils;

namespace NmkdUtils
{
    public static class Logger
    {
        public class Entry
        {
            public string Message = "";
            public Level LogLevel = Level.Info;
            public bool Print = true;
            public bool WriteToFile = true;
            public bool PrintWithBreak = true;
            public ConsoleColor? CustomColor = null;
            public int ShowTwiceTimeout = 0;
            public string? ReplaceWildcard = null;
            public string FileSuffix = "";

            public Entry() { }
            public Entry(object msg, Level? level = null, bool? print = null, bool? toFile = null, string? suffix = null)
            {
                Message = $"{msg}";
                LogLevel = level ?? LogLevel;
                Print = print ?? Print;
                WriteToFile = toFile ?? WriteToFile;
                FileSuffix = suffix ?? FileSuffix;
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

        private static readonly Channel<Entry> _channel;
        private static readonly Task _loggingTask;
        private static volatile bool _stopping;
        private static int _pendingCount; // in-flight items
        public static long DroppedCount;  // if writes are attempted after stop

        public static string LastLogMsg = "";
        public static string LastLogMsgCon = "";
        public static string LastLogMsgFile = "";
        private static long _lastLogTicks;
        private static readonly object _logLock = new();


        private static readonly Dictionary<Level, ConsoleColor> _logLevelColors = new()
        {
            { Level.Debug, ConsoleColor.DarkBlue },
            { Level.Verbose, ConsoleColor.DarkGray },
            { Level.Info, ConsoleColor.Gray },
            { Level.Warning, ConsoleColor.Yellow },
            { Level.Error, ConsoleColor.Red },
            { Level.Force, ConsoleColor.White },
        };

        private static readonly Dictionary<Level, string> _logLevelNames = new()
        {
            { Level.Debug, "DBG" },
            { Level.Verbose, "VRB" },
            { Level.Info, "INF" },
            { Level.Warning, "WRN" },
            { Level.Error, "ERR" },
            { Level.Force, "FRC" },
        };

        public static event Action<string>? ConsoleWrittenMsg;
        public static event Action<string>? ConsoleWrittenFull;

        private static readonly int _maxLogTypeStrLen = Enum.GetNames(typeof(Level)).Max(name => name.Length);
        private static readonly string _sessionDate = $"{DateTime.Now:yyyy-MM-dd}"; // Store session date once for log file naming

        static Logger()
        {
            _debugger = Debugger.IsAttached;
            Try(() => Console.OutputEncoding = Encoding.UTF8, catchAction: (ex) => Console.WriteLine($"Failed to set console output to UTF-8. {ex.Message}"));
            LogsDir = PathUtils.GetCommonSubdir(PathUtils.CommonDir.Logs);
            Directory.CreateDirectory(LogsDir);
            var chanOpts = new UnboundedChannelOptions { SingleReader = true, SingleWriter = false, AllowSynchronousContinuations = true }; // Unbounded, single reader, many writers; fastest and non-blocking for producers.
            _channel = Channel.CreateUnbounded<Entry>(chanOpts);
            _loggingTask = Task.Run(ProcessLogQueueAsync);
        }

        /// <summary> Gracefully stop the logger. Complete writer so reader drains and exits cleanly. </summary>
        public static void StopLogging()
        {
            _stopping = true;
            _channel.Writer.TryComplete();
            Try(_loggingTask.Wait, logEx: false);
        }

        private static async Task ProcessLogQueueAsync()
        {
            var reader = _channel.Reader;
            while (await reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (reader.TryRead(out var entry))
                {
                    Try(() => WriteLog(entry));
                    Interlocked.Decrement(ref _pendingCount);
                }
            }
            while (reader.TryRead(out var leftover)) // Drain any remaining items just in case
            {
                Try(() => WriteLog(leftover));
                Interlocked.Decrement(ref _pendingCount);
            }
        }


        /// <summary>
        /// Enqueue a log entry (<paramref name="o"/> will be converted to string) with the provided log <paramref name="level"/>. <br/> <paramref name="showTwiceTimeout"/> (ms) defines the wait before an identical message can be logged again. <br/>
        /// <paramref name="condition"/> can be used to conditionally log the message, <paramref name="print"/> and <paramref name="toFile"/> can override printing to console and writing to the log file, ignoring <paramref name="level"/>. <br/>
        /// <paramref name="color"/> can give the log entry a custom color when printed to console (no effect in file). <br/>
        /// </summary>
        public static void Log(object o, Level level = Level.Info, int showTwiceTimeout = 0, string? replaceWildcard = null, Func<bool>? condition = null, bool? print = null, bool? toFile = null, ConsoleColor? color = null, string? suffix = null)
        {
            // Check if this is a log Entry object since it should be enqueued, not turned into a string
            if (o is Entry e)
            {
                if (ShouldEnqueue(e) && _channel.Writer.TryWrite(e))
                {
                    Interlocked.Increment(ref _pendingCount);
                }

                return;
            }

            // Return if condition is given and not met
            if (condition != null && !condition())
                return;

            // Redirect to Exception log function if this is an exception object
            if (o is Exception ex)
            {
                Log(ex, "");
                return;
            }

            var entry = new Entry($"{o}", level) { ShowTwiceTimeout = showTwiceTimeout, ReplaceWildcard = replaceWildcard };
            SetIfNotNull(ref entry.Print, print);
            SetIfNotNull(ref entry.WriteToFile, toFile);
            SetIfNotNull(ref entry.CustomColor, color);
            SetIfNotNull(ref entry.FileSuffix, suffix);

            // Enqueue only if there is at least one target
            if (!ShouldEnqueue(entry) || !_channel.Writer.TryWrite(entry))
                return;

            Interlocked.Increment(ref _pendingCount);
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

        private enum Target { Console, File }

        // pick targets for this entry
        private static List<Target> GetLogTargets(Entry e)
        {
            var result = new List<Target>(2);

            // Force bypasses disable list
            if (e.LogLevel != Level.Force && DisabledLevels.Contains(e.LogLevel))
                return result;

            bool console = e.Print && ConsoleLogLevel != Level.None && (e.LogLevel == Level.Force || (int)e.LogLevel >= (int)ConsoleLogLevel);
            result.AddIf(Target.Console, console);
            bool file = e.WriteToFile && (e.LogLevel == Level.Force || (int)e.LogLevel >= (int)FileLogLevel);
            result.AddIf(Target.File, file);
            return result;
        }

        private static bool ShouldEnqueue(Entry e) => GetLogTargets(e).Count > 0 && !_stopping;

        private static bool IsDuplicateSuppressed(string msg, int timeoutMs, long nowTicks)
        {
            if (timeoutMs <= 0)
                return false;

            lock (_logLock)
            {
                double ms = (nowTicks - _lastLogTicks) * 1000.0 / Stopwatch.Frequency;
                if (msg == LastLogMsg && ms < timeoutMs)
                    return true;

                LastLogMsg = msg;
                _lastLogTicks = nowTicks;
                return false;
            }
        }

        public static void WriteLog(Entry entry)
        {
            string msg = entry.Message;
            Level level = entry.LogLevel;

            // Decide targets
            var targets = GetLogTargets(entry);
            if (targets.Count == 0)
                return;

            // Duplicate suppression
            if (IsDuplicateSuppressed(msg, entry.ShowTwiceTimeout, Stopwatch.GetTimestamp()))
                return;

            string msgNoPrefix = msg;
            var lines = msg.GetLines();
            var linesPrint = new List<string>(lines); // Copy lines to a new list for printing

            void PrefixLines(List<string> linesList, string prefix, bool trim = false)
            {
                for (int i = 0; i < linesList.Count; i++)
                {
                    string linePfx = i == 0 ? prefix : "".PadRight(prefix.Length);
                    linesList[i] = trim ? $"{linePfx} {linesList[i].Trim()}" : $"{linePfx} {linesList[i]}";
                }
            }

            if (targets.Contains(Target.Console))
            {
                string firstLinePrefix = PrintFullLevelNames ? $"[{level.ToString().Up().PadRight(_maxLogTypeStrLen, '.')}]" : $"[{_logLevelNames[level]}]";
                PrefixLines(linesPrint, firstLinePrefix, trim: false);
                string output = linesPrint.Join("\n");
                string text = PrintLogLevel ? output : msgNoPrefix;
                var prev = Console.ForegroundColor;
                Try(() =>
                {
                    Console.ForegroundColor = entry.CustomColor ?? _logLevelColors[level];
                    bool replace = entry.ReplaceWildcard != null && LastLogMsgCon.MatchesWildcard(entry.ReplaceWildcard) && entry.Message.MatchesWildcard(entry.ReplaceWildcard);
                    Debug.WriteLineIf(_debugger, text);
                    CliUtils.Write(text, replace, resetColAfter: true, newLine: entry.PrintWithBreak);
                });
                Console.ForegroundColor = prev;
                LastLogMsgCon = msg;
                ConsoleWrittenMsg?.Invoke(msg);
                ConsoleWrittenFull?.Invoke(output);
            }

            if (targets.Contains(Target.File))
            {
                var linesFile = new List<string>(lines);
                string firstLinePrefix = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{_logLevelNames[level]}]";
                PrefixLines(linesFile, firstLinePrefix, trim: true);
                bool writeToMainAndCustom = entry.FileSuffix.EndsWith('+'); // If suffix ends with +, write to both main log and custom (suffixed) file
                entry.FileSuffix = entry.FileSuffix.TrimEnd('+'); // Remove + if present
                string dbg = _debugger ? "_debug" : "";

                if (writeToMainAndCustom)
                {
                    TryWriteToFile(Path.Combine(LogsDir, $"{_sessionDate}{dbg}.txt"), linesFile.Join(Environment.NewLine)); // Write to main log
                }

                TryWriteToFile(Path.Combine(LogsDir, $"{_sessionDate}{entry.FileSuffix}{dbg}.txt"), linesFile.Join(Environment.NewLine)); // Write to custom log (or main if suffix empty)
            }
        }

        private static void TryWriteToFile(string path, string text, int delayMs = 100, int retries = 10)
        {
            for (int i = 0; i < retries; i++)
            {
                if (Try(() => File.AppendAllText(path, text + Environment.NewLine), logEx: false))
                    return;

                Thread.Sleep(i <= 5 ? delayMs : delayMs * 2); // Wait longer if we're retrying a lot
            }

            Console.WriteLine($"Failed to write to log file after {retries} retries! ({path})");
        }

        public static void WaitForEmptyQueue()
        {
            if(Volatile.Read(ref _pendingCount) == 0)
                return;

            // brief yield to let consumer start
            Thread.Sleep(5);
            var sw = new SpinWait();
            while (Volatile.Read(ref _pendingCount) > 0)
            {
                sw.SpinOnce();
                if (sw.NextSpinWillYield) Thread.Sleep(5);
            }
            Thread.Sleep(5);
        }
    }
}