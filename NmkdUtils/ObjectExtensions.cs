using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            if (dictionary == null)
            {
                return defaultValue;
            }

            TValue result = dictionary.TryGetValue(key, out var value) ? value : defaultValue;

            // For string values, return empty string instead of null as default value
            if (result == null && typeof(TValue) == typeof(string))
            {
                return (TValue)(object)string.Empty;
            }

            return result;
        }

    }
}
