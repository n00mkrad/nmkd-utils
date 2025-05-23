

using System.Collections.Frozen;
using System.Text;
using System.Text.RegularExpressions;

namespace NmkdUtils
{
    public class StringUtils
    {
        /// <summary> Returns the longest string that all strings start with (e.g. a common root path of many file paths) </summary>
        public static string FindLongestCommonPrefix(IEnumerable<string> strings)
        {
            var stringsArray = strings is string[]? (string[])strings : strings.ToArray();

            if (stringsArray == null || stringsArray.Length == 0)
                return "";

            // Start by assuming the whole first string is the common prefix
            string prefix = stringsArray[0];

            for (int i = 1; i < stringsArray.Length; i++)
            {
                // Reduce the prefix length until a match is found
                while (stringsArray[i].IndexOf(prefix) != 0)
                {
                    prefix = prefix.Substring(0, prefix.Length - 1);
                    if (prefix == "") return "";
                }
            }

            return prefix;
        }

        /// <summary> Optimized version based on https://github.com/picrap/WildcardMatch </summary>
        public static bool WildcardMatch(string wildcard, ReadOnlySpan<char> s, int wildcardIndex, int sIndex, bool ignoreCase)
        {
            while (true)
            {
                // Check if we are at the end of the wildcard string
                if (wildcardIndex == wildcard.Length)
                    return sIndex == s.Length;

                char c = wildcard[wildcardIndex];
                switch (c)
                {
                    case '?':
                        // Match any single character
                        break;
                    case '*':
                        // If this is the last wildcard char, match any sequence including empty
                        if (wildcardIndex == wildcard.Length - 1)
                            return true;

                        // Try to match the rest of the pattern after the asterisk with any part of the remaining string
                        for (int i = sIndex; i < s.Length; i++)
                        {
                            if (WildcardMatch(wildcard, s.Slice(i), wildcardIndex + 1, 0, ignoreCase))
                                return true;
                        }
                        return false;
                    default:
                        // Check character match taking into account the ignoreCase parameter
                        char wildcardChar = ignoreCase ? char.ToLower(c) : c;
                        if (sIndex == s.Length || (ignoreCase ? char.ToLower(s[sIndex]) : s[sIndex]) != wildcardChar)
                            return false;
                        break;
                }

                // Move to the next character in both strings
                wildcardIndex++;
                sIndex++;
            }
        }

        public static string ReplacePathsWithFilenames(string s)
        {
            // Regular expression to find file paths enclosed in double quotes
            var regex = new Regex("\"[^\"]*\"");

            // Use a MatchEvaluator delegate to replace each match
            return regex.Replace(s, m =>
            {
                // Extract the path from the matched value and get the filename
                string fullPath = m.Value.Trim('"');
                string filename = Path.GetFileName(fullPath);

                // Return the filename enclosed in double quotes
                return $"\"{filename}\"";
            });
        }

        /// <summary>
        /// Converts a string (can be any object if it implements ToString) that is assumed to be PascalCase to snake_case
        /// </summary>
        public static string PascalToSnakeCase(object input)
        {
            string s = $"{input}";

            if (s.IsEmpty())
                return "";

            var regex = new Regex("(?<=[a-z0-9])[A-Z]|(^[A-Z][a-z0-9]+)");
            return regex.Replace(s, m => "_" + m.Value.ToLower()).TrimStart('_');
        }

        public static List<string> GetEnumNamesSnek(Type enumType)
        {
            return Enum.GetNames(enumType).Select(PascalToSnakeCase).ToList();
        }

        public static string PrintEnumsCli(Type enumType, bool withNumbers = true, bool linebreaks = false)
        {
            string delimiter = linebreaks ? "\n" : " ";
            var list = GetEnumNamesSnek(enumType);
            if (withNumbers)
                list = list.Select((s, i) => $"{i}: {s}").ToList();
            return list.Join(delimiter);
        }

        /// <summary>
        /// Parses a string representing a bitrate (e.g. "24m"), case-insensitive, and returns it as kbps
        /// </summary>
        public static int ParseBitrateKbps(string s)
        {
            if (s.IsEmpty())
                return 0;

            s = s.Low().Trim();

            if (s.Last() == 'm')
                return (s.GetFloat() * 1000).RoundToInt();

            return s.GetInt();
        }

