

namespace NmkdUtils
{
    public static class FormatExtensions
    {
        public static string GetElapsedStr(this System.Diagnostics.Stopwatch sw)
        {
            return FormatUtils.Time(sw.Elapsed);
        }

        public static string Format (this TimeSpan ts)
        {
            return FormatUtils.Time(ts);
        }

        /// <summary> Converts a DateTime to a Unix timestamp in whole milliseconds </summary>
        public static long ToUnixMs(this DateTime dt)
        {
            return new DateTimeOffset(dt.ToUniversalTime()).ToUnixTimeMilliseconds();
        }
    }
}
