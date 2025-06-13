using System.Text.RegularExpressions;

namespace NmkdUtils
{
    public partial class Regexes
    {
        /// <summary> Space after an ellipsis if it's followed by punctuation, e. g. "... ?" </summary>
        public static readonly Regex SpaceAfterEllipsis = new Regex(@"(?<=\.{3}) (?=[\p{P}])", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Space after an ellipsis at the start of a line </summary>
        public static readonly Regex SpaceAfterEllipsisLineStart = new Regex(@"(?m)(?<=^\.\.\.)\s", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Dot followed by an uppercase letter </summary>
        public static readonly Regex PunctuationFollowedByUpperLetter = new Regex(@"([.?!])(?=[A-Z])", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Comma followed by an lowercase letter </summary>
        public static readonly Regex CommaBeforeLowerLetter = new Regex(@",(?=[a-z])", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static readonly Regex SingleQuoteBeforeUpperLetter = new Regex(@"'(?=[A-Z])", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public static readonly Regex SingleQuoteBeforeUpperLetterAfterSpace = new Regex(@"(?<=\s)'(?=[A-Z])", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Any trailing lowercase "s" </summary>
        public static readonly Regex TrailingS = new Regex(@"s\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> A size in brackets, e.g. "[960x540]" </summary>
        public static readonly Regex SizeInBrackets = new Regex(@"\[\d+x\d+\]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Text in backticks (markdown etc.) </summary>
        public static readonly Regex InlineCode = new Regex(@"`([^`]+)`", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Text in brackets trailing colons </summary>
        public static readonly Regex TextInBracketsColon = new Regex(@"\[(.*?)\]:?", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Text in parentheses (incl. parentheses) including leading space </summary>
        public static readonly Regex TextInParenthesesLeadingSpaces = new Regex(@"\s*\([^()]*\)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Text in parentheses (incl. parentheses) including leading space </summary>
        public static readonly Regex TextInParentheses = new Regex(@"\([^()]*\)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Speaker name in SDH subtitles. Handles edge cases with lowercase letters like "McGREGOR", "DiCAPRIO" etc. </summary>
        public static readonly Regex SdhSpeakerName = new Regex(@"(?:^|(?<=[\p{P}]\s))(?:(?:Mac)|(?:[A-Z][A-Za-z]))[A-Z0-9\- ]*:\s?", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    }
}
