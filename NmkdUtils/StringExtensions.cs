using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

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

        /// <summary> Shortcut for ToLowerInvariant </summary>
        public static string Lower(this string s)
        {
            if (s == null)
                return s;

            return s.ToLowerInvariant();
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
    }
}
