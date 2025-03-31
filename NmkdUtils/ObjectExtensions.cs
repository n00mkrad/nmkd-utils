using System.Collections;

namespace NmkdUtils
{
    public static class ObjectExtensions
    {
        /// <summary> Get a value from a dictionary, returns <paramref name="fallback"/> if not found. For strings, the default fallback is an empty string instead of null. </summary>
        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue fallback = default)
        {
            // For string values, use empty string instead of null as default value
            if (typeof(TValue) == typeof(string) && fallback is null)
            {
                fallback = (TValue)(object)"";
            }

            if (dictionary == null)
                return fallback;

            return dictionary.TryGetValue(key, out var value) ? value : fallback;
        }

        // Same as above, but with out parameter
        public static bool Get<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, out TValue value, TValue fallback = default)
        {
            value = dictionary.Get(key, fallback);
            return !value.Equals(fallback);
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
                dict[key] = [value];
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

        /// <summary> Checks if a collection is not null and has at least 1 item </summary>
        public static bool HasItems<T>(this IEnumerable<T> source)
        {
            return source?.Any() ?? false;
        }

        /// <summary>
        /// Splits a string by <paramref name="separator"/> and returns only non-empty results. If <paramref name="input"/> is null, an empty array is returned by default (<paramref name="returnEmptyArrayInsteadOfNull"/>).<br/>
        /// Mainly for CLI parsing of comma-separated values.
        /// </summary>
        public static IEnumerable<string> SplitValues(this string input, char separator = ',', bool returnEmptyArrayInsteadOfNull = true)
        {
            if (input is null)
                return returnEmptyArrayInsteadOfNull ? new string[0] : null;

            return input.Split(separator).Where(s => s.IsNotEmpty());
        }

        public static string ToStringFlexible(this object o, string nullPlaceholder = "N/A", string joinSeparator = ", ", int maxListItems = 10)
        {
            if (o is null)
                return nullPlaceholder;

            if (o is string s)
                return s;

            int maxItemsRecursion =  (maxListItems - 1).Clamp(2);

            if (o is IList list)
            {
                string suffix = list.Count > maxListItems ? ", ..." : "";
                return $"[{string.Join(joinSeparator, list.Cast<object>().Take(maxListItems).Select(item => item.ToStringFlexible()))}{suffix}]";
            }

            var type = o.GetType();

            if (type.IsGenericType)
            {
                var genericTypeDef = type.GetGenericTypeDefinition();

                if (genericTypeDef.FullName.StartsWith("System.Tuple"))
                {
                    var props = type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    var propNames = props.Where(p => p.Name.StartsWith("Item")).Select(p => p.GetValue(o)?.ToStringFlexible(maxListItems: maxItemsRecursion));
                    return $"Tuple[{props.Length}]{{{string.Join(", ", propNames)}}}";
                }
                else if (genericTypeDef.FullName.StartsWith("System.ValueTuple"))
                {
                    var fields = type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    var fieldNames = fields.Select(f => f.GetValue(o)?.ToStringFlexible(maxListItems: maxItemsRecursion));
                    return $"ValueTuple[{fields.Length}]{{{string.Join(", ", fieldNames)}}}";
                }
            }

            return $"{o}";
        }
    }
}
