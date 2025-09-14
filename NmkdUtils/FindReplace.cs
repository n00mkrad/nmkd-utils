using System.Text.RegularExpressions;

namespace NmkdUtils
{
    public class FindReplace
    {
        public enum MatchMode
        {
            Anywhere,
            AtStart,
            AtEnd,
            Complete
        }

        public string Find { get; set; } = "";
        public string Replace { get; set; } = "";
        public bool CaseSensitive { get; set; } = false;
        public bool UseRegex { get; set; } = false;
        public MatchMode Mode { get; set; } = MatchMode.Anywhere;

        public FindReplace() { }

        public FindReplace(string find, string replace = "", bool ci = false, bool useRegex = false, MatchMode mode = MatchMode.Anywhere)
        {
            Find = find;
            Replace = replace;
            CaseSensitive = ci;
            UseRegex = useRegex;
            Mode = mode;
        }

        /// <summary> Apply a single <see cref="FindReplace"/> to an input string. </summary>
        public static string Apply(string input, FindReplace config)
        {
            if (input.IsEmpty() || config.Find.IsEmpty())
                return input;

            string pattern = config.UseRegex ? config.Find : Regex.Escape(config.Find); // If RegEx is disabled, escape the "Find" string to treat it literally

            // Apply anchors based on MatchMode
            switch (config.Mode)
            {
                case MatchMode.AtStart:
                    pattern = "^" + pattern;
                    break;
                case MatchMode.AtEnd:
                    pattern += "$";
                    break;
                case MatchMode.Complete:
                    pattern = "^" + pattern + "$";
                    break;
                case MatchMode.Anywhere:
                default:
                    break; // No special anchoring
            }

            RegexOptions options = config.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            return Regex.Replace(input, pattern, config.Replace, options);
        }

        /// <summary> Apply multiple <see cref="FindReplace"/> objects in sequence to an input string. </summary>
        public static string Apply(string input, IEnumerable<FindReplace> configs)
        {
            if (configs == null)
                return input;

            string result = input;

            foreach (var config in configs)
            {
                result = Apply(result, config);
            }

            return result;
        }
    }
}
