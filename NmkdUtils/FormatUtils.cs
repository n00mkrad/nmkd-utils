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
                double num = Math.Round(bytes / Math.Pow(mult, place), 1);
                string s = ($"{Math.Sign(sizeBytes) * num} {suf[place]}");
                return noDecimals ? s.Split('.').First() : s;
            }
            catch
            {
                return "? B";
            }
        }

        /// <summary>
        /// Converts a TimeSpan <paramref name="ts"/> into a string. <paramref name="forceDecimals"/> forces decimals for seconds (e.g. 1.00 instead of 1).
        /// <paramref name="noDays"/> will limit the output to hours, minutes and seconds in case it's more than 24 hours.
        /// </summary>
        public static string Time(TimeSpan ts, bool forceDecimals = false, bool noDays = true)
        {
            return Time((long)ts.TotalMilliseconds, forceDecimals: forceDecimals, noDays: noDays);
        }

        public static string Time(long milliseconds, bool forceDecimals = false, bool noDays = true)
        {
            if (milliseconds < 200)
            {
                return $"{milliseconds}ms";
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

            public static int BitDepthFromPixFmt(string pixFmt)
            {
                pixFmt = pixFmt.Low();
                if (pixFmt.MatchesWildcard("yuv*p")) return 8;
                if (pixFmt.MatchesWildcard("*p10?e")) return 10;
                if (pixFmt.MatchesWildcard("*p12?e")) return 12;
                if (pixFmt.MatchesWildcard("*p16?e")) return 16;
                return 0;
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

        public static string BeautifyFfmpegStats(string s)
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
                return orig;

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
                values.Add($"Output Size: {(size.GetFloat() / 1024f).ToString("0.##")} MiB");

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

        public static string ProgramInfo(string buildTimestamp)
        {
            string platform = OsUtils.IsWindows ? "[Windows]" : "[Linux/Other]";
            string elevated = OsUtils.IsElevated ? " [Elevated]" : "";
            string buildTime = buildTimestamp.IsEmpty() ? "" : $" [Built {buildTimestamp} UTC]";
            return $"{platform}{elevated}{buildTime}";
        }

        public static string NicerStackTrace(string trace)
        {
            if (trace.IsEmpty())
                return "";

            var split = trace.SplitIntoLines();

            for (int i = 0; i < split.Length; i++)
            {
                split[i] = Regex.Replace(split[i], @"`\d", "");

                if (split[i].StartsWith("   at ") && split[i].MatchesWildcard("* in *.cs:line*"))
                {
                    split[i] = Regex.Replace(split[i], @" in .+\\([^\\]+):line", " in $1 - Line");
                }

                string indentation = new string(' ', (i + 1) * 2);
                split[i] = $"{indentation}{split[i]}";
            }

            trace = string.Join(Environment.NewLine, split);
            trace = trace.Replace("   at ", "");
            return trace;
        }

        public static string LastProjectStackItem(string trace)
        {
            string appName = Path.GetFileNameWithoutExtension(Environment.ProcessPath);
            var split = trace.SplitIntoLines();
            var line = split.Where(s => s.StartsWith($"   at {appName}.")).ToList();

            if (line.Count != 0)
            {
                return line.First().Replace($"   at {appName}.", "").Split('(')[0].Trim();
            }

            return "";
        }
    }
}
