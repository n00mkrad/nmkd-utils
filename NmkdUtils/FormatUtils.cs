
namespace NmkdUtils
{
    public class FormatUtils
    {
        /// <summary> Returns readable file size. Calculates using 1024 as base with <paramref name="binaryCalculation"/>, otherwise 1000. If <paramref name="binaryNotation"/> is true, "KiB" instead of "KB" will be output, etc. </summary>
        public static string FileSize(long sizeBytes, bool binaryCalculation = true, bool binaryNotation = false)
        {
            try
            {
                int mult = binaryCalculation ? 1024 : 1000;
                string[] suf = binaryNotation ? ["B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB"] : ["B", "KB", "MB", "GB", "TB", "PB", "EB"];
                if (sizeBytes == 0) return "0" + suf[0];
                long bytes = Math.Abs(sizeBytes);
                int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, mult)));
                double num = Math.Round(bytes / Math.Pow(mult, place), 1);
                return ($"{Math.Sign(sizeBytes) * num} {suf[place]}");
            }
            catch
            {
                return "? B";
            }
        }

        public static string Time(TimeSpan ts, bool forceDecimals = false)
        {
            return Time((long)ts.TotalMilliseconds, forceDecimals: forceDecimals);
        }

        public static string Time(long milliseconds, bool forceDecimals = false)
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
            else if (milliseconds < 86400000)
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
            public static int BitDepthFromPixFmt(string pixFmt)
            {
                pixFmt = pixFmt.Low();
                if (pixFmt.MatchesWildcard("yuv*p")) return 8;
                if (pixFmt.MatchesWildcard("*p10?e")) return 10;
                if (pixFmt.MatchesWildcard("*p12?e")) return 12;
                if (pixFmt.MatchesWildcard("*p16?e")) return 16;
                return 0;
            }
        }
    }
}
