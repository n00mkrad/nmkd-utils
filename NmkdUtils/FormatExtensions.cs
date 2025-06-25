

using System.Diagnostics;

namespace NmkdUtils
{
    public static class FormatExtensions
    {
        /// <summary> Converts a DateTime to a Unix timestamp in whole milliseconds </summary>
        public static long ToUnixMs(this DateTime dt)
        {
            return new DateTimeOffset(dt.ToUniversalTime()).ToUnixTimeMilliseconds();
        }

        public static string AsPercentage(this float value, int decimals = 0, bool addPercent = true)
        {
            string format = "0." + new string('0', decimals);
            string s = (value * 100).ToString(format.Trim('.'));
            return addPercent ? s + "%" : s;
        }

        /// <summary> Get formatted time string from a Stopwatch </summary>
        public static string Format(this Stopwatch sw) => FormatUtils.Time(sw);
        /// <summary> Get formatted time string from a TimeSpan </summary>
        public static string Format(this TimeSpan ts) => FormatUtils.Time(ts);
        /// <summary> Get formatted string from a Size (e.g. "1280x720" unless a different format is specified) </summary>
        public static string Format(this System.Drawing.Size s, string format = "{0}x{1}") => format.Format(s.Width, s.Height);
        /// <summary> <inheritdoc cref="Format(System.Drawing.Size, string)"/> </summary>/>
        public static string Format(this SixLabors.ImageSharp.Size s, string format = "{0}x{1}") => format.Format(s.Width, s.Height);
        /// <summary> Get formatted string from a byte array </summary>
        public static string FormatFileSize(this byte[] bytes, bool binaryNotation = true, bool binaryCalculation = false, bool noDecimals = false) => FormatUtils.FileSize(bytes.Length, binaryCalculation, binaryNotation, noDecimals);
        /// <summary> Get formatted string from a FileInfo </summary>
        public static string FormatFileSize(this FileInfo fi, bool binaryNotation = true, bool binaryCalculation = false, bool noDecimals = false) => FormatUtils.FileSize(fi.Length, binaryCalculation, binaryNotation, noDecimals);
    }
}
