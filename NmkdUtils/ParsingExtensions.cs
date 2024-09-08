

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

            Logger.LogWrn($"Unable to parse '{value}' to enum type '{typeof(T).Name}'.{(fallback.HasValue ? $" Defaulting to {fallback}." : "")}");

            if (fallback.HasValue)
            {
                return fallback.Value;
            }
            else
            {
                return (T)Enum.GetValues(typeof(T)).GetValue(0);
            }
        }

        public static List<string> GetValues<T>() where T : Enum
        {
            return Enum.GetNames(typeof(T)).ToList();
        }
    }
}