namespace NmkdUtils
{
    public static class FormatExtensions
    {
        public static string Format (this TimeSpan ts)
        {
            return FormatUtils.Time(ts);
        }
    }
}
