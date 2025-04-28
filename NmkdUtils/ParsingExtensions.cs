

using Newtonsoft.Json.Linq;

namespace NmkdUtils
{
    public static class ParsingExtensions
    {
        /// <summary>
        /// Gets the enum value from a string. If not found, returns <paramref name="fallback"/> if provided. <paramref name="flexible"/> removes all hyphens and underscores before parsing (e.g. for passing snake_case).
        /// </summary>
        public static T GetEnum<T>(this string value, bool ignoreCase = true, bool flexible = false, T? fallback = null, bool log = false) where T : struct
        {
            if (flexible)
            {
                value = value.Replace("-", "").Replace("_", "");
            }

            if (Enum.TryParse(value, ignoreCase, out T result))
                return result;

            if (log)
            {
                Logger.LogWrn($"Unable to parse '{value}' to enum type '{typeof(T).Name}'.{(fallback.HasValue ? $" Defaulting to {fallback}." : "")}");
            }

            if (fallback.HasValue)
                return fallback.Value;

            return (T)Enum.GetValues(typeof(T)).GetValue(0);
        }

        public static T GetEnumCli<T>(this object value, T? fallback = null, bool log = false) where T : struct
        {
            if (value is int i)
                return GetEnum(i, fallback, log);

            return GetEnum($"{value}", ignoreCase: true, flexible: true, fallback, log);
        }

        public static T GetEnum<T>(this int value, T? fallback = null, bool log = false) where T : struct
        {
            if (Enum.IsDefined(typeof(T), value))
                return (T)Enum.ToObject(typeof(T), value);

            if (log)
            {
                Logger.LogWrn($"Unable to parse '{value}' to enum type '{typeof(T).Name}'.{(fallback.HasValue ? $" Defaulting to {fallback}." : "")}");
            }

            if (fallback.HasValue)
                return fallback.Value;

            return (T)Enum.GetValues(typeof(T)).GetValue(0);
        }

        public static List<string> GetValues<T>() where T : Enum
        {
            return Enum.GetNames(typeof(T)).ToList();
        }

        public static bool TryParseToJObject (this string json, out JObject jo, bool printErr = true, bool printTextWithErr = false)
        {
            try
            {
                jo = JObject.Parse(json);
                return true;
            }
            catch (Exception ex)
            {
                if(printErr)
                {
                    Logger.LogErr($"Failed to parse JSON: {ex.Message}{(printTextWithErr ? $"\n{json}" : "")}");
                }
                jo = new JObject();
                return false;
            }
        }
    }
}