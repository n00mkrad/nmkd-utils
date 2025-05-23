using NmkdUtils.Extensions;
using System.Net.Http.Headers;
using static NmkdUtils.Media.MediaData;
using static NmkdUtils.Media.StreamFiltering;

namespace NmkdUtils.Media
{
    public class StreamFiltering
    {
        public class StreamFilter
        {
            /// <summary> Stream index(es). Use negative numbers to invert the stream ID selection </summary>
            public List<int>? Indexes { get; set; } = null;
            /// <summary> Stream media type(s) </summary>
            public List<CodecType>? Types { get; set; } = null;
            /// <summary> Stream codec(s) </summary>
            public List<string>? Codecs { get; set; } = null;
            /// <summary> Stream language(s) </summary>
            public List<LanguageUtils.Language>? Langs { get; set; } = null;
            /// <summary> Stream title(s) as wildcard patterns. Case-insensitive unless prefixed with '_' </summary>
            public List<string>? Titles { get; set; } = null;
            /// <summary> Invert the filter to blacklist instead of whitelist </summary>
            public bool Invert { get; set; } = false;
            /// <summary> Amount of streams to return; 0 means all, 1 is first, etc. </summary>
            public int Amount { get; set; } = 0;
        }

        /// <summary>
        /// Filter streams using multiple <paramref name="filters"/> in sequence.<br/><br/>Refer to <see cref="FilterStreams(IEnumerable{Stream}, CodecType, LanguageUtils.Language?, string, bool, int)"/>:<br/>
        /// </summary>
        public static List<Stream> FilterStreams(IEnumerable<Stream> streams, List<StreamFilter> filters)
        {
            foreach (var f in filters)
            {
                streams = FilterStreams(streams, f);
            }
            return streams.ToList();
        }

        /// <summary>
        /// Filters streams using a single <paramref name="f"/> filter
        /// </summary>
        public static List<Stream> FilterStreams(IEnumerable<Stream> streams, StreamFilter f)
        {
            var copy = f.Invert ? streams.ToList() : []; // If invert is true, we need a copy of the original list to remove matches from

            if (f.Indexes.HasItems())
            {
                // Include streams with positive indexes, exclude streams with negative indexes
                var pos = f.Indexes.Where(i => i >= 0);
                var neg = f.Indexes.Where(i => i < 0).Select(Math.Abs); // Turn negative indexes into positive ones again
                streams = streams.Where(s => pos.Contains(s.Index) && !neg.Contains(s.Index));
            }

            if (f.Types.HasItems())
            {
                streams = streams.Where(s => f.Types.Contains(s.Type));
            }

            if (f.Codecs.HasItems())
            {
                streams = streams.Where(s => f.Codecs.Any(c => s.CodecFriendly.MatchesWildcard(c, orContains: true)));
            }

            if (f.Langs.HasItems())
            {
                streams = streams.Where(s => f.Langs.Contains(LanguageUtils.GetLangByCode(s.Language)));
            }

            if (f.Titles.HasItems())
            {
                // Filter streams by title; getting only those that match the wildcard pattern. If a title is prefixed with _, it will be case-sensitive, the underscore is removed before running the wildcard match
                streams = streams.Where(s => f.Titles.Any(t => s.Title.MatchesWildcard(t.TrimStart('_'), ignoreCase: !t.StartsWith("_"))));
            }

            f.Amount = f.Amount > 0 ? f.Amount : 100; // Hardcoded upper limit of 100 streams for now

            if (!f.Invert)
                return streams.Take(f.Amount).ToList(); // Limit amount and return

            // Remove all matches from the copy using stream index
            foreach (var s in streams)
            {
                copy.RemoveAll(c => c.Index == s.Index);
            }

            return copy.Take(f.Amount).ToList(); // Limit amount and return
        }

