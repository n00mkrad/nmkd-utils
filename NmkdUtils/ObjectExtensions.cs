﻿

using System.Collections;
using System.Diagnostics;
using System.Text;

namespace NmkdUtils
{
    public static class ObjectExtensions
    {
        /// <summary> Turns an object into an array of the same type with the object as its sole entry </summary>
        public static T[] AsArray<T>(this T obj)
        {
            return new[] { obj };
        }

        /// <summary> Turns an object into a list of the same type with the object as its sole entry </summary>
        public static List<T> AsList<T>(this T obj)
        {
            return new List<T> { obj };
        }

        /// <summary> Get a value from a dictionary, returns <paramref name="fallback"/> if not found. For strings, the default fallback is an empty string instead of null. </summary>
        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue fallback = default)
        {
            // For string values, use empty string instead of null as default value
            if (typeof(TValue) == typeof(string) && fallback is null)
            {
                fallback = (TValue)(object)string.Empty;
            }

            if (dictionary == null)
            {
                return fallback;
            }

            return dictionary.TryGetValue(key, out var value) ? value : fallback;
        }

        /// <summary> Adds a <paramref name="value"/> to a dictionary of lists; creates a new list if there is none yet. </summary>
        public static void AddToList<TKey, TValue>(this Dictionary<TKey, List<TValue>> dict, TKey key, TValue value)
        {
            // Check if the dictionary already contains the key
            if (dict.ContainsKey(key))
            {
                // Add the value to the existing list
                dict[key].Add(value);
            }
            else
            {
                // Create a new list, add the value and add this list to the dictionary
                dict[key] = new List<TValue> { value };
            }
        }

        /// <summary> Shortcut for '<paramref name="source"/>.Any() == false' </summary>
        public static bool None<T>(this IEnumerable<T> source)
        {
            return !source.Any();
        }

        /// <summary> Shortcut for '<paramref name="source"/>.Any() == false' </summary>
        public static bool None<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            return !source.Any(predicate);
        }

        public static string ToStringFlexible(this object o, string nullPlaceholder = "N/A", string joinSeparator = ", ")
        {
            if (o is null)
                return nullPlaceholder;

            if (o is IList list)
                return $"[{string.Join(joinSeparator, list.Cast<object>().Select(o => o.ToStringFlexible()))}]"; // List elements

            Type type = o.GetType();

            if (type.IsGenericType && type.FullName != null && type.FullName.StartsWith("System.Tuple"))
            {
                var propNames = type.GetProperties().Where(p => p.Name.StartsWith("Item")).Select(x => x.GetValue(o)?.ToStringFlexible());
                return $"Tuple[{propNames.Count()}]{{{string.Join(", ", propNames)}}}";
            }

            return $"{o}";
        }
    }
}
