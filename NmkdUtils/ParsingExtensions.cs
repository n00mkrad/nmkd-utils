

namespace NmkdUtils
{
    public static class ParsingExtensions
    {
        /// <summary>
        /// Gets the enum value from a string. If not found, returns <paramref name="fallback"/> if provided. <paramref name="flexible"/> removes all hyphens and underscores before parsing
        /// </summary>
        public static T GetEnum<T>(this string value, bool ignoreCase = true, bool flexible = false, T? fallback = null) where T : struct
        {
            if (flexible)
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