        public static StreamFilter AddStreamFilterArgs(ArgParseExtensions.Options opts, bool first = false)
        {
            opts.AddHelpArgIfNotPresent();
            var f = new StreamFilter();

            var set = new NDesk.Options.OptionSet() {
                { $"title__{nameof(StreamFilter)}", $"Media Stream Selection:", v => { } },
                { "s_id|indexes=", "Stream index(es).", v => f.Indexes = v.SplitValues().Select(s => s.GetInt()).ToList() },
                { "s_a|amount=", "Amount of streams to return; 0 means all, 1 is first, etc.", v => f.Amount = v.GetInt() },
                { "s_ty|type=", $"Stream type(s) ({StringUtils.GetEnumNamesSnek(typeof(CodecType)).Join("/")})", v => f.Types = v.SplitValues().Select(t => t.GetEnumCli<CodecType>()).ToList() },
                { "s_c|codecs=", $"Stream codec(s) as wildcard patterns.", v => f.Codecs = v.SplitValues().ToList() },
                { "s_t|titles=", "Stream title(s) as wildcard patterns. Case-insensitive unless prefixed with '_'", v => f.Titles = v.SplitValues().ToList() },
                { "s_l|langs=", "Stream language(s) as ISO 639 codes or names", v => f.Langs = v.SplitValues().Select(LanguageUtils.GetLangByNameOrCode).ToList() },
                { "s_i|invert", "Invert the filter to act as a blacklist instead of a whitelist", v => f.Invert = v != null }
            };

            if (first)
            {
                foreach (var opt in set.Reverse())
                {
                    opts.OptionsSet.Insert(0, opt);
                }
            }
            else
            {
                foreach (var opt in set)
                {
                    opts.OptionsSet.Add(opt);
                }
            }

            return f;
        }

        public static StreamFilter FromInteractiveCli()
        {
            var f = new StreamFilter();

            string idxInput = CliUtils.ReadLine("Specify stream index(es). Use negative numbers to invert the stream ID selection. Use * to select all and skip additional prompts:");
            if (idxInput == "*")
                return f;
            f.Indexes = idxInput.SplitValues().Select(s => s.GetInt()).ToList();
            CliUtils.ReadLine("Specify amount of streams to return (0 = All, 1 = First):", v => f.Amount = v.GetInt());
            CliUtils.ReadLine($"Specify stream type(s) ({StringUtils.GetEnumNamesSnek(typeof(CodecType)).Join("/")}):", v => f.Types = v.SplitValues().Select(t => t.GetEnum<CodecType>(flexible: true)).ToList());
            CliUtils.ReadLine("Specify stream codec(s) as wildcard patterns:", v => f.Codecs = v.SplitValues().ToList());
            CliUtils.ReadLine("Specify stream title(s) as wildcard patterns (case-insensitive unless prefixed with '_'):", v => f.Titles = v.SplitValues().ToList());
            CliUtils.ReadLine("Specify stream language(s) as ISO 639 codes or names:", v => f.Langs = v.SplitValues().Select(LanguageUtils.GetLangByNameOrCode).ToList());
            CliUtils.ReadBool("Invert the filter to act as a blacklist instead of a whitelist:", v => f.Invert = v);

            return f;
        }

        public const string StreamNameVars = "[S_ID] = ID, [S_CODEC] = Codec, [S_TITLE] = Title, [S_LANG] = Language, [S_LANG2] = Language 2-char code, [S_LANG3] = Language 3-char code";

        // TODO: Implement somewhere lol
        public static string FillStreamVars(string text, Stream stream)
        {
            var lang = stream.LanguageParsed;

            return text
                .Replace("[S_ID]", stream.Index.ToString())
                .Replace("[S_LANG3]", lang == null ? "und" : lang.Name)
                .Replace("[S_LANG2]", lang == null ? "un" : lang.Iso6391)
                .Replace("[S_LANG]", lang == null ? "Unknown" : lang.Name)
                .Replace("[S_TITLE]", stream.Title)
                .Replace("[S_CODEC]", stream.Codec.Up());
        }
    }
}
