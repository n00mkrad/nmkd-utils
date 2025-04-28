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
        public static bool IsEmpty(this string? s)
        {
            return string.IsNullOrWhiteSpace(s);
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


        [GeneratedRegex("\r\n|\r|\n")]
        private static partial Regex SplitIntoLinesPattern();
        public static string[] SplitIntoLines(this string str) => SplitIntoLinesPattern().Split(str);
        public static string[] SplitIntoLinesOut(this string str, out string[] lines)
        {
            lines = SplitIntoLines(str);
            return lines;
        }

        /// <summary>
        /// Split a string by a <paramref name="separator"/> into <paramref name="parts"/>. Returns true if the split resulted in at least 2 parts, or exactly the <paramref name="targetParts"/> count if specified.
        /// </summary>
        public static bool SplitOut (this string str, string separator, out string[] parts, int targetParts = -1)
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
        /// Split a string by a <paramref name="separator"/> into 2 parts, <paramref name="split1"/> and <paramref name="split2"/>. The return bool is success/failure.<br/>
        /// If <paramref name="allowMore"/> is true, a split result with more than 2 entries does not count as failure.
        /// </summary>
        public static bool Split2(this string str, string separator, out string split1, out string split2, string defaultVal1 = "", string defaultVal2 = "", bool allowMore = false)
        {
            split1 = defaultVal1;
            split2 = defaultVal2;

            if (str.IsEmpty())
                return false;

            var split = str.Split(separator);

            if (split.Length < 2)
                return false;

            if (!allowMore && split.Length > 2)
                return false;

            split1 = split[0];
            split2 = split[1];
            return true;
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
            if (!allowDotComma)
                s = Regex.Replace(s, $"[^0-9.{(allowScientific ? "e" : "")}]", "");
            else
                s = Regex.Replace(s, $"[^.,0-9-{(allowScientific ? "e" : "")}]", "");

            return s.Trim();
        }

        public static int GetInt(this string? str, out bool success, bool allowScientificNotation = false)
        {
            int i = str.GetInt(allowScientificNotation, failureValue: int.MinValue);
            success = i != int.MinValue;
            return i;
        }

        public static int GetInt(this string? str, bool allowScientificNotation = false, int failureValue = 0)
        {
            if (str == null || str.Length < 1)
                return 0;

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
                return 0;
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
        public static string ReplaceFirst(this string s, string find, string replace)
        {
            if (s.IsEmpty())
                return s;

            int place = s.IndexOf(find);

            if (place == -1)
                return s;

            return s.Remove(place, find.Length).Insert(place, replace);
        }

        /// <summary> Replaces only the last occurence of a string in a string </summary>
        public static string ReplaceLastOccurrence(this string s, string find, string replace)
        {
            if (s.IsEmpty())
                return s;

            int place = s.LastIndexOf(find);

            if (place == -1)
                return s;

            return s.Remove(place, find.Length).Insert(place, replace);
        }

        /// <summary> Replaces a string if it starts with the <paramref name="find"/> string. Ignores later occurences unless <paramref name="firstOccurenceOnly"/> is false. </summary>
        public static string ReplaceAtStart(this string s, string find, string replace, bool firstOccurenceOnly = true)
        {
            if(s.IsEmpty() || !s.StartsWith(find))
                return s;

            if (firstOccurenceOnly)
                return s.ReplaceFirst(find, replace);
            else
                return s.Replace(find, replace);
        }

        /// <summary> Replaces line breaks (one or more) with <paramref name="delimiter"/> </summary>
        public static string RemoveLineBreaks(this string s, string delimiter = " ")
        {
            if (s.IsEmpty())
                return s;

            string pattern = @"(?:\r\n|\r|\n)+"; // matches one or more occurrences of \r\n (Windows), \n (Unix), or \r (older Macs)
            return Regex.Replace(s, pattern, delimiter); // Replace the consecutive line breaks with a single delimiter
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
        public static string TrimPath (this string s)
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

        /// <summary> Checks if a string matches all wildcard <paramref name="patterns"/> </summary>
        public static bool MatchesAllWildcards(this string s, IEnumerable<string> patterns, bool ignoreCase = true, bool orContains = false)
        {
            return patterns.All(p => s.MatchesWildcard(p, ignoreCase, orContains));
        }

        /// <summary> Capitalizes the first char of a string. </summary>
        public static string CapitalizeFirstChar(this string s)
        {
            if (s.IsEmpty())
                return s;

            return char.ToUpper(s[0]) + s.Substring(1);
        }

        /// <summary>
        /// Removes text in parentheses, including the parentheses themselves. Optionally removes the leading space before the parentheses.
        /// </summary>
        public static string RemoveTextInParentheses (this string s, bool includeLeadingSpace = true)
        {
            string pattern = includeLeadingSpace ? @"\s*\([^()]*\)" : @"\([^()]*\)";
            return Regex.Replace(s, pattern, "");
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
    }
}
