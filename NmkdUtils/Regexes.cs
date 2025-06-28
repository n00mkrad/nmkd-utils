using System.Text.RegularExpressions;

namespace NmkdUtils
{
    public partial class Regexes
    {
        /// <summary> Space after an ellipsis if it's followed by punctuation, e. g. "... ?" </summary>
        public static readonly Regex SpaceAfterEllipsis = new(@"(?<=\.{3}) (?=[\p{P}])", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Space after an ellipsis at the start of a line </summary>
        public static readonly Regex SpaceAfterEllipsisLineStart = new(@"(?m)(?<=^\.\.\.)\s", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Dot followed by an uppercase letter and then a lowercase letter </summary>
        public static readonly Regex PunctuationFollowedByUpperLetter = new(@"([.?!])(?=[A-Z][a-z])", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Comma followed by an lowercase letter </summary>
        public static readonly Regex CommaBeforeLowerLetter = new(@",(?=[a-z])", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static readonly Regex SingleQuoteBeforeUpperLetter = new(@"'(?=[A-Z])", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public static readonly Regex SingleQuoteBeforeUpperLetterAfterSpace = new(@"(?<=\s)'(?=[A-Z])", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Any trailing lowercase "s" </summary>
        public static readonly Regex TrailingS = new(@"s\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> A size in brackets, e.g. "[960x540]" </summary>
        public static readonly Regex SizeInBrackets = new(@"\[\d+x\d+\]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Text in backticks (markdown etc.) </summary>
        public static readonly Regex InlineCode = new(@"`([^`]+)`", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Text in brackets trailing colons </summary>
        public static readonly Regex TextInBracketsColon = new(@"\[(.*?)\]:?", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Text in parentheses (incl. parentheses) including leading space </summary>
        public static readonly Regex TextInParenthesesLeadingSpaces = new(@"\s*\([^()]*\)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Text in parentheses (incl. parentheses) including leading space </summary>
        public static readonly Regex TextInParentheses = new(@"\([^()]*\)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Speaker name in SDH subtitles. Handles edge cases with lowercase letters like "McGREGOR", "DiCAPRIO" etc. </summary>
        public static readonly Regex SdhSpeakerName = new(@"(?:^|(?<=[\p{P}]\s))(?:(?:Mac)|(?:[A-Z][A-Za-z]))[A-Z0-9\- .]*:\s?", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Single hyphen at the start of a line </summary>
        public static readonly Regex HyphensAtLineStart = new(@"^-+(?!-)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Not part of a number; does not allow comma or scientific notation </summary>
        public static readonly Regex NoNumber = new(@"[^0-9\.]", RegexOptions.Compiled);

        /// <summary> Not part of a number; does not allow comma but allows scientific notation </summary>
        public static readonly Regex NoNumberAllowSci = new(@"[^0-9\.e]", RegexOptions.Compiled);

        /// <summary> Not part of a number; allows comma but not scientific notation </summary>
        public static readonly Regex NoNumberAllowComma = new(@"[^0-9\.,\-]", RegexOptions.Compiled);

        /// <summary> Not part of a number; allows comma and scientific notation </summary>
        public static readonly Regex NoNumberAllowCommaAndSci = new(@"[^0-9\.,\-e]", RegexOptions.Compiled);

        /// <summary> Duplicate text: Text, then whitespace, then same text again </summary>
        public static readonly Regex DuplicateText = new(@"^(.+?)\s+\1$", RegexOptions.Compiled);

        /// <summary> Single letter (except for I, A, O) at the start of a line, followed by whitespace </summary>
        public static readonly Regex SingleLetterNotWordAtStart = new(@"^(?![IAOiao])[A-Za-z] \s*", RegexOptions.Compiled);

        /// <summary> Any words repeated (in succesion) any amount of times </summary>
        public static readonly Regex RepeatedWords = new(@"\b(\w+)\b(?:\s+\1\b)+", RegexOptions.Compiled);

        /// <summary> Any word part (2+ characters) at the end of a word repeated any amount of times </summary>
        public static readonly Regex WordPartRepetition = new(@"\b[\w']*?([\w']{2,})\1\b", RegexOptions.Compiled);
        /// <summary> Any word part of exactly 2 characters at the end of a word repeated any amount of times </summary>
        public static readonly Regex WordPartRepetition2Ch = new(@"\b[\w']*?([\w']{2})\1\b", RegexOptions.Compiled);

        /// <summary> Tokenize words, allowing apostrophes and punctuation </summary>
        public static readonly Regex TokenizeWords = new(@"[\w']+|[^\w']+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        /// <summary> Match tokenized words (only alphanumeric and apostrophes) </summary>
        public static readonly Regex MatchTokenizedWords = new(@"^[\w']+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Repetitions of letters and apostrophes, e.g. "Kitchen's's" - Replace with '$1 to fix </summary>
        public static readonly Regex ApostropheRepetition = new(@"'(\p{L}+)(?:'\1)+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Lowercase "l" that could be an "I" - Does not allow anything directly after it </summary>
        public static readonly Regex SoleLowerLThatCouldBeI = new(@"(?m)(?:(?<= )|^)l(?![A-Za-z])", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Lowercase "l" that could be an "I" - Allows lowercase l and uppercae I directly after it </summary>
        public static readonly Regex LowerLThatCouldBeI = new(@"(?m)(?:(?<=^)|(?<= ))l(?=(?:[^A-Za-z]|[lI]|$))", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> HTML-style tags </summary>
        public static readonly Regex TagsHtml = new(@"<[^>]+>", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        /// <summary> HTML-style tags or tags in curly braces (e.g. subtitle styling) </summary>
        public static readonly Regex TagsHtmlOrCurlyBraces = new(@"<[^>]+>|{[^}]+}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        /// <summary> Tags in curly braces (e.g. subtitle styling) </summary>
        public static readonly Regex TagsCurlyBraces = new(@"\{[^}]*\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        /// <summary> HTML size tag </summary>
        public static readonly Regex TagsHtmlSize = new("size=\"\\d+\"", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> *Should* match emojis, needs some testing </summary>
        public static readonly Regex Emojis = new(@"(?:(\ud83c[\udde6-\uddff]){2}|([\#\*0-9]\u20e3)|(?:\u00a9|\u00ae|[\u2000-\u3300]|[\ud83c-\ud83e][\ud000-\udfff])(?:(?:\ud83c[\udffb-\udfff])?(?:\ud83e[\uddb0-\uddb3])?(?:\ufe0f?\u200d(?:[\u2000-\u3300]|[\ud83c-\ud83e][\ud000-\udfff])\ufe0f?)?)*)+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        
        /// <summary> A single letter repeated 2+ times after an apostrophe, at the end of a word. Excludes "ll" (e.g. "you'll") </summary>
        public static readonly Regex LetterRepAfterApostr = new(@"'(?!ll(?: |$))([A-Za-z])\1+(?= |$)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Parameter names (not types) in stack traces </summary>
        public static readonly Regex StackTraceLineParamName = new(@" (?<=\b\w+(?:<[^>]*>)?\s)[A-Za-z_]\w*(?=\s*(?:,|\)))", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        /// <summary> For stack trace cleaning - local function garbage </summary>
        public static readonly Regex StackTraceLocalFuncGarbage = new(@"<(?<type>[^>]+)>g__(?<method>[^|]+)\|\d+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        /// <summary> For stack trace cleaning - DisplayClass garbage </summary>
        public static readonly Regex StackTraceDispClassGarbage = new(@"\.?<>c__DisplayClass\d+_\d+&?", RegexOptions.Compiled | RegexOptions.CultureInvariant); // Old:  \.<\>c__DisplayClass\d+_\d+

        /// <summary> Platform-independent line breaks </summary>
        public static readonly Regex LineBreaks = new("\r\n|\r|\n", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Uppercase "I" at the end of a word that's at least 2 chars, e.g. "vakoI" instead of "vakol" </summary>
        public static readonly Regex UpperIAtWordEnd = new(@"(?<=[a-z]{2})I(?=\s|$)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Slash that is most likely an (italic) I </summary>
        public static readonly Regex OcrSlashToI = new(@"(?<=^|\s)\/(?=\w|[.,!?]|\s|$)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public static readonly Regex AnyLetterAtLeast3x = new(@"([A-Za-z])\1{2,}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public static readonly Regex StaySpaceAroundApostrophe = new(@"(?<=')\s(?=[A-Za-z]{1,2}\b)|\s(?='[A-Za-z]{1,2}\b)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Non-sentence-ending periods, checks for words with max 3 chars that start with a capital letter, e.g. Mr., Ltd., ... </summary>
        public static readonly Regex NonSentenceEndingPeriod1 = new(@"(?<=\b[A-Z][A-Za-z0-9]{0,2})\.", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        /// <summary> Non-sentence-ending periods, checks for abbreviations like "e.g.", "i.e." </summary>
        public static readonly Regex NonSentenceEndingPeriod2 = new(@"(?<=\b[A-Za-z]\.[A-Za-z])\.", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        /// <summary> Non-sentence-ending periods, checks for common abbreviations like "et al.", "etc.", "vs.", "cf.", "misc.", "dept." </summary>
        public static readonly Regex NonSentenceEndingPeriod3 = new(@"(?<=\b(?:et al|etc|vs|cf|misc|dept|Capt|Corp|Blvd|Dept))\.", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        /// <summary> Non-sentence ending periods, checks for periods that are between numbers, e.g. "1.5", "2.0", "3.14" </summary>
        public static readonly Regex NonSentenceEndingPeriodNumbers = new(@"(?<=\d)\.(?=\d)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    }
}
