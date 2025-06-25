using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NmkdUtils
{
    public static partial class StringExtensions
    {
        /// <summary> Shortcut for !string.IsNullOrWhiteSpace </summary>
        public static bool IsNotEmpty(this string? s)
        {
            return !string.IsNullOrWhiteSpace(s);
        }

        /// <summary> Shortcut for string.IsNullOrWhiteSpace </summary>
        public static bool IsEmpty(this string? s, bool whitespaceCountsAsEmpty = true)
        {
            if (whitespaceCountsAsEmpty)
                return string.IsNullOrWhiteSpace(s);

            return string.IsNullOrEmpty(s);
        }

        public static string ReplaceEmpty(this string s, string replaceWith = "N/A")
        {
            return s.IsEmpty() ? replaceWith : s;
        }

        /// <summary> Wrap with quotes, optionally convert backslashes to slashes or add a space to front/end </summary>
        public static string Wrap(this string path, bool backslashToSlash = false, bool addSpaceFront = false, bool addSpaceEnd = false, char ch = '"')
        {
            string s = $"{ch}{path}{ch}";

            if (addSpaceFront)
                s = " " + s;

            if (addSpaceEnd)
                s += " ";

            if (backslashToSlash)
                s = s.Replace(@"\", "/");

            return s;
        }

        /// <summary> Wrap with single quotes, optionally convert backslashes to slashes or add a space to front/end </summary>
        public static string WrapSingle(this string path, bool backslashToSlash = false, bool addSpaceFront = false, bool addSpaceEnd = false)
        {
            string s = $"'{path}'";

            if (addSpaceFront)
                s = " " + s;

            if (addSpaceEnd)
                s += " ";

            if (backslashToSlash)
                s = s.Replace(@"\", "/");

            return s;
        }

        public static string[] SplitIntoLines(this string str, bool ignoreEmpty = false)
        {
            string[] split = Regexes.LineBreaks.Split(str);
            return ignoreEmpty ? split.Where(line => line.IsNotEmpty()).ToArray() : split;
        }
        public static string[] SplitIntoLinesOut(this string str, out string[] lines, bool ignoreEmpty = false)
        {
            lines = SplitIntoLines(str, ignoreEmpty);
            return lines;
        }

        /// <summary> Splits a string into lines, works for Windows/Linux style line endings. <paramref name="removeEmpty"/> will not return empty lines. </summary>
        public static List<string> GetLines(this string str, bool removeEmpty = false)
        {
            var split = Regexes.LineBreaks.Split(str);
            return removeEmpty ? split.Where(line => line.IsNotEmpty()).ToList() : split.ToList();
        }
        /// <inheritdoc cref="GetLines(string, bool)"/>
        public static List<string> GetLines(this string str, out List<string> lines, bool removeEmpty = false)
        {
            lines = str.GetLines(removeEmpty);
            return lines;
        }

        /// <summary>
        /// Split a string by a <paramref name="separator"/> into <paramref name="parts"/>. Returns true if the split resulted in at least 2 parts, or exactly the <paramref name="targetParts"/> count if specified.
        /// </summary>
        public static bool SplitOut(this string str, string separator, out string[] parts, int targetParts = -1)
        {
            parts = str.Length == 1 ? str.Split(separator[0]) : str.Split(separator); // Use single char split if possible

            if (targetParts >= 0 && parts.Length != targetParts)
            {
                parts = [];
                return false;
            }

            return parts.Length > 1;
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

        /// <summary>
        /// Splits a string by multiple separators
        /// </summary>
        public static string[] Split(this string s, IEnumerable<string> separators, bool ci = false)
        {
            if (s == null)
                return [];

            List<string> result = new List<string>();
            int startIndex = 0;
            while (startIndex <= s.Length)
            {
                int matchIndex = -1;
                string matchSep = null;
                foreach (string sep in separators)
                {
                    if (string.IsNullOrEmpty(sep)) continue;
                    int idx = s.IndexOf(sep, startIndex, ci ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                    if (idx >= 0 && (matchIndex < 0 || idx < matchIndex))
                    {
                        matchIndex = idx;
                        matchSep = sep;
                    }
                }
                if (matchIndex < 0)
                {
                    result.Add(s[startIndex..]);
                    break;
                }
                result.Add(s[startIndex..matchIndex]);
                startIndex = matchIndex + matchSep.Length;
            }
            return result.Skip(1).ToArray();
        }

        public static float GetFloat(this string? str)
        {
            if (str == null || str.Length < 1)
                return 0f;

            string num = str.TrimNumbers(true).Replace(",", ".");
            float.TryParse(num, NumberStyles.Any, CultureInfo.InvariantCulture, out float value);
            return value;
        }

        /// <summary> Remove anything from a string that is not a number, optionally allowing scientific notation (<paramref name="allowScientific"/>) </summary>
        public static string TrimNumbers(this string s, bool allowDotComma = false, bool allowScientific = false)
        {
            Regex rgx = allowDotComma
            ? (allowScientific ? Regexes.NoNumberAllowCommaAndSci : Regexes.NoNumberAllowComma)
            : (allowScientific ? Regexes.NoNumberAllowSci : Regexes.NoNumber);

            return s.RegexReplace(rgx).Trim();
        }

        public static bool GetIntOut(this string? str, out int value, bool allowScientificNotation = false)
        {
            value = str.GetInt(allowScientificNotation, failureValue: int.MinValue);
            return value != int.MinValue;
        }

        public static int GetInt(this string? str, out bool success, bool allowScientificNotation = false)
        {
            int i = str.GetInt(allowScientificNotation, failureValue: int.MinValue);
            success = i != int.MinValue;
            return i;
        }

        public static int GetInt(this string? str, bool allowScientificNotation = false, int failureValue = 0)
        {
            if (str.IsEmpty())
                return failureValue;

            str = str.Trim();

            try
            {
                if (allowScientificNotation && CouldBeScientificNotation(str))
                    return int.Parse(str.TrimNumbers(true, true), NumberStyles.Float, CultureInfo.InvariantCulture);

                if (str.Length >= 2 && str[0] == '-' && str[1] != '-')
                    return int.Parse("-" + str.TrimNumbers());
                else
                    return int.Parse(str.TrimNumbers());
            }
            catch
            {
                return failureValue;
            }
        }

        /// <summary> Somewhat basic check to determine if a number string appears to be written as scientific notation </summary>
        private static bool CouldBeScientificNotation(string? s)
        {
            if (s is null || !(s.Contains("e+", StringComparison.OrdinalIgnoreCase) || s.Contains("e-", StringComparison.OrdinalIgnoreCase)))
                return false;

            if (s[0] == 'e' || s.Last() == '+' || s.Last() == '-') // e must be in the middle, can't be first char (and +- can't be last)
                return false;

            return true;
        }

        public static long GetLong(this string str)
        {
            if (str == null || str.Length < 1)
                return 0;

            str = str.Trim();

            try
            {
                if (str.Length >= 2 && str[0] == '-' && str[1] != '-')
                    return long.Parse("-" + str.TrimNumbers());
                else
                    return long.Parse(str.TrimNumbers());
            }
            catch
            {
                return 0;
            }
        }

        public static bool GetBool(this string s)
        {
            try
            {
                if (s.IsEmpty())
                    return false;

                return bool.Parse(s);
            }
            catch
            {
                return false;
            }
        }

        /// <summary> Split a string by another string </summary>
        public static string[] Split(this string str, string trimStr)
        {
            if (str == null)
                return [];

            return str.Split([trimStr], StringSplitOptions.None);
        }

        /// <summary> Checks if a string is an integer (consists only of numbers) </summary>
        public static bool IsIntegerNumber(this string value)
        {
            return value.IsNotEmpty() && value.All(char.IsDigit);
        }

        /// <summary> Replaces a list of characters with a given string </summary>
        public static string ReplaceChars(this string str, IEnumerable<char> chars, string replaceWith = "")
        {
            foreach (char c in chars)
            {
                str = str.Replace(c.ToString(), replaceWith);
            }

            return str;
        }

        /// <summary> Replaces a list of strings with a given string </summary>
        public static string Replace(this string str, IEnumerable<string> strings, string replaceWith = "")
        {
            foreach (string s in strings)
            {
                str = str.Replace(s, replaceWith);
            }

            return str;
        }

        /// <summary> Replaces only the first occurence of a string in a string </summary>
        public static string ReplaceFirst(this string s, string find, string replace, bool caseIns = true)
        {
            if (s.IsEmpty())
                return s;

            int place = s.IndexOf(find, caseIns ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

            if (place == -1)
                return s;

            return s.Remove(place, find.Length).Insert(place, replace);
        }

        /// <summary> Replaces only the last occurence of a string in a string </summary>
        public static string ReplaceLast(this string s, string find, string replace)
        {
            if (s.IsEmpty())
                return s;

            int place = s.LastIndexOf(find);

            if (place == -1)
                return s;

            return s.Remove(place, find.Length).Insert(place, replace);
        }

        /// <summary> Replaces a string if it starts with the <paramref name="find"/> string. Ignores later occurences unless <paramref name="firstOccurenceOnly"/> is false. </summary>
        public static string ReplaceAtStart(this string s, string find, string replace = "", bool firstOccurenceOnly = true, bool caseIns = false)
        {
            if (s.IsEmpty() || !s.StartsWith(find, caseIns ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                return s;

            if (firstOccurenceOnly)
                return s.ReplaceFirst(find, replace, caseIns);
            else
                return s.Replace(find, replace, caseIns ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        /// <summary> Replaces line breaks (one or more) with <paramref name="delimiter"/> </summary>
        public static string SquashLines(this string s, string delimiter = " ", List<string>? unlessLineStartsWith = null)
        {
            if (s.IsEmpty())
                return s;

            if (unlessLineStartsWith != null)
            {
                foreach (string exceptionStr in unlessLineStartsWith)
                {
                    s = s.Replace($"\n{exceptionStr}", $"<<<NO_LINE_BREAK>>>{exceptionStr}");
                }
            }

            string pattern = @"(?:\r\n|\r|\n)+"; // matches one or more occurrences of \r\n (Windows), \n (Unix), or \r (older Macs)
            string result = Regex.Replace(s, pattern, delimiter); // Replace the consecutive line breaks with a single delimiter

            if (unlessLineStartsWith != null)
            {
                result = result.Replace($"<<<NO_LINE_BREAK>>>", $"\n");
            }

            return result;
        }

        /// <summary>
        /// Removes empty/whitespace lines. If <paramref name="newLine"/> is not passed, Environment.NewLine is used for joining.
        /// </summary>
        public static string RemoveEmptyLines(this string s, string? newLine = null)
        {
            if (s.IsEmpty())
                return s;

            return s.SplitIntoLines().Where(l => l.IsNotEmpty()).Join(Environment.NewLine);
        }

        /// <summary> Shortcut for ToLowerInvariant </summary>
        public static string Low(this string s)
        {
            if (s.IsEmpty())
                return s;

            return s.ToLowerInvariant();
        }

        /// <summary> Shortcut for ToUpperInvariant </summary>
        public static string Up(this string s)
        {
            if (s.IsEmpty())
                return s;

            return s.ToUpperInvariant();
        }

        /// <summary> Removes any chars that are not a digit </summary>
        public static string RemoveNumbers(this string input)
        {
            return new string(input.Where(c => !char.IsDigit(c)).ToArray());
        }

        /// <summary> Replace all chars with an aterisk by default, or a custom censor char <paramref name="censorChar"/> </summary>
        public static string Censor(this string s, char censorChar = '*')
        {
            if (s.IsEmpty())
                return s;

            return new string(censorChar, s.Length);
        }

        /// <summary> Limit sting to <paramref name="maxChars"/> chars, optionally using an <paramref name="ellipsis"/> for the last 3 chars if too long </summary>
        public static string Trunc(this string s, int maxChars, bool ellipsis = true)
        {
            if (s.IsEmpty())
                return s;

            string suffix = "";

            // Truncate the string so it fits in the maxChars limit. If ellipsis is true, add "..." to the end, but only if that wouldn't make it longer than maxChars
            if (s.Length > maxChars)
            {
                suffix = ellipsis && maxChars > 3 ? "..." : "";
                return string.Concat(s.AsSpan(0, maxChars - suffix.Length), suffix);
            }

            return s;
        }

        /// <summary> Shortcut for string.Replace(myString, string.Empty) </summary>
        public static string Remove(this string s, string stringToRemove)
        {
            if (s.IsEmpty() || stringToRemove.IsEmpty())
                return s;

            return s.Replace(stringToRemove, "");
        }

        /// <summary> Removes all specified chars from a string </summary>
        public static string Remove(this string s, IEnumerable<char> charsToRemove)
        {
            if (s.IsEmpty())
                return s;

            return s.ReplaceChars(charsToRemove, "");
        }

        /// <summary> Trims whitespaces as well as trailing slashes or backslashes </summary>
        public static string TrimPath(this string s)
        {
            if (s.IsEmpty())
                return s;

            return s.Trim().TrimEnd('\\').TrimEnd('/');
        }

        /// <summary> Checks if a string matches a wildcard <paramref name="pattern"/> </summary>
        public static bool MatchesWildcard(this string s, string pattern, bool ignoreCase = true, bool orContains = false)
        {
            // If the pattern contains no wildcards, we use a simple Contains() check, if allowed by the parameters
            if (orContains && !s.Contains('*') && !s.Contains('?'))
                return s.Contains(pattern, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

            return StringUtils.WildcardMatch(pattern, s, 0, 0, ignoreCase);
        }

        /// <summary> Checks if a string matches all of the provided wildcard <paramref name="patterns"/> </summary>
        public static bool MatchesAllWildcards(this string s, IEnumerable<string> patterns, bool ignoreCase = true, bool orContains = false) => patterns.All(p => s.MatchesWildcard(p, ignoreCase, orContains));
        /// <summary> Checks if a string matches any of the provided wildcard <paramref name="patterns"/> </summary>
        public static bool MatchesAnyWildcard(this string s, IEnumerable<string> patterns, bool ignoreCase = true, bool orContains = false) => patterns.Any(p => s.MatchesWildcard(p, ignoreCase, orContains));

        /// <summary> Capitalizes the first char of a string. </summary>
        public static string CapitalizeFirstChar(this string s)
        {
            if (s.IsEmpty())
                return s;

            return char.ToUpper(s[0]) + s.Substring(1);
        }

        /// <summary>
        /// Removes text in parentheses, including the parentheses themselves. Optionally removes the leading space before the parentheses. <br/>
        /// With <paramref name="convertBrackets"/>, brackets are converted to parentheses first.
        /// </summary>
        public static string RemoveTextInParentheses(this string s, bool withLeadingSpace = false, bool convertBrackets = false)
        {
            if (convertBrackets)
                s = s.Replace('[', '(').Replace(']', ')');

            return s.RegexReplace(withLeadingSpace ? Regexes.TextInParenthesesLeadingSpaces : Regexes.TextInParentheses);
        }

        /// <summary>
        /// Shortcut for string.Join(separator, source) with a default separator of ", ".
        /// </summary>
        public static string Join<T>(this IEnumerable<T> source, string separator = ", ")
        {
            return string.Join(separator, source);
        }

        /// <summary> Wraps a string in an XML CDATA tag (opening and closing) </summary>
        public static string WrapXml(this object s, string tag, bool cdata = false)
        {
            return cdata ? $"<{tag}><![CDATA[{s}]]></{tag}>" : $"<{tag}>{s}</{tag}>";
        }

        public static string ToStr(this Size size)
        {
            return $"{size.Width}x{size.Height}";
        }

        /// <summary>
        /// Replaces consecutive spaces (or optionally, any whitespace characters if <paramref name="includeAnyWhitespace"/> is true) with a single space.
        /// </summary>
        public static string SquashSpaces(this string s, bool includeAnyWhitespace = false)
        {
            if (s.IsEmpty())
                return s;

            string pattern = includeAnyWhitespace ? @"\s+" : " +"; // Choose the appropriate regex: "\s+" matches any whitespace, " +" matches only space characters.
            return Regex.Replace(s, pattern, " ");
        }

        /// <summary> Shortcut for case-insensitive string.Contains </summary>
        public static bool ContainsCi(this string s, string value)
        {
            return s.Contains(value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary> Checks if a string contains any of the given values. Optionally case-insensitive. </summary>
        public static bool ContainsAny(this string s, IEnumerable<string> values, bool caseIns = false)
        {
            return values.Any(value => s.Contains(value, caseIns ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
        }

        /// <summary> Uses TextCopy to copy a string to the clipboard. </summary>
        public static void CopyToClipboard(this string s)
        {
            TextCopy.ClipboardService.SetText(s);
        }

        public static bool IsOneOf(this string s, bool caseSensitive, params object[] strings)
        {
            StringComparison strComp = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            if (strings.Length == 0)
                return false;

            if (strings.Length == 1 && strings[0] is IEnumerable<string> col)
                return col.Any(v => s.Equals(v, strComp));

            return strings.Any(v => s.Equals(v.ToString(), strComp));
        }

        public static bool IsOneOf(this string s, params object[] strings) => IsOneOf(s, caseSensitive: true, strings);

        public static string DecodeUnicodeEscapes(string input)
        {
            // Finds every “\u” followed by exactly four hex digits
            return Regex.Replace(input, @"\\u([0-9A-Fa-f]{4})", m =>
            {
                // parse the hex value, convert to its char, and return
                int code = int.Parse(m.Groups[1].Value, NumberStyles.HexNumber);
                return ((char)code).ToString();
            });
        }

        // Matches Unicode words and allows internal hyphens or apostrophes (e.g., "co-founder", "can't").
        private static readonly Regex _wordsRegex = new(@"\b\p{L}+(?:[\p{Pd}']\p{L}+)*\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex _wordsNumbersRegex = new(@"\b\p{L}+(?:[\p{Pd}']\p{L}+)*\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Returns the number of words in <paramref name="input"/>. A "word" is any contiguous run of Unicode letters, optionally containing embedded hyphens or apostrophes. </summary>
        public static int GetWordCount(this string? input, bool alsoMatchNumbers = true) => input.GetWords(alsoMatchNumbers).Length;

        /// <summary> Gets all words in <paramref name="input"/>. A "word" is any contiguous run of Unicode letters, optionally containing embedded hyphens or apostrophes. </summary>
        public static string[] GetWords(this string? input, bool alsoMatchNumbers = true)
        {
            if (input.IsEmpty())
                return [];

            Regex regex = alsoMatchNumbers ? _wordsNumbersRegex : _wordsRegex;
            MatchCollection matches = regex.Matches(input);
            string[] words = new string[matches.Count];

            for (int i = 0; i < matches.Count; i++)
            {
                words[i] = matches[i].Value;
            }

            return words;
        }

        /// <summary> Checks if a string consists mostly of numbers, based on <paramref name="threshold"/> (0-1). </summary>
        public static bool IsMostlyNumbers(this string s, float threshold = 0.8f)
        {
            s = s.Replace(".", "").Replace(" ", "");
            int length = s.Length;
            int digitCount = s.Count(char.IsDigit);
            float digitQuota = (float)digitCount / length;
            return digitQuota > threshold;
        }

        /// <summary> Repeats a string <paramref name="count"/> times. <br/> If <paramref name="atLeastOne"/> is true, a count of less than 1 will return the string once instead of an empty string. </summary>
        public static string RepeatStr(this string s, int count, bool atLeastOne = false)
        {
            if (count <= 0 && !atLeastOne)
                return s;

            if (count <= 0)
                return "";

            return string.Concat(Enumerable.Repeat(s, count));
        }

        /// <summary> Like replace, but using a RegEx pattern string. </summary>
        public static string RegexReplace(this string s, string pattern, string replacement = "")
        {
            if (s.IsEmpty() || pattern.IsEmpty())
                return s;

            return Regex.Replace(s, pattern, replacement);
        }

        /// <summary> Like replace, but using a RegEx pattern object. </summary>
        public static string RegexReplace(this string s, Regex pattern, string replacement = "")
        {
            if (s.IsEmpty())
                return s;

            return pattern.Replace(s, replacement);
        }

        /// <summary>
        /// Extended Replace method, does not error if <paramref name="find"/> is empty, can be case-insensitive with <paramref name="ci"/>, <br/>
        /// can replace only the first occurence with <paramref name="firstOnly"/>.
        /// </summary>
        public static string Replace(this string s, string find, string replace = "", bool ci = false, bool firstOnly = false)
        {
            if (s.IsEmpty(false) || find.IsEmpty(false))
                return s;

            if (firstOnly)
                return s.ReplaceFirst(find, replace, caseIns: true);
            else
                return s.Replace(find, replace, ci ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        private static string TrimSide(this string s, string trimStr, bool ci, bool fromStart)
        {
            if (s.IsEmpty())
                return s;

            StringComparison comparison = ci ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            if (fromStart)
            {
                while (s.StartsWith(trimStr, comparison))
                {
                    s = s.Substring(trimStr.Length);
                }
            }
            else
            {
                while (s.EndsWith(trimStr, comparison))
                {
                    s = s.Substring(0, s.Length - trimStr.Length);
                }
            }

            return s;
        }

        /// <summary> <inheritdoc cref="Trim(string, string, bool)"/>/> </summary>
        public static string TrimStart(this string s, string trimStr, bool ci = false) => s.TrimSide(trimStr, ci, fromStart: true);

        /// <summary> <inheritdoc cref="Trim(string, string, bool)"/>/> </summary>
        public static string TrimEnd(this string s, string trimStr, bool ci = false) => s.TrimSide(trimStr, ci, fromStart: false);

        /// <summary> Trim an entire string instead of a char from a string. Case-insensitive if <paramref name="ci"/> is true. </summary>
        public static string Trim(this string s, string trimStr, bool ci = false) => s.IsEmpty() ? s : s.TrimStart(trimStr, ci).TrimEnd(trimStr, ci);

        public static bool FuzzyMatches(this string s, string comparisonStr, int threshold = 90, bool trim = true) => s.GetFuzzyMatchScore(comparisonStr, trim) >= threshold;

        public static int GetFuzzyMatchScore(this string s, string comparisonStr, bool trim = true)
        {
            if (s.IsEmpty() || comparisonStr.IsEmpty())
                return 0;

            if (trim)
            {
                s = s.Trim();
            }

            int score = FuzzySharp.Fuzz.WeightedRatio(s, comparisonStr);
            // if(score > 70)
            //     Debug.WriteLine($"Fuzz score; Checking for '{comparisonStr}' in '{s}': {score}");
            return score;
        }

        /// <summary> Shortcut for string.Format </summary>
        public static string Format(this string s, params object[] values)
        {
            if (s.IsEmpty())
                return s;

            return string.Format(s, values);
        }

        public static bool MatchesRegex(this string s, Regex regex, out List<Match> matches)
        {
            matches = [];

            if (s.IsEmpty())
                return false;

            matches = regex.Matches(s).ToList();
            return matches.Any();
        }

        public static bool MatchesRegex(this string s, Regex regex) => MatchesRegex(s, regex, out _);

        public static string AppendIf(this string s, string text, Func<bool> condition)
        {
            if (s.IsEmpty() || !condition())
                return s;

            return s + text;
        }
        public static string AppendIf(this string s, string text, bool condition) => s.AppendIf(text, () => condition);
    }
}
