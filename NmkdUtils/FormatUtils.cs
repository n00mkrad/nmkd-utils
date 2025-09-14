using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NmkdUtils
{
    public class FormatUtils
    {
        /// <summary> Returns readable file size. Calculates using 1024 as base with <paramref name="binaryCalculation"/>, otherwise 1000. If <paramref name="binaryNotation"/> is true, "KiB" instead of "KB" will be output, etc. </summary>
        public static string FileSize(long sizeBytes, bool binaryCalculation = true, bool binaryNotation = false, bool noDecimals = false)
        {
            try
            {
                int mult = binaryCalculation ? 1024 : 1000;
                string[] suf = binaryNotation ? ["B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB"] : ["B", "KB", "MB", "GB", "TB", "PB", "EB"];
                if (sizeBytes == 0) return "0" + suf[0];
                long bytes = Math.Abs(sizeBytes);
                int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, mult)));
                double num = Math.Round(bytes / Math.Pow(mult, place), 2);
                string s = ($"{Math.Sign(sizeBytes) * num} {suf[place]}");
                return noDecimals ? s.Split('.').First() : s;
            }
            catch
            {
                return "? B";
            }
        }

        /// <summary> <inheritdoc cref="Time(double, bool, bool)"/> </summary>
        public static string Time(TimeSpan ts, bool forceDecimals = false, bool noDays = true) => Time(ts.TotalMilliseconds, forceDecimals: forceDecimals, noDays: noDays);

        /// <summary> <inheritdoc cref="Time(double, bool, bool)"/> </summary>
        public static string Time(Stopwatch sw, bool forceDecimals = false, bool noDays = true) => Time(sw.Elapsed.TotalMilliseconds, forceDecimals: forceDecimals, noDays: noDays);

        /// <summary>
        /// Converts a time (in milliseconds) into a string. <paramref name="forceDecimals"/> forces decimals for seconds (e.g. 1.00 instead of 1). <br/>
        /// <paramref name="noDays"/> will limit the output to hours/minutes/seconds in case it's more than 24 hours.
        /// </summary>
        public static string Time(double milliseconds, bool forceDecimals = false, bool noDays = true)
        {
            if (milliseconds < 200)
            {
                return $"{milliseconds:0.#}ms";
            }
            else if (milliseconds < 60000)
            {
                double seconds = milliseconds / 1000.0;
                return forceDecimals ? $"{seconds:F2}s" : $"{seconds:0.##}s";
            }
            else if (milliseconds < 3600000)
            {
                int minutes = (int)(milliseconds / 60000);
                int seconds = (int)((milliseconds % 60000) / 1000);
                return $"{minutes:D2}:{seconds:D2}";
            }
            else if (milliseconds < 86400000 || milliseconds >= 86400000 && noDays)
            {
                int hours = (int)(milliseconds / 3600000);
                int minutes = (int)((milliseconds % 3600000) / 60000);
                int seconds = (int)((milliseconds % 60000) / 1000);
                return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
            }
            else
            {
                int days = (int)(milliseconds / 86400000);
                int hours = (int)((milliseconds % 86400000) / 3600000);
                int minutes = (int)((milliseconds % 3600000) / 60000);
                int seconds = (int)((milliseconds % 60000) / 1000);
                return $"{days:D2}:{hours:D2}:{minutes:D2}:{seconds:D2}";
            }
        }

        public class Media
        {
            public static string Bitrate(int bps, bool space = true)
            {
                string s = space ? " " : "";
                float kbps = bps / 1000;

                if (bps < 10000)
                    return $"{kbps:0.0#}{s}kbps";

                return kbps < 10000 ? $"{kbps:0}{s}kbps" : $"{kbps / 1000:0.0}{s}Mbps";
            }

            /// <summary>
            /// Gets bit depth from pixel format string. If the format is not recognized, it returns the <paramref name="fallback"/> value.
            /// </summary>
            public static int BitDepthFromPixFmt(string pixFmt, int fallback = 8)
            {
                pixFmt = pixFmt.Low();
                if (pixFmt.MatchesWildcard("yuv*p")) return 8;
                if (pixFmt.MatchesWildcard("???24")) return 8; // e.g. rgb24, bgr24
                if (pixFmt.MatchesWildcard("*p10?e")) return 10;
                if (pixFmt.MatchesWildcard("*12?e") || pixFmt.MatchesWildcard("*36?e")) return 12;
                if (pixFmt.MatchesWildcard("*16?e") || pixFmt.MatchesWildcard("*48?e") || pixFmt.MatchesWildcard("*64?e")) return 16;
                if (pixFmt.MatchesWildcard("*32?e") || pixFmt.MatchesWildcard("*96?e") || pixFmt.MatchesWildcard("*128?e")) return 32;
                if (pixFmt.MatchesWildcard("*14?e")) return 14;
                if (pixFmt.MatchesWildcard("*9?e")) return 9;
                if (pixFmt.MatchesWildcard("*5?e")) return 5;
                if (pixFmt.OrderBy(c => c) == "rgba".OrderBy(c => c)) return 8; // Check for any order of rgba
                if (pixFmt.IsOneOf("nv12", "nv16", "ya8")) return 8;
                if (pixFmt.MatchesWildcard("mono?")) return 1; // e.g. monow, monob
                if (pixFmt.IsOneOf("gray", "pal8")) return 8;
                return fallback;
            }

            public enum LayoutStringFormat { Raw, Prettier, Numbers }
            public static string AudioLayout(string ffmpegLayoutName, LayoutStringFormat format = LayoutStringFormat.Raw)
            {
                if (format == LayoutStringFormat.Prettier)
                {
                    string s = ffmpegLayoutName.Replace("(", " (");
                    s = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s);
                    return s;
                }

                if (format == LayoutStringFormat.Numbers)
                    return ffmpegLayoutName.Replace("mono", "1.0").Replace("stereo", "2.0");

                return ffmpegLayoutName.Trim();
            }
        }

        /// <summary> Beautifies the output of ffmpeg's progress stats (Frame, FPS, Bitrate, Size, Time, Speed). If <paramref name="keepOtherText"/> is false, every output that isn't stats will be stripped.  </summary>
        public static string BeautifyFfmpegStats(string s, bool keepOtherText = false)
        {
            if (s.IsEmpty())
                return s;

            string orig = s;
            s = s.SquashSpaces().Replace("= ", "=").Replace("Lsize", "size"); // Squash multiple spaces, remove spaces after equals signs for easier parsing, Lsize => size (what's the difference?)
            string frame = s.Contains("frame=") ? s.Split("frame=").Last().Split(' ').First().Replace("N/A", "") : "";
            string fps = s.Contains(" fps=") ? s.Split(" fps=").Last().Split(' ').First().Replace("N/A", "").Replace("0.0", "") : "";
            string size = s.Contains("size=") ? s.Split("size=").Last().Split(' ').First().Split('K').First().Replace("N/A", "") : "";
            string time = s.Contains(" time=") ? s.Split(" time=").Last().Split(' ').First().Replace("N/A", "") : "";
            string br = s.Contains(" bitrate=") ? s.Split(" bitrate=").Last().Split(' ').First().Split('k').First().Replace("N/A", "") : "";
            string speed = s.Contains(" speed=") ? s.Split(" speed=").Last().Split(' ').First().Split('x').First().Replace("N/A", "") : "";

            if (speed.IsEmpty() && (frame.IsEmpty() || size.IsEmpty()))
                return keepOtherText ? orig : "";

            List<string> values = [];

            if (frame.IsNotEmpty())
                values.Add($"Frame: {frame}");

            if (fps.IsNotEmpty())
                values.Add($"FPS: {fps}");


            if (br.IsNotEmpty())
            {
                float bitrate = br.GetFloat();
                string brStr = bitrate < 2048 ? $"{bitrate:0} kbps" : $"{bitrate / 1024:0.0} Mbps";
                values.Add($"Bitrate: {brStr}");
            }

            if (size.IsNotEmpty())
                values.Add($"Output Size: {(size.GetFloat() / 1024f).ToString("0.00#")} MiB");

            if (time.IsNotEmpty() && !time.StartsWith("-"))
                values.Add($"Time: {time}");

            if (speed.IsNotEmpty())
            {
                if (speed.Contains("e+"))
                {
                    values.Add($"Speed: >999x");
                }
                else
                {
                    float speedVal = speed.GetFloat();
                    string speedStr = speedVal < 1f ? $"{speedVal:0.##}x" : $"{speedVal:0.0}x";
                    values.Add($"Speed: {speedStr}");
                }
            }

            return values.Join(" - ").Trim();
        }

        /// <summary> Returns a string containing the current platform, elevation status, and build timestamp. </summary>
        public static string ProgramInfo(string buildTimestamp)
        {
            string platform = OsUtils.IsWindows ? "[Windows]" : "[Linux/Other]";
            string elevated = OsUtils.IsElevated ? " [Elevated]" : "";
            string buildTime = buildTimestamp.IsEmpty() ? "" : $" [Built {buildTimestamp} UTC]";
            return $"{platform}{elevated}{buildTime}";
        }

        /// <summary> Formats a stack trace to be more readable and compact </summary>
        public static string NicerStackTrace(string trace)
        {
            if (trace.IsEmpty())
                return "";

            var split = trace.GetLines();

            for (int i = 0; i < split.Count; i++)
            {
                split[i] = Regex.Replace(split[i], @"`\d", "");

                if (split[i].StartsWith("   at ") && split[i].MatchesWildcard("* in *.cs:line*"))
                {
                    split[i] = Regex.Replace(split[i], @" in .+\\([^\\]+):line", " in $1 [") + "]";
                }

                string indentation = new string(' ', (i + 1) * 2);
                split[i] = $"{indentation}{split[i]}";
            }

            trace = CleanStackTrace(split.ToList()).Join("\n");
            return trace;
        }

        /// <summary> Removes internal compiler-generated types and local function names from a stack trace. </summary>
        public static List<string> CleanStackTrace(List<string> lines, int removeParamNamesIfLongerThan = 120)
        {
            var cleaned = new List<string>();
            string indent = " ";

            foreach (var line in lines)
            {
                string clean = line.Trim().RegexReplace(Regexes.StackTraceDispClassGarbage); // strip out the generated display-class type
                clean = clean.RegexReplace(Regexes.StackTraceLocalFuncGarbage, "${type}.${method}"); // demangle local function names: "<VlmOcr>g__Sample|6" → "VlmOcr.Sample"

                if (clean.Length > removeParamNamesIfLongerThan)
                {
                    clean = clean.RegexReplace(Regexes.StackTraceLineParamName); // Remove names of parameters, leaving only types, if line too long
                }

                clean = clean.ReplaceAtStart("at", firstOccurenceOnly: true);

                if (clean.Contains("ThrowHelper"))
                    continue;

                if (clean.MatchesWildcard("--- * ---")) // e.g. "--- End of stack trace from previous location ---"
                {
                    indent += "  ";
                    continue; // Skip this line, but increase indentation
                }

                cleaned.Add(indent + clean);
            }

            return cleaned;
        }

        /// <summary> Gets the last stack item that belongs to the current project (not a library). </summary>
        public static string LastProjectStackItem(string trace)
        {
            string appName = Path.GetFileNameWithoutExtension(Environment.ProcessPath);
            var split = trace.GetLines();
            var line = split.Where(s => s.Trim().StartsWith($"   at {appName}.")).ToList();

            if (line.Count < 1)
                return "";

            return line.First().Replace($"   at {appName}.", "").Split('(')[0].Trim();
        }

        public static string Exception(Exception ex, string note = "", bool withTrace = true)
        {
            string trace = ex.StackTrace ?? "";
            string location = trace.IsEmpty() ? "Unknown Location" : LastProjectStackItem(trace);
            var unwrapped = ex.Unwrap(); // Unwraps AggregateException, etc, if necessary

            string locStr = location.IsEmpty() ? "" : $"[{location}] ";
            // string typeStr = unwrapped.Count == 1 ? unwrapped[0].GetType().Name : "AggregateException";
            string traceStr = withTrace ? "\n" + NicerStackTrace(trace) : "";
            string noteStr = note.IsEmpty() ? "" : $"{note} - ";

            if (unwrapped.Count == 1)
            {
                ex = unwrapped[0];
                return $"{locStr}[{ex.GetType()}] {noteStr}{ex.Message.Trunc(330)}{traceStr}";
            }
            else
            {
                return $"{locStr}[{ex.GetType()}] [{unwrapped.Count} Inner] {noteStr}{ex.Message.Trunc(320)}{traceStr}";
            }
        }

        /// <summary> Formats two numbers and the resulting percentage, e.g. "5/20 (25%)" </summary>
        public static string NumsPercent(int part, int total, int zPad = 0, string prcFormat = "0.#")
        {
            if (total == 0)
                return $"{part}/{total}";
            float percent = (part / (float)total) * 100f;
            return $"{part.ZPad(zPad)}/{total.ZPad(zPad)} ({percent.ToString(prcFormat)}%)";
        }

        /// <summary> Count amount of items of any IEnumerable, get current item using IndexOf and return a string like "3/10". </summary>
        public static string IterationProgress<T>(IEnumerable<T> items, T currentItem, bool brackets = false)
        {
            // Check if IENumerable is ICollection to avoid multiple enumeration, then get count
            int total = items is ICollection<T> coll ? coll.Count : items.Count();
            // Check if IEnumerable is IList to avoid ToList(), then get index of current item
            int currentIndex = items is IList<T> list ? list.IndexOf(currentItem) : items.ToList().IndexOf(currentItem);
            string s = $"{currentIndex + 1}/{total}";
            return brackets ? $"[{s}]" : s;
        }
    }
}
