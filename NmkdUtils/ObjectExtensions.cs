

namespace NmkdUtils
{
    public static class ObjectExtensions
    {
        /// <summary> Turns an object into an array of the same type with the object as its only entry </summary>
        public static T[] AsArray<T>(this T obj)
        {
            return new[] { obj };
        }

        /// <summary> Turns an object into a list of the same type with the object as its only entry </summary>
        public static List<T> AsList<T>(this T obj)
        {
            return new List<T> { obj };
        }

        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
        {
            // For string values, use empty string instead of null as default value
            if (typeof(TValue) == typeof(string) && defaultValue is null)
            {
                defaultValue = (TValue)(object)string.Empty;
            }

            if (dictionary == null)
            {
                return defaultValue;
            }

            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }

    }
}
