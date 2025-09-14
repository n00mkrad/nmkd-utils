using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using NmkdUtils.Structs;

namespace NmkdUtils.Media
{
    public class MediaData
    {
        public enum CodecType { Audio, Video, Subtitle, Data, Attachment, Unknown }
        public enum Primaries { None, P3, Rec709, Rec2020, Unknown }
        public enum WhitePoint { None, D65, D63, D60, Unknown }

        public class Format
        {
            public string Filename { get; set; } = "";
            [JsonProperty("nb_streams")]
            public int StreamsCount { get; set; }
            [JsonProperty("size")]
            public long SizeBytes { get; set; }
            [JsonProperty("format_name")]
            public string FormatName { get; set; } = "";
            [JsonProperty("format_long_name")]
            public string FormatNameLong { get; set; } = "";
            [JsonProperty("bit_rate")]
            public int Bitrate { get; set; }
            [JsonProperty("duration")]
            public float DurationSecs { get; set; }
            [JsonProperty("probe_score")]
            public int FfprobeScore { get; set; }
            public Dictionary<string, string> Tags { get; set; } = [];

            public List<(float StartTime, float EndTime, string Title)> Chapters = [];
            public string Title = "";
            public TimeSpan Duration = TimeSpan.Zero;
            public string DurationStr = "";

            public void ParseAdditionalValues(JObject j)
            {
                Chapters = j["chapters"]?.Select(c => (c["start_time"].Value<float>(), c["end_time"].Value<float>(), $"{c["tags"]?["title"]}")).ToList() ?? [];
                Title = Tags.Get("title");
                Duration = TimeSpan.FromSeconds(DurationSecs);
                DurationStr = FormatUtils.Time(Duration);
            }

            public override string ToString()
            {
                string streams = $"{StreamsCount} Stream{(StreamsCount != 1 ? "s" : "")}";
                string size = FormatUtils.FileSize(SizeBytes);
                string br = FormatUtils.Media.Bitrate(Bitrate);
                string title = Tags.Get("title", out string t, "") ? $"'{t.Trunc(120)}', " : "";
                string format = $"{Path.GetExtension(Filename).TrimStart('.').Up()} ({FormatName.Replace(",", "/")})";
                return $"{streams}, {size}, {DurationStr}, {br}, {title}{format}";
            }
        }

        public class FrameData
        {
            [JsonProperty("side_data_list")]
            public JArray SideData { get; set; } = new();
            public Dictionary<string, string> Values { get; set; } = new();

            [JsonExtensionData]
            private IDictionary<string, JToken>? _values = default!;

            public FrameData() { }

            [JsonConstructor]
            public FrameData(JArray side_data_list)
            {
                SideData = side_data_list;
            }

            [OnDeserialized]
            private void OnDeserialized(StreamingContext context)
            {
                Values = _values.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
            }
        }

        public class ColorInfo // https://trac.ffmpeg.org/wiki/colorspace#color_primaries
        {
            public string Space { get; set; } = "";
            public string Transfer { get; set; } = "";
            public string Primaries { get; set; } = "";
            public bool FullRange { get; set; } = false;
            public string Range => FullRange ? "pc" : "tv";
            public bool IsHdr { get; set; } = false;

            public ColorInfo() { }
            public ColorInfo(string colSpace, string colTransfer, string primaries, bool fullRange = false, bool allowConversion = true)
            {
                Space = colSpace.IsEmpty() ? "bt709" : colSpace;
                Transfer = colTransfer.IsEmpty() ? "bt709" : colTransfer;
                Primaries = primaries.IsEmpty() ? "bt709" : primaries;
                FullRange = fullRange;
                IsHdr = Primaries == "bt2020" && (Transfer == "smpte2084" || Transfer == "arib-std-b67");

                if (!allowConversion)
                    return;

                Space = Space.Replace("bt470bg", "bt470").Replace("bt470m", "bt470"); // Parameter seemingly only accepts br470 but ffprobe readout can be bt470m or bt470bg
                Transfer = Transfer.Replace("bt470m", "gamma22").Replace("bt470bg", "gamma28"); // Parameter accepts gamma22/gamma28, but bt470m/bt470bg are used in the ffprobe output
            }

            public string FfmpegArgs => $" -colorspace {Space} -color_primaries {Primaries} -color_trc {Transfer} -color_range {Range}";
        }

        public class ColorMasteringData
        {
            public static readonly Dictionary<Primaries, (float[] R, float[] G, float[] B)> ColorPrimariesReference = new()
            {
                { Primaries.P3, (new[] { 0.680f, 0.320f }, new[] { 0.265f, 0.690f }, new[] { 0.150f, 0.060f }) },
                { Primaries.Rec709, (new[] { 0.640f, 0.330f }, new[] { 0.300f, 0.600f }, new[] { 0.150f, 0.060f }) },
                { Primaries.Rec2020, (new[] { 0.708f, 0.292f }, new[] { 0.170f, 0.797f }, new[] { 0.131f, 0.046f }) }
            };

