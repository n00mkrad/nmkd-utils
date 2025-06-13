

using System.Collections.Frozen;
using System.Text;
using System.Text.RegularExpressions;

namespace NmkdUtils
{
    public class StringUtils
    {
        /// <summary> Returns the longest string that all strings start with (e.g. a common root path of many file paths). <br/> Case-insensitive if <paramref name="ci"/> is true. </summary>
        public static string FindLongestCommonPrefix(IEnumerable<string> strings, bool ci = false)
        {
            var stringsArray = strings is string[]? (string[])strings : strings.ToArray();

            if (stringsArray == null || stringsArray.Length == 0)
                return "";

            var stringComp = ci ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            // Start by assuming the whole first string is the common prefix
            string prefix = stringsArray[0];

            for (int i = 1; i < stringsArray.Length; i++)
            {
                while (!stringsArray[i].StartsWith(prefix, stringComp))
                {
                    prefix = prefix.Substring(0, prefix.Length - 1);
                    if (prefix == "")
                        return "";
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

            Regex emojiRegex = new Regex(@"(?:(\ud83c[\udde6-\uddff]){2}|([\#\*0-9]\u20e3)|(?:\u00a9|\u00ae|[\u2000-\u3300]|[\ud83c-\ud83e][\ud000-\udfff])(?:(?:\ud83c[\udffb-\udfff])?(?:\ud83e[\uddb0-\uddb3])?(?:\ufe0f?\u200d(?:[\u2000-\u3300]|[\ud83c-\ud83e][\ud000-\udfff])\ufe0f?)?)*)+", RegexOptions.Compiled);
            return emojiRegex.Replace(s, "");
        }

        /// <summary>
        /// Breaks long lines based on <paramref name="targetLength"/>. The hard limit is <paramref name="targetLength"/> * <paramref name="tolerance"/>. <br/>
        /// <paramref name="windowFraction"/> controls how “wide” the prioritized split zone around the exact middle is (e.g. 0.25 = 50% of the total length).
        /// </summary>
        public static string[] SplitLongLines(string[] lines, int targetLength = 45, float tolerance = 1.25f, double windowFraction = 0.2)
        {
            if (lines == null)
                return lines;

            if (windowFraction <= 0 || windowFraction >= 0.5)
                throw new ArgumentOutOfRangeException(nameof(windowFraction), "windowFraction must be > 0 and < 0.5");

            var result = new List<string>();
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line) || line.Length <= (tolerance * tolerance).RoundToInt())
                {
                    result.Add(line);
                    continue;
                }

                var remaining = line.Replace("...", "…"); // Replace ellipsis with a single character for splitting
                while (remaining.Length > targetLength)
                {
                    int splitPos = FindSplitPosition(remaining, targetLength, windowFraction);
                    var part = remaining.Substring(0, splitPos).Trim();
                    if (part.Length > 0)
                        result.Add(part);
                    remaining = remaining.Substring(splitPos).Trim();
                }

                if (remaining.Length > 0)
                    result.Add(remaining);
            }

            return result.Select(r => r.Replace("…", "...")).ToArray(); // Undo ellipsis replacement and return the result

            int FindSplitPosition(string text, int threshold, double wf)
            {
                int limit = Math.Min(text.Length, threshold);
                int mid = text.Length / 2;
                int lower = Math.Max(0, (int)(mid - wf * text.Length));
                int upper = Math.Min(limit, (int)(mid + wf * text.Length));
                char[] punctuation = { '.', ',', ';', '!', '?', ':', '"', '…' };

                // 1. punctuation in [lower..upper], not followed by letter/digit
                var punctPositions = Enumerable.Range(lower, upper - lower)
                    .Where(i =>
                    {
                        if (!punctuation.Contains(text[i])) return false;
                        if (i + 1 < text.Length && char.IsLetterOrDigit(text[i + 1])) return false;
                        return true;
                    })
                    .ToList();
                if (punctPositions.Any())
                {
                    int best = punctPositions.OrderBy(i => Math.Abs(i - mid)).First();
                    return best + 1;
                }

                // 2. whitespace in [0..limit]
                var spacePositions = Enumerable.Range(0, limit).Where(i => char.IsWhiteSpace(text[i])).ToList();
                if (spacePositions.Any())
                {
                    int best = spacePositions.OrderBy(i => Math.Abs(i - mid)).First();
                    return best;
                }

                // 3. fallback to midpoint
                return mid;
            }
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
                if (convertList.Contains(word))
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

            if (convertList.Count == 0)
                return s;

            Logger.Log($"Auto-capitalize: {convertList.Join()}", print: false);
            convertList.ForEach(str => s = s.Replace(str, str.Up())); // Capitalize all words in the list
            return s;
        }

        /// <summary>
        /// Checks a string for repeated words and limits the number of repetitions to <paramref name="maxRepetitions"/> while trying to preserve punctuation. <br/>
        /// </summary>
        public static List<string> LimitWordReps(IEnumerable<string> lines, int maxRepetitions = 4)
        {
            var original = lines.Join(" ⏎ ").Trim();
            var normalizedLines = new List<string>();
            foreach (var line in lines)
            {
                var tokens = line.Split(' ');
                var resultTokens = new List<string>();
                string lastCore = null;
                int count = 0;

                foreach (var token in tokens)
                {
                    var (core, punct) = SplitToken(token);

                    // if it's just punctuation (no “word” inside), always emit it and reset
                    if (string.IsNullOrEmpty(core))
                    {
                        resultTokens.Add(token);
                        lastCore = null;
                        count = 0;
                        continue;
                    }

                    var normalizedCore = core.ToLowerInvariant();
                    if (lastCore != null && normalizedCore == lastCore)
                    {
                        count++;
                        if (count <= maxRepetitions)
                        {
                            resultTokens.Add(core + punct);
                        }
                        else if (count == maxRepetitions + 1)
                        {
                            // ensure the last kept copy carries the current punctuation
                            int idx = resultTokens.Count - 1;
                            resultTokens[idx] = core + punct;
                        }
                        // beyond that, skip
                    }
                    else
                    {
                        lastCore = normalizedCore;
                        count = 1;
                        resultTokens.Add(core + punct);
                    }
                }

                normalizedLines.Add(string.Join(" ", resultTokens));
            }

            if (original != normalizedLines.Join(" ⏎ ").Trim())
                Logger.Log($"Deduped words:\n{original}\n{normalizedLines.Join(" ⏎ ").Trim()}");

            return normalizedLines;

            (string core, string punct) SplitToken(string token)
            {
                int split = token.Length;
                while (split > 0 && char.IsPunctuation(token[split - 1]))
                    split--;
                string core = split > 0 ? token[..split] : "";
                string punct = split < token.Length ? token[split..] : "";
                return (core, punct);
            }
        }

