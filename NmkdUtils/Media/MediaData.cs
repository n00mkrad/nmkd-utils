using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using NmkdUtils.Structs;

namespace NmkdUtils.Media
{
    public class MediaData
    {
        public enum CodecType
        {
            Audio, Video, Subtitle, Data, Attachment, Unknown
        }

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

            [JsonIgnore] public string Title => Tags.Get("title");
            [JsonIgnore] public TimeSpan Duration => TimeSpan.FromSeconds(DurationSecs);
            [JsonIgnore] public string DurationStr => FormatUtils.Time(Duration);

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
            public JArray SideData { get; set; }
            public Dictionary<string, string> Values { get; set; } = new();

            [JsonExtensionData]
            private IDictionary<string, JToken> _values;

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

        public class ColorMasteringData
        {
            public Fraction RedX { get; private set; }
            public Fraction RedY { get; private set; }
            public Fraction GreenX { get; private set; }
            public Fraction GreenY { get; private set; }
            public Fraction BlueX { get; private set; }
            public Fraction BlueY { get; private set; }
            public Fraction WhiteX { get; private set; }
            public Fraction WhiteY { get; private set; }
            public Fraction MinLuminance { get; private set; }
            public Fraction MaxLuminance { get; private set; }
            public int MaxContentLightLevel { get; private set; }
            public int AvgContentLightLevel { get; private set; }

            public ColorMasteringData(JArray frameSideData)
            {
                if (frameSideData == null)
                    return;

                var colorDict = frameSideData.Where(x => $"{x["side_data_type"]}" == "Mastering display metadata").FirstOrDefault();
                var lightDict = frameSideData.Where(x => $"{x["side_data_type"]}" == "Content light level metadata").FirstOrDefault();

                if (colorDict != null)
                {
                    var colorValues = colorDict.ToObject<Dictionary<string, string>>();
                    RedX = new Fraction(colorValues.Get("red_x"));
                    RedY = new Fraction(colorValues.Get("red_y"));
                    GreenX = new Fraction(colorValues.Get("green_x"));
                    GreenY = new Fraction(colorValues.Get("green_y"));
                    BlueX = new Fraction(colorValues.Get("blue_x"));
                    BlueY = new Fraction(colorValues.Get("blue_y"));
                    WhiteX = new Fraction(colorValues.Get("white_point_x"));
                    WhiteY = new Fraction(colorValues.Get("white_point_y"));
                    MinLuminance = new Fraction(colorValues.Get("min_luminance"));
                    MaxLuminance = new Fraction(colorValues.Get("max_luminance"));
                }

                if (lightDict != null)
                {
                    var lightValues = colorDict.ToObject<Dictionary<string, string>>();
                    MinLuminance = new Fraction(lightValues.Get("min_luminance"));
                    MaxLuminance = new Fraction(lightValues.Get("max_luminance"));
                }

                if (colorDict == null && lightDict == null)
                    return;

                Logger.Log($"Stream color data: Red {RedX} {RedY}, Green {GreenX} {GreenY}, Blue {BlueX} {BlueY}, White {WhiteX} {WhiteY}, Min Lum {MinLuminance}, Max Lum {MaxLuminance}", Logger.Level.Verbose);
            }
        }
    }
}
