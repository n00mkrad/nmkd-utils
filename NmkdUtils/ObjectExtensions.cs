using System.Collections;
using System.Runtime.CompilerServices;

namespace NmkdUtils
{
    public static class ObjectExtensions
    {
        #region Collections

        /// <summary> Gets a random item from a list. </summary>
        public static T GetRandomItem<T>(this IList<T> list)
        {
            ArgumentNullException.ThrowIfNull(list);
            if (list.Count == 0) throw new ArgumentException("List cannot be empty.", nameof(list));
            int index = Random.Shared.Next(list.Count);
            return list[index];
        }

        /// <summary> Returns the element at the given <paramref name="index"/>, if there is none, returns <paramref name="fallback"/> </summary>
        public static T At<T>(this IList<T> list, int index, T fallback = default)
        {
            if (list == null || index < 0 || index > (list.Count - 1))
                return fallback;

            return list[index];
        }

        /// <summary> Get a value from a dictionary, returns <paramref name="fallback"/> if not found. For strings, the default fallback is an empty string instead of null. </summary>
        public static string GetStr<TKey, TValue>(this IDictionary<TKey, TValue>? dictionary, TKey key, string fallback = "")
        {
            var result = dictionary.Get(key, (TValue)(object)fallback);
            return result is null ? fallback : $"{result}";
        }

        /// <summary> Get a value from a dictionary, returns <paramref name="fallback"/> if not found. For strings, the default fallback is an empty string instead of null. </summary>
        public static TValue? Get<TKey, TValue>(this IDictionary<TKey, TValue>? dictionary, TKey key, TValue? fallback = default)
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
        public static bool Get<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, out TValue? value, TValue? fallback = default)
        {
            value = dictionary.Get(key, fallback);
            return value is not null && !value.Equals(fallback);
        }

        /// <summary>
        /// Sets a <paramref name="key"/> to a <paramref name="value"/> in dictionary <paramref name="dict"/> if it is not null. If the key already exists, its value is updated.
        /// </summary>
        /// <returns> True if the key already existed, False if it was added, null if the dictionary is null. </returns>
        public static bool? Set<TKey, TValue>(this IDictionary<TKey, TValue>? dict, TKey key, TValue value)
        {
            if (dict == null)
                return null;

            if (dict.ContainsKey(key))
            {
                dict[key] = value;
                return true;
            }

            dict.Add(key, value);
            return false;
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

        public static (List<T> Shuffled, int[] Permutation) ShuffleWithPermutation<T>(this IList<T> list, Random rng)
        {
            int n = list.Count;
            var perm = Enumerable.Range(0, n).ToArray();

            // Fisher–Yates on the index array
            for (int i = n - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (perm[i], perm[j]) = (perm[j], perm[i]);
            }

            // build the shuffled list by mapping through perm
            var shuffled = perm.Select(idx => list[idx]).ToList();
            // Logger.Log($"Shuffled with permutation {perm.Join()}");
            return (shuffled, perm);
        }

        public static List<T> DeShuffle<T>(this IList<T> shuffled, int[] perm)
        {
            if (shuffled.Count != perm.Length)
                throw new ArgumentException("Shuffled count does not equal permutation array");

            var result = new T[shuffled.Count];
            for (int i = 0; i < perm.Length; i++)
            {
                // perm[i] is the original index of shuffled[i]
                result[perm[i]] = shuffled[i];
            }

            return result.ToList();
        }

        #endregion

        #region LINQ Shortcuts & Extensions

        /// <summary> Shortcut for '<paramref name="source"/>.Any() == false' </summary>
        public static bool None<T>(this IEnumerable<T> source) => !source.Any();

        /// <summary> Shortcut for '<paramref name="source"/>.Any() == false' </summary>
        public static bool None<T>(this IEnumerable<T> source, Func<T, bool> predicate) => !source.Any(predicate);

        /// <summary> Checks if a collection is not null and has at least 1 item </summary>
        public static bool HasItems<T>(this IEnumerable<T> source) => source?.Any() ?? false;

        public static TProperty MostCommonBy<T, TProperty>(this List<T> list, Func<T, TProperty> selector)
        {
            if (list == null || list.Count == 0)
                throw new InvalidOperationException("Sequence contains no elements");

            return list.GroupBy(selector).MaxBy(g => g.Count()).Key;
        }

        #endregion

        #region Exceptions

        /// <summary>
        /// Checks if the exception is of one or more specified <paramref name="types"/> or if it contains an inner exception with the type(s).<br/>
        /// If <paramref name="mustHaveAll"/> is true, AggregateExceptions need to contain all <paramref name="types"/> for it to return true.
        /// </summary>
        public static bool IsOrContains(this Exception ex, List<Type> types, bool mustHaveAll = false)
        {
            if (ex == null || types == null || types.Count < 1)
                return false;

            List<Type> list = [ex.GetType()];
            // If ex is an AggregateException, use just the inner exceptions
            if (ex is AggregateException aggEx)
            {
                list = aggEx.InnerExceptions.Select(e => e.GetType()).ToList();
            }

            if (list.Count == 1)
                return types.Contains(list[0]);

            if (mustHaveAll)
                return types.All(t => list.Contains(t));

            return types.Any(t => list.Contains(t));
        }

        /// <summary> Returns the type(s) of the exception or, in the case of an AggregateException, the types of the inner exceptions. </summary>
        public static List<string> GetTypes(this Exception ex)
        {
            if (ex is AggregateException aggEx)
                return aggEx.InnerExceptions.Select(e => e.GetType().ToString()).ToList();

            return [ex.GetType().ToString()];
        }

        #endregion

        #region Misc

        /// <summary> Advanced ToString method that can unwrap collections and tuples. Prints null as <paramref name="nullPlaceholder"/>. Lists are truncated after <paramref name="maxListItems"/> items. </summary>
        public static string ToStringFlexible(this object o, string nullPlaceholder = "N/A", string joinSeparator = ", ", int maxListItems = 10)
        {
            if (o is null)
                return nullPlaceholder;

            if (o is string s)
                return s;

            int maxItemsRec = (maxListItems - 1).Clamp(2);

            if (o is IEnumerable enumerable and not string)
            {
                var items = enumerable.Cast<object>().Take(maxListItems).Select(item => item.ToStringFlexible(maxListItems: maxItemsRec));
                string suffix = enumerable.Cast<object>().Skip(maxListItems).Any() ? ", ..." : "";
                return $"[{string.Join(joinSeparator, items)}{suffix}]";
            }

            if (o is ITuple t)
                return $"Tuple[{t.Length}]{{{string.Join(joinSeparator, Enumerable.Range(0, t.Length).Select(i => t[i]?.ToStringFlexible(maxListItems: maxItemsRec)))}}}";

            return o.ToString();
        }

        #endregion
    }
}