        /// <summary>
        /// Trims any run of consecutive identical characters in <paramref name="s"/> so that no character repeats more than <paramref name="threshold"/> times in a row.
        /// </summary>
        public static string LimitCharReps(string s, int threshold)
        {
            if (s.IsEmpty())
                return s;

            threshold = threshold.Clamp(1, 1000); // Ensure threshold is within a reasonable range
            var sb = new StringBuilder(s.Length);
            char previousChar = '\0';
            int runCount = 0;

            foreach (char c in s)
            {
                if (c == previousChar)
                {
                    runCount++;
                }
                else
                {
                    previousChar = c;
                    runCount = 1;
                }

                if (runCount <= threshold)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Removes repetitions of any char in <paramref name="charsToCollapse"/>, collapsing consecutive runs down to a single char.
        /// </summary>
        public static string TrimCharReps(string s, IEnumerable<char> charsToCollapse)
        {
            if (s.IsEmpty() || charsToCollapse == null || charsToCollapse.None())
                return s;

            var toCollapse = new HashSet<char>(charsToCollapse); // For O(1) lookup
            var sb = new StringBuilder(s.Length);
            char previousChar = '\0';

            foreach (char c in s)
            {
                if (c == previousChar && toCollapse.Contains(c)) // If this char is one we're collapsing AND it's the same as the last one we kept, skip it; otherwise append.
                    continue;

                sb.Append(c);
                previousChar = c;
            }

            return sb.ToString();
        }

        /// <summary> Checks if a string is a valid Base64-encoded string. </summary>
        public static bool IsBase64(string s)
        {
            if (s.IsEmpty() || s.Length % 4 != 0)
                return false;
            Span<byte> buffer = new byte[s.Length];
            return Convert.TryFromBase64String(s, buffer, out _);
        }

        /// <summary> Checks if a string is a valid web URL (http or https). </summary>
        public static bool IsWebUrl(string s)
        {
            if (!Uri.TryCreate(s, UriKind.Absolute, out var uri))
                return false;
            return uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp;
        }

        /// <summary> Checks if a path could be a file path without any actual I/O operations. </summary>
        public static bool CouldBeFilePath(string s)
        {
            if (s.IsEmpty())
                return false;

            if (!Path.IsPathFullyQualified(s))
                return false;

            if (Uri.TryCreate(s, UriKind.Absolute, out var uri) && uri.IsFile)
                return true;

            return s.IndexOfAny(Path.GetInvalidPathChars()) == -1;
        }

        /// <summary>
        /// Returns the string with the highest fuzzy match score against <paramref name="findStr"/>, with a minimum score <paramref name="threshold"/>.
        /// </summary>
        public static string GetClosestFuzzyMatch(IEnumerable<string> strings, string findStr, int threshold = 90)
        {
            if (strings == null || !strings.Any() || findStr.IsEmpty())
                return null;

            return strings.Where(s => s.FuzzyMatches(findStr, threshold)).OrderByDescending(s => s.GetFuzzyMatchScore(findStr)).FirstOrDefault(); // Return the first (best) match
        }

        /// <summary> <inheritdoc cref="ReplaceLinesFuzzy(string, string, out List{ValueTuple{string, int}}, string, int, bool, bool)"/> </summary>
        public static string ReplaceLinesFuzzy(string s, string find, string replace = "", int threshold = 90, bool mustMatchWordCount = true, bool log = false) 
            => ReplaceLinesFuzzy(s, find, out _, replace, threshold, mustMatchWordCount, log);

        /// <summary>
        /// Splits a string into lines and replaces each line with <paramref name="replace"/> if the line fuzzy-matches <paramref name="find"/> with a score of at least <paramref name="threshold"/>. <br/>
        /// If <paramref name="mustMatchWordCount"/> is true, the line must have the same number of words as <paramref name="find"/> to be replaced. <br/>
        /// </summary>
        public static string ReplaceLinesFuzzy(string s, string find, out List<(string Line, int Score)> scores, string replace = "", int threshold = 90, bool mustMatchWordCount = true, bool log = false)
        {
            scores = [];

            if (s.IsEmpty() || find.IsEmpty())
                return s;

            var lines = s.SplitIntoLines();

            for(int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == find || lines[i].IsEmpty())
                    continue;

                var score = lines[i].GetFuzzyMatchScore(find);
                scores.Add((lines[i], score));

                if (score >= threshold)
                {
                    if (mustMatchWordCount && lines[i].GetWordCount() != find.GetWordCount())
                        continue;

                    if(log)
                        Logger.Log($"Replacing line '{lines[i]}' with '{replace}' (fuzzy match {score}%)");

                    lines[i] = replace;
                }
            }

            return lines.Join("\n");
        }
    }
}
