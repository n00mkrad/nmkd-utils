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

        /// <summary> Shortcut for ElementAtOrDefault with support for default/fallback value </summary>
        public static T At<T>(this IEnumerable<T> source, int index, T fallback = default)
        {
            var result = source.ElementAtOrDefault(index);
            return result is null ? fallback : result;
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

        /// <summary> Add <paramref name="item"/> to list if <paramref name="condition"/> is true </summary>
        public static void AddIf<T>(this IList<T>? list, T? item, bool condition = true)
        {
            if (list == null || !condition || item == null)
                return;

            list.Add(item);
        }

        /// <summary> Add <paramref name="item"/> to list if it is not already in that list </summary>
        public static void AddIfNotContains<T>(this IList<T>? list, T item) => list?.AddIf(item, !list.Contains(item));

        /// <summary> <inheritdoc cref="AddIf{T}(IList{T}?, T, bool)"/> </summary>
        public static void AddIf<T>(this IList<T>? list, T item, Func<bool> condition) => list.AddIf(item, condition());

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

        /// <summary> Gets the single most common item using a <paramref name="selector"/> </summary>
        public static T MostCommonBy<T, TProperty>(this IEnumerable<T> list, Func<T, TProperty> selector)
            => list.MostCommonGroupBy(selector).FirstOrDefault();

        /// <summary> Gets a list of most common items using a <paramref name="selector"/> </summary>
        public static IEnumerable<T> MostCommonGroupBy<T, TProperty>(this IEnumerable<T> list, Func<T, TProperty> selector)
        {
            if (list == null || !list.Any())
                return [];

            return list.GroupBy(selector).MaxBy(g => g.Count())!;
        }

        /// <summary> Order a list by how common each item is (first = most common), preserving the original order within each group. </summary>
        public static IEnumerable<T> OrderByFreq<T>(this IEnumerable<T> list, out int mostCommonCount)
        {
            mostCommonCount = 0;
            if (list == null || !list.Any())
                return [];

            var counts = list.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count()); // Build a dictionary of counts
            mostCommonCount = counts.Values.Max();
            return list.OrderByDescending(x => counts[x]); // Stable sort original sequence by descending count
        }
        /// <inheritdoc cref="OrderByFreq{T}(IEnumerable{T}, out int)"/>
        public static IEnumerable<T> OrderByFreq<T>(this IEnumerable<T> list)
            => OrderByFreq(list, out _);

        public static List<object> ToObjectList(this IEnumerable source) => source.Cast<object>().ToList();

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

        /// <summary> Returns a List of exceptions in case it's an AggregateException, otherwise just returns the single exception. </summary>
        public static List<Exception> Unwrap(this Exception ex)
        {
            if (ex is AggregateException aggregate)
                return aggregate.Flatten().InnerExceptions.ToList();

            return [ex];
        }

        public static List<string> GetMessages(this Exception ex)
        {
            List<string> messages = [];
            var exs = ex.Unwrap();

            foreach(var e in exs)
            {
                string msg = e.Message.TrimEnd('.');

                if (e.InnerException == null)
                {
                    messages.Add(msg);
                    continue;
                }

                msg = msg.Replace(" inner exception", " inner");
                string inner = e.InnerException.Message.TrimEnd('.');
                messages.Add(msg.ContainsCi(inner) ? msg : $"{msg} -> {inner}");
            }

            return messages;
        }

        public static void Log(this Exception ex, bool withTrace = true)
        {
            foreach (var e in ex.Unwrap())
            {
                Logger.Log(e, printTrace: withTrace);
            }
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

        public static void Log(this System.Diagnostics.Stopwatch sw, string note = "", bool reset = false)
        {
            Logger.Log($"{note} {sw.Format()}".Trim());

            if (reset)
                sw.Restart();
        }

        /// <summary> Shortcut for Parallel.ForEach with threads parameter </summary>
        public static void ParallelForEach<T>(this IEnumerable<T> source, Action<T> action, int threads = 0)
        {
            if (threads <= 0)
                threads = Environment.ProcessorCount;
            Parallel.ForEach(source, new ParallelOptions { MaxDegreeOfParallelism = threads }, action);
        }

        /// <summary> Cancels and disposes a <see cref="CancellationTokenSource"/> safely, ignoring any <see cref="ObjectDisposedException"/> that may occur. </summary>
        public static void CancelAndDispose(this CancellationTokenSource? cts)
        {
            if (cts == null)
                return;

            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException) { /* Ignore */ }
            finally
            {
                cts.Dispose();
            }
        }

        #endregion
    }
}
