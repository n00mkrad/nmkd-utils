

using System.Text;
using System.Text.RegularExpressions;

namespace NmkdUtils
{
    public class StringUtils
    {
        /// <summary> Returns the longest string that all strings start with (e.g. a common root path of many file paths) </summary>
        public static string FindLongestCommonPrefix(IEnumerable<string> strings)
        {
            var stringsArray = strings is string[] ? (string[])strings : strings.ToArray();

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

        public static string PascalToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var regex = new Regex("(?<=[a-z0-9])[A-Z]|(^[A-Z][a-z0-9]+)");
            return regex.Replace(input, m => "_" + m.Value.ToLower()).TrimStart('_');
        }
    }
}
