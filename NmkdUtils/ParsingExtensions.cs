

namespace NmkdUtils
{
    public static class ParsingExtensions
    {
        public static T GetEnum<T>(this string value, bool ignoreCase = true, bool removeHyphensUnderscores = false, T? fallback = null) where T : struct
        {
            if (removeHyphensUnderscores)
            {
                value = value.Replace("-", "").Replace("_", "");
            }

            if (Enum.TryParse<T>(value, ignoreCase, out T result))
            {
                return result;
            }

            if (fallback.HasValue)
            {
                return fallback.Value;
            }
            else
            {
                Logger.LogWrn($"Unable to parse '{value}' to enum type '{typeof(T).Name}'.");
                return (T)Enum.GetValues(typeof(T)).GetValue(0);
            }
        }

        public static List<string> GetValues<T>() where T : Enum
        {
            return Enum.GetNames(typeof(T)).ToList();
        }
    }
}