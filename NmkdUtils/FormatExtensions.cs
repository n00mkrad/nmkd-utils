﻿

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
    }
}