            public static readonly Dictionary<WhitePoint, float[]> WhitePointReference = new()
            {
                { WhitePoint.D65, new[] { 0.3127f, 0.3290f } },
                { WhitePoint.D63, new[] { 0.3142f, 0.3516f } },
                { WhitePoint.D60, new[] { 0.32168f, 0.33767f } }
            };

            public Fraction[] RedRaw { get; private set; } = [];
            public Fraction[] GreenRaw { get; private set; } = [];
            public Fraction[] BlueRaw { get; private set; } = [];
            public Fraction[] WhitepointRaw { get; private set; } = [];
            public Fraction[] LuminanceRangeRaw { get; private set; } = [];
            public float[] Red { get; private set; } = [];
            public float[] Green { get; private set; } = [];
            public float[] Blue { get; private set; } = [];
            public float[] Whitepoint { get; private set; } = [];
            public float? MinLuminance { get; private set; } = null;
            public float? MaxLuminance { get; private set; } = null;
            public int? MaxCll { get; private set; } = null;
            public int? MaxFall { get; private set; } = null;
            public Primaries Primaries { get; private set; } = Primaries.None;
            public WhitePoint WhitePoint { get; private set; } = WhitePoint.None;

            public ColorMasteringData(JArray? frameSideData)
            {
                if (frameSideData == null)
                    return;

                var masteringDict = frameSideData.Where(sd => $"{sd["side_data_type"]}" == "Mastering display metadata").FirstOrDefault();
                var contentLightDict = frameSideData.Where(sd => $"{sd["side_data_type"]}" == "Content light level metadata").FirstOrDefault();

                if (masteringDict != null)
                {
                    var vals = masteringDict.ToObject<Dictionary<string, string>>();
                    RedRaw = [new Fraction(vals.Get("red_x")!), new Fraction(vals.Get("red_y")!)];
                    GreenRaw = [new Fraction(vals.Get("green_x")!), new Fraction(vals.Get("green_y")!)];
                    BlueRaw = [new Fraction(vals.Get("blue_x")!), new Fraction(vals.Get("blue_y")!)];
                    WhitepointRaw = [new Fraction(vals.Get("white_point_x")!), new Fraction(vals.Get("white_point_y")!)];
                    LuminanceRangeRaw = [new Fraction(vals.Get("min_luminance")!), new Fraction(vals.Get("max_luminance")!)];

                    Red = [RedRaw[0].Float, RedRaw[1].Float];
                    Green = [GreenRaw[0].Float, GreenRaw[1].Float];
                    Blue = [BlueRaw[0].Float, BlueRaw[1].Float];
                    Whitepoint = [WhitepointRaw[0].Float, WhitepointRaw[1].Float];
                    MinLuminance = LuminanceRangeRaw[0].Float;
                    MaxLuminance = LuminanceRangeRaw[1].Float;

                    Logger.Log($"[Mastering display data primaries] Red: {Red[0]:0.0##},{Red[1]:0.0##} - Green: {Green[0]:0.0##},{Green[1]:0.0##} - Blue: {Blue[0]:0.0##},{Blue[1]:0.0##}", Logger.Level.Verbose);
                    Logger.Log($"[Mastering display data luminance] Whitepoint: {Whitepoint[0]:0.0####},{Whitepoint[1]:0.0####} - Min Luminance: {MinLuminance:0.####} - Max Luminance: {MaxLuminance:0.#}", Logger.Level.Verbose);

                    // Detect primaries with an allowed error of 0.01
                    Primaries = ColorPrimariesReference.FirstOrDefault(r => r.Value.R[0].EqualsRoughly(Red[0]) && r.Value.R[1].EqualsRoughly(Red[1]) && r.Value.G[0].EqualsRoughly(Green[0]) && r.Value.G[1].EqualsRoughly(Green[1]) && r.Value.B[0].EqualsRoughly(Blue[0]) && r.Value.B[1].EqualsRoughly(Blue[1])).Key;
                    // Detect whitepoint with an allowed error of 0.001
                    WhitePoint = WhitePointReference.FirstOrDefault(r => r.Value[0].EqualsRoughly(Whitepoint[0]) && r.Value[1].EqualsRoughly(Whitepoint[1])).Key;

                    Logger.Log($"[Mastering display data analysis] Primaries: {Primaries} - Whitepoint: {WhitePoint}", Logger.Level.Verbose);
                }

                if (contentLightDict != null)
                {
                    var vals = contentLightDict.ToObject<Dictionary<string, string>>();
                    MaxCll = new Fraction(vals.Get("max_content", "1")!).Float.Round();
                    MaxFall = new Fraction(vals.Get("max_average", "1")!).Float.Round();

                    Logger.Log($"[Content light levels] MaxCLL: {MaxCll}, MaxFALL: {MaxFall}", Logger.Level.Verbose);
                }
            }
        }
    }
}
