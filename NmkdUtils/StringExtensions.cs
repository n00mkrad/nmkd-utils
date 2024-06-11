using System.Globalization;
using System.Text.RegularExpressions;

namespace NmkdUtils
{
    public static partial class StringExtensions
    {
        /// <summary> Shortcut for !string.IsNullOrWhiteSpace </summary>
        public static bool IsNotEmpty(this string s)
        {
            return !string.IsNullOrWhiteSpace(s);
        }

        /// <summary> Shortcut for string.IsNullOrWhiteSpace </summary>
        public static bool IsEmpty(this string s)
        {
            return string.IsNullOrWhiteSpace(s);
        }

        /// <summary> Wrap with quotes, optionally convert backslashes to slashes or add a space to front/end </summary>
        public static string Wrap(this string path, bool backslashToSlash = false, bool addSpaceFront = false, bool addSpaceEnd = false)
        {
            string s = "\"" + path + "\"";

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
        public static string[] SplitIntoLines(this string str)
        {
            return SplitIntoLinesPattern().Split(str);
        }

        public static float GetFloat(this string str)
        {
            if (str.Length < 1 || str == null)
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

        public static int GetInt(this string str, bool allowScientificNotation = false)
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
        private static bool CouldBeScientificNotation(string s)
        {
            if (!(s.ToLowerInvariant().Contains("e+") || s.ToLowerInvariant().Contains("e-")))
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

            return str.Split(new string[] { trimStr }, StringSplitOptions.None);
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
            int place = s.IndexOf(find);

            if (place == -1)
                return s;

            return s.Remove(place, find.Length).Insert(place, replace);
        }

        /// <summary> Replaces only the last occurence of a string in a string </summary>
        public static string ReplaceLastOccurrence(this string s, string find, string replace)
        {
            int place = s.LastIndexOf(find);

            if (place == -1)
                return s;

            return s.Remove(place, find.Length).Insert(place, replace);
        }

        /// <summary> Shortcut for ToLowerInvariant </summary>
        public static string Low(this string s)
        {
            if (s == null)
                return s;

            return s.ToLowerInvariant();
        }

        /// <summary> Shortcut for ToUpperInvariant </summary>
        public static string Up(this string s)
        {
            if (s == null)
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

        /// <summary> Get enum from string (case-insensitive), return <paramref name="fallback"/> if parsing fails </summary>
        public static TEnum GetEnum<TEnum>(this string enumString, bool ignoreCase = true, TEnum? fallback = null) where TEnum : struct
        {
            if (Enum.TryParse(enumString, ignoreCase, out TEnum result))
            {
                return result;
            }
            else
            {
                return fallback.GetValueOrDefault();
            }
        }

        /// <summary> Limit sting to <paramref name="maxChars"/> chars, optionally using an <paramref name="ellipsis"/> for the last 3 chars if too long </summary>
        public static string Trunc(this string s, int maxChars, bool ellipsis = true)
        {
            if (s.IsEmpty())
                return s;

            string str = s.Length <= maxChars ? s : s.Substring(0, maxChars);

            if (ellipsis && s.Length > maxChars)
            {
                str += "…";
            }

            return str;
        }

        /// <summary> Shortcut for Replace(myString, string.Empty) </summary>
        public static string Remove(this string s, string stringToRemove)
        {
            if (s.IsEmpty() || stringToRemove.IsEmpty())
                return s;

            return s.Replace(stringToRemove, "");
        }

        /// <summary> Trims whitespaces as well as trailing slashes or backslashes </summary>
        public static string TrimPath (this string s)
        {
            if (s.IsEmpty())
                return s;

            return s.Trim().TrimEnd('\\').TrimEnd('/');
        }

        /// <summary> Checks if a string matches a wildcard <paramref name="pattern"/> </summary>
        public static bool MatchesWildcard(this string s, string pattern, bool ignoreCase = true)
        {
            return WildcardMatch(pattern, s, 0, 0, ignoreCase);
        }

        /// <summary> https://github.com/picrap/WildcardMatch </summary>
        private static bool WildcardMatch(this string wildcard, string s, int wildcardIndex, int sIndex, bool ignoreCase)
        {
            for (; ; )
            {
                // in the wildcard end, if we are at tested string end, then strings match
                if (wildcardIndex == wildcard.Length)
                    return sIndex == s.Length;

                var c = wildcard[wildcardIndex];
                switch (c)
                {
                    // always a match
                    case '?':
                        break;
                    case '*':
                        // if this is the last wildcard char, then we have a match, whatever the tested string is
                        if (wildcardIndex == wildcard.Length - 1)
                            return true;
                        // test if a match follows
                        return Enumerable.Range(sIndex, s.Length - sIndex).Any(i => WildcardMatch(wildcard, s, wildcardIndex + 1, i, ignoreCase));
                    default:
                        var cc = ignoreCase ? char.ToLower(c) : c;
                        if (s.Length == sIndex)
                        {
                            return false;
                        }
                        var sc = ignoreCase ? char.ToLower(s[sIndex]) : s[sIndex];
                        if (cc != sc)
                            return false;
                        break;
                }

                wildcardIndex++;
                sIndex++;
            }
        }
    }
}
