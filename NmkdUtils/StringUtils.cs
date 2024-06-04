

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
    }
}