        public static List<string> FindWordRepetitions(string input, bool ignoreCase = true, int minLetterCount = 2)
        {
            var matches = Regex.Matches(input, @"\b(\w+)\s+\1\b", ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);

            if (matches.Any())
                return matches.Select(m => m.Groups[1].Value).Where(m => m.Trim().Length >= minLetterCount).ToList();

            return [];
        }

        public static bool FindWordRepetitionsOut(string input, out List<string> repetitions, bool ignoreCase = true, int minLetterCount = 2)
        {
            repetitions = FindWordRepetitions(input, ignoreCase, minLetterCount);
            return repetitions.Any();
        }

        public static Dictionary<string, List<string>> GroupByPrefix(IEnumerable<FileInfo> source, int amountOfBins = 2, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            source = source.OrderBy(s => s.Name, StringComparer.FromComparison(comparison)).ToList();
            Dictionary<string, List<string>> grouped = [];
            List<FileInfo> alreadyGrouped = [];

            foreach (var s in source)
            {
                var name = s.Name;

                if (alreadyGrouped.Contains(s))
                    continue;

                for (int i = 2; i < 1000; i++)
                {
                    string pfx = name.Substring(0, i);
                    var newMatches = source.Where(x => x.Name.StartsWith(pfx, comparison) && !alreadyGrouped.Contains(x)).ToList();

                    if (newMatches.Count == amountOfBins)
                    {
                        alreadyGrouped.AddRange(newMatches);
                        grouped[pfx] = newMatches.Select(f => f.FullName).ToList();
                        Logger.Log($"Grouped {newMatches.Count} items with prefix '{pfx}':\n{newMatches.Join("\n")}\n\n", Logger.Level.Debug);
                        break;
                    }
                }
            }

            return grouped;
        }

        private static string GetLongestPrefix(IReadOnlyList<string> words)
        {
            if (words is null || words.Count == 0)
                return "";

            var prefix = words[0];

            for (var i = 1; i < words.Count && prefix.Length > 0; i++)
            {
                var candidate = words[i];
                var max = Math.Min(prefix.Length, candidate.Length);
                var idx = 0;

                while (idx < max && prefix[idx] == candidate[idx])
                    idx++;

                prefix = prefix[..idx];
            }

            return prefix;
        }

        public static string RemoveTags(string s, bool htmlTags = true, bool curlyBraceTags = true, bool htmlOnlyRemSize = false)
        {
            if (s.IsEmpty())
                return s;

            if (htmlTags && curlyBraceTags)
                return Regex.Replace(s, @"<[^>]+>|{[^}]+}", "", RegexOptions.Compiled);

            if (htmlTags)
                s = Regex.Replace(s, @"<[^>]+>", "", RegexOptions.Compiled);

            if (curlyBraceTags)
                s = Regex.Replace(s, @"\{[^}]*\}", "", RegexOptions.Compiled);

            if (htmlOnlyRemSize)
                return Regex.Replace(s, "size=\"\\d+\"", "", RegexOptions.Compiled);

            return s;
        }

        public static string RemoveEmojis(string s)
        {
            if (s.IsEmpty())
                return s;

            Regex emojiRegex = new Regex(@"(?:[\uD800-\uDBFF][\uDC00-\uDFFF])|\uFE0F", RegexOptions.Compiled);
            return emojiRegex.Replace(s, "");
        }

        public static string[] SplitLongLines(string[] lines, int threshold = 45)
        {
            if (lines == null) throw new ArgumentNullException(nameof(lines));
            if (threshold <= 0) throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be positive.");

            var result = new List<string>();
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line) || line.Length <= (threshold * 1.33333f).RoundToInt())
                {
                    result.Add(line);
                    continue;
                }

                var remaining = line;
                while (remaining.Length > threshold)
                {
                    int splitPos = FindSplitPosition(remaining, threshold);
                    var part = remaining.Substring(0, splitPos).Trim();
                    if (part.Length > 0)
                        result.Add(part);
                    remaining = remaining.Substring(splitPos).Trim();
                }

                if (remaining.Length > 0)
                    result.Add(remaining);
            }

            return result.ToArray();

            int FindSplitPosition(string text, int threshold)
            {
                int limit = Math.Min(text.Length, threshold);
                int mid = text.Length / 2;
                int quarter = text.Length / 4;
                int threeQuarter = 3 * text.Length / 4;
                char[] punctuation = { '.', ',', ';', '!', '?', ':' };

                // Prioritize splitting after punctuation within the middle 50% of the text
                var punctPositions = Enumerable.Range(0, limit).Where(i => punctuation.Contains(text[i]) && i >= quarter && i <= threeQuarter).ToList();
                if (punctPositions.Any())
                {
                    int bestPunct = punctPositions.OrderBy(i => Math.Abs(i - mid)).First();
                    return bestPunct + 1; // include the punctuation
                }

                // Fallback: split at whitespace closest to the middle within the limit
                var spacePositions = Enumerable.Range(0, limit).Where(i => char.IsWhiteSpace(text[i])).ToList();
                if (spacePositions.Any())
                {
                    int bestSpace = spacePositions.OrderBy(i => Math.Abs(i - mid)).First();
                    return bestSpace;
                }

                // Hard split at the midpoint if no suitable whitespace or punctuation
                return mid;
            }
        }

        public static List<string> RemoveMatchingStartEnd(IEnumerable<string> strings)
        {
            int charsToRemoveStart = 0;

            for (int i = 0; i < strings.MinBy(s => s.Length).Length; i++)
            {
                if (strings.All(s => s[i] == strings.First()[i]))
                    charsToRemoveStart++;
                else
                    break;
            }

            int charsToRemoveEnd = 0;

            for (int i = 0; i < strings.MinBy(s => s.Length).Length; i++)
            {
                if (strings.All(s => s[s.Length - 1 - i] == strings.First()[strings.First().Length - 1 - i]))
                    charsToRemoveEnd++;
                else
                    break;
            }

            charsToRemoveStart = (charsToRemoveStart - 3).Clamp(0, 1000);
            //charsToRemoveEnd = (charsToRemoveEnd - 12).Clamp(0, 1000);

            return strings.Select(s => s.Substring(charsToRemoveStart, s.Length - charsToRemoveStart - charsToRemoveEnd)).ToList();
        }

        public static List<List<string>> RemoveIdenticalLines(List<List<string>> entries, string replaceWith = "...", bool pad = true)
        {
            if (entries == null || entries.Count <= 1 || entries.Any(e => e == null))
                return entries;

            var result = entries.Select(e => new List<string>(e)).ToList();
            int minLines = result.Min(e => e.Count);

            for (int i = 0; i < minLines; i++)
            {
                string firstLine = result[0][i];
                bool allSame = result.All(e => e[i] == firstLine);

                if (allSame)
                {
                    for (int j = 0; j < result.Count; j++)
                    {
                        result[j][i] = replaceWith;
                    }
                }
            }

            if (!pad)
                return result;

            // Pad each column so all lines at the same position have equal length
            int maxLines = result.Max(e => e.Count);
            for (int i = 0; i < maxLines; i++)
            {
                int maxLen = result.Where(e => e.Count > i).Max(e => e[i].Length);

                for (int j = 0; j < result.Count; j++)
                {
                    if (result[j].Count > i)
                    {
                        result[j][i] = result[j][i].PadRight(maxLen);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Capitalizes words that have a certain percentage of uppercase letters (<paramref name="threshold"/>) and have at least <paramref name="minLetters"/> letters.
        /// </summary>
        public static string CapitalizeWords(string s, int minLetters = 7, float threshold = 0.6f)
        {
            if (s.IsEmpty())
                return s;

            List<string> NamePrefixes2Ch = ["Mc", "Di", "Le", "La", "Du"];
            List<string> NamePrefixes3Ch = ["Mac"];
            List<string> convertList = [];

            foreach (string word in s.GetWords())
            {
                if(convertList.Contains(word))
                    continue;

                int letterCount = word.Count(char.IsLetter);
                int upperCount = word.Count(char.IsUpper);
                int lowerCount = word.Count(char.IsLower);

                if (letterCount >= minLetters && upperCount < letterCount && ((float)upperCount / letterCount) > threshold)
                {
                    // Edge case: Ignore names with 2-char prefix (1 lowercase char) like McDonald, DiCaprio, etc.
                    if (word.Length >= 5 && lowerCount == 1 && NamePrefixes2Ch.Any(word.StartsWith))
                        continue;

                    // Edge case: Ignore names with 3-char prefix (2 lowercase chars) like MacLeod
                    if (word.Length >= 6 && lowerCount == 2 && NamePrefixes3Ch.Any(word.StartsWith))
                        continue;

                    convertList.Add(word);
                }
            }

            if(convertList.Count == 0)
                return s;

            Logger.Log($"Auto-capitalize: {convertList.Join()}", print: false);
            convertList.ForEach(str => s = s.Replace(str, str.Up())); // Capitalize all words in the list
            return s;
        }
    }
}
