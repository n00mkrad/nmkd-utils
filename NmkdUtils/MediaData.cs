using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using NmkdUtils.Structs;
using System.Globalization;

namespace NmkdUtils
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
            public Dictionary<string, string> Tags { get; set; } = new();

            [JsonIgnore] public string Title => Tags.Get("title");
            [JsonIgnore] public TimeSpan Duration => TimeSpan.FromSeconds(DurationSecs);
            [JsonIgnore] public string DurationStr => FormatUtils.Time(Duration);

            public override string ToString()
            {
                return $"{StreamsCount} Streams, {FormatUtils.FileSize(SizeBytes)}, {DurationStr}, {(Bitrate / 1024f).RoundToInt()} kbps, {FormatName.Replace(",", "/")}";
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

        public class Stream
        {
            public int Index { get; set; }
            public CodecType Type { get; set; }
            public string Codec { get; set; }
            public string CodecLong { get; set; }
            public Dictionary<string, string> Values { get; set; } = new();
            public Dictionary<string, int> Disposition { get; set; }
            public Dictionary<string, string> Tags { get; set; }
            public List<Dictionary<string, string>> SideData { get; set; }
            [JsonIgnore] public int BitrateDemuxed { get; set; } = 0;
            public string Title => Tags.Get("title", "");
            public int Bitrate => (Values.Get("bit_rate").IsNotEmpty() ? Values.Get("bit_rate", "0").GetInt() : Tags.Get("BPS", "0").GetInt()) / 1024;
            public string Language => Tags.Get("language", "");
            public bool Default => Disposition.Get("default") == 1;

            [JsonExtensionData]
            private IDictionary<string, JToken> _values;

            public Stream() { }

            [JsonConstructor]
            public Stream(int index, string codec_name, string codec_long_name, string codec_type, Dictionary<string, int> disposition, Dictionary<string, string> tags, List<Dictionary<string, string>> side_data_list)
            {
                Index = index;
                Codec = codec_name;
                CodecLong = codec_long_name;
                Type = Enum.TryParse<CodecType>(codec_type, true, out var result) ? result : CodecType.Unknown;
                Disposition = disposition;
                Tags = tags;
                SideData = side_data_list;
            }

            // TODO: Test
            public int GetRelativeIndex(MediaObject parent)
            {
                if (Type == CodecType.Video) return parent.VidStreams.IndexOf((VideoStream)this);
                else if (Type == CodecType.Audio) return parent.AudStreams.IndexOf((AudioStream)this);
                else if (Type == CodecType.Subtitle) return parent.SubStreams.IndexOf((SubtitleStream)this);
                return -1;
            }

            [OnDeserialized]
            private void OnDeserialized(StreamingContext context)
            {
                Values = _values.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
            }

            public override string ToString()
            {
                return Print();
            }

            public string Print(MediaObject? parentMedia = null, bool padCodec = false)
            {
                int maxTitleChars = 120; // Max chars of a stream/format title to display, longer gets truncated
                int streamTypePad = parentMedia == null ? 0 : parentMedia.Streams.Max(s => s.Type.ToString().Length);
                string streamType = Type.ToString().PadRight(streamTypePad);
                int indexPad = parentMedia == null ? 1 : (parentMedia.Streams.Count - 1).ToString().Length; // Count -1 to account for zero-indexing
                string str = $"[{Index.ToString().PadLeft(indexPad)}] {streamType}:";
                var lang = LanguageUtils.GetLangByCode(Language);

                List<string> infos = new();

                int GetCodecPadding()
                {
                    var codecStrings = new List<string>();

                    foreach (var stream in parentMedia.Streams)
                    {
                        string profile = "";
                        if (stream is VideoStream) profile = ((VideoStream)stream).Profile;
                        else if (stream is AudioStream) profile = ((AudioStream)stream).Profile;
                        codecStrings.Add(Aliases.GetFriendlyCodecName(stream.Codec, profile));
                    }

                    return codecStrings.Max(s => s.Length);
                }

                int codecPadding = padCodec && parentMedia != null ? GetCodecPadding() : 0;

                string GetBitrateSize(int kbps, TimeSpan? duration, bool parentheses = true)
                {
                    if (kbps <= 0 || duration == null || duration?.TotalSeconds <= 0)
                        return "";
                    string s = $"{FormatUtils.FileSize(((double)(kbps / 8 * duration?.TotalSeconds)).RoundToLong() * 1024)}";
                    return parentheses ? $"({s})" : s;
                }

                string GetBitrateStr(Stream s)
                {
                    int br = s.Bitrate;
                    int brDemux = s.BitrateDemuxed;
                    TimeSpan? duration = null;
                    if (s is VideoStream v)
                        duration = v.Duration;
                    else if (s is AudioStream a)
                        duration = a.Duration;
                    if (br > 0 && brDemux > 0)
                        return br.RatioTo(brDemux) < 1.1f ? $"{brDemux} kbps" : $"{br} kbps (metadata), {brDemux} kbps (measured) ({brDemux})";
                    else if (br > 0 && brDemux <= 0)
                        return $"{br} kbps {GetBitrateSize(br, duration)}".Trim();
                    else if (br <= 0 && brDemux > 0)
                        return $"{brDemux} kbps {GetBitrateSize(brDemux, duration)}".Trim();
                    return "";
                }

                if (Type == CodecType.Video)
                {
                    var v = new VideoStream(this, ((VideoStream)this).FrameData);
                    infos.Add(Aliases.GetFriendlyCodecName(Codec, v.Profile).PadRight(codecPadding));
                    infos.Add(Title.IsNotEmpty() ? $"'{Title.Trunc(maxTitleChars)}'" : "");
                    infos.Add($"{v.Width}x{v.Height}");
                    infos.Add(GetBitrateStr(v));
                    infos.Add($"{v.PixFmt.Up()} ({FormatUtils.Media.BitDepthFromPixFmt(v.PixFmt)}-bit)");
                    infos.Add(v.Hdr ? "HDR" : "");
                    infos.Add(v.DoviProfile >= 0 ? $"Dolby Vision (P{v.DoviProfile})" : "");
                    infos.Add(v.Hdr10Plus ? $"HDR10+" : "");
                    infos.Add($"{v.Fps.GetString("0.###")}{(v.Fps.Denominator != 1 ? $" ({v.Fps})" : "")} FPS"); ;
                    infos.Add(v.Values.Get("closed_captions") == "1" ? "Closed Captions" : "");
                    infos.Add(v.Values.Get("film_grain") == "1" ? "Film Grain" : "");
                    infos.Add(v.Values.Get("sample_aspect_ratio").IsNotEmpty() ? $"SAR {v.Values.Get("sample_aspect_ratio")}" : "");
                    infos.Add(v.Values.Get("display_aspect_ratio").IsNotEmpty() ? $"DAR {v.Values.Get("display_aspect_ratio")}" : "");
                    infos.Add(v.Tags.Get("filename").IsNotEmpty() ? $"'{v.Tags.Get("filename")}'" : "");
                }
                else if (Type == CodecType.Audio)
                {
                    var a = new AudioStream(this);
                    infos.Add(Aliases.GetFriendlyCodecName(Codec, a.Profile).PadRight(codecPadding));
                    infos.Add(a.Profile.Contains("Atmos") ? "Dolby Atmos" : "");
                    infos.Add(lang == null ? "" : lang.Name);
                    infos.Add(Title.IsNotEmpty() ? $"'{Title.Trunc(maxTitleChars)}'" : "");
                    infos.Add(GetBitrateStr(a));
                    infos.Add($"{a.Channels} Channels");
                    infos.Add(FormatUtils.Media.AudioLayout(a.ChannelLayout, FormatUtils.Media.LayoutStringFormat.Prettier));
                    infos.Add($"{(a.SampleRate / 1000).ToString("0.0###")} kHz");
                }
                else if (Type == CodecType.Subtitle)
                {
                    var s = new SubtitleStream(this);
                    infos.Add(Aliases.GetFriendlyCodecName(Codec).PadRight(codecPadding));
                    infos.Add(lang == null ? "" : lang.Name);
                    infos.Add(Title.IsNotEmpty() ? $"'{Title.Trunc(maxTitleChars)}'" : "");
                    infos.Add(s.Frames > 0 ? $"{s.Frames} Frames" : "");
                    infos.Add(s.Forced ? "Forced" : "");
                    infos.Add(s.Sdh ? "SDH" : "");
                }
                else if (Type == CodecType.Data)
                {
                    var d = new DataStream(this);
                    infos.Add(d.HandlerName.IsEmpty() ? d.CodecTagString.Up() : $"{d.CodecTagString.Up()} ({d.HandlerName})");
                }
                else if (Type == CodecType.Attachment)
                {
                    var at = new AttachmentStream(this);
                    int filenamePad = parentMedia == null ? 0 : parentMedia.Streams.Where(s => s.Type == CodecType.Attachment).Max(s => ((AttachmentStream)s).Filename.Length);
                    infos.Add(at.MimeType.IsEmpty() ? at.Filename : $"{at.Filename.PadRight(filenamePad)} ({at.MimeType})");
                }

                return $"{str} {string.Join(", ", infos.Where(s => s.IsNotEmpty()))}";
            }
        }

        public class VideoStream : Stream
        {
            public FrameData? FrameData { get; set; } = null;
            public ColorMasteringData? ColorData { get; set; } = null;
            public int Width => Values.Get("width").GetInt();
            public int Height => Values.Get("height").GetInt();
            public string PixFmt => Values.Get("pix_fmt", "");
            public bool Hdr => ColorTransfer == "smpte2084" && ColorPrimaries == "bt2020";
            public bool LimitedRange => Values.Get("color_range", "tv") == "tv";
            public string ColorSpace => Values.Get("color_space", "");
            public string ColorTransfer => Values.Get("color_transfer", "");
            public string ColorPrimaries => Values.Get("color_primaries", "");
            public Fraction Fps => new Fraction(Values.Get("r_frame_rate", ""));
            public Fraction AvgFps => new Fraction(Values.Get("avg_frame_rate", ""));
            public string Profile => Values.Get("profile");
            public int DoviProfile => SideData == null ? -1 : SideData.Where(x => x.ContainsKey("dv_profile")).FirstOrDefault().Get("dv_profile", "-1").GetInt();
            public bool Hdr10Plus => (FrameData == null || FrameData.SideData == null) ? false : FrameData.SideData.Any(item => item["side_data_type"] != null && item["side_data_type"].ToString().Contains("HDR10+"));
            public TimeSpan Duration => FfmpegUtils.GetTimespanFromFfprobe(Tags);

            public VideoStream(Stream s, FrameData? fd = null, ColorMasteringData cd = null) { Index = s.Index; Type = s.Type; Codec = s.Codec; CodecLong = s.CodecLong; Values = s.Values; Tags = s.Tags; SideData = s.SideData; FrameData = fd; ColorData = cd; BitrateDemuxed = s.BitrateDemuxed; }
        }

        public class AudioStream : Stream
        {
            public string Profile => Values.Get("profile");
            public string SampleFmt => Values.Get("sample_fmt", "");
            public int SampleRate => Values.Get("sample_rate").GetInt();
            public int Channels => Values.Get("channels").GetInt();
            public string ChannelLayout => Values.Get("channel_layout", "");
            public bool Atmos => Profile.Contains("+ Dolby Atmos");
            public TimeSpan Duration => FfmpegUtils.GetTimespanFromFfprobe(Tags);

            public AudioStream(Stream s) { Index = s.Index; Type = s.Type; Codec = s.Codec; CodecLong = s.CodecLong; Values = s.Values; Tags = s.Tags; BitrateDemuxed = s.BitrateDemuxed; }
        }

        public class SubtitleStream : Stream
        {
            public int Frames => GetNumberOfFrames();
            public bool Forced => Disposition.Get("forced") == 1;
            public bool Sdh => Disposition.Get("hearing_impaired") == 1;

            public SubtitleStream(Stream s) { Index = s.Index; Type = s.Type; Codec = s.Codec; CodecLong = s.CodecLong; Values = s.Values; Tags = s.Tags; }

            private int GetNumberOfFrames()
            {
                string s = Tags.Get("NUMBER_OF_FRAMES");

                if (s.IsNotEmpty())
                    return s.GetInt();

                return Tags.Get("NUMBER_OF_FRAMES-eng").GetInt();
            }
        }

        public class AttachmentStream : Stream
        {
            public string Filename => Tags.Get("filename");
            public string MimeType => Tags.Get("mimetype");

            public AttachmentStream(Stream s) { Index = s.Index; Type = s.Type; Codec = s.Codec; CodecLong = s.CodecLong; Values = s.Values; Tags = s.Tags; }
        }

        public class DataStream : Stream
        {
            public string CodecTagString => Values.Get("codec_tag_string");
            public string HandlerName => Tags.Get("handler_name");

            public DataStream(Stream s) { Index = s.Index; Type = s.Type; Codec = s.Codec; CodecLong = s.CodecLong; Values = s.Values; Tags = s.Tags; }
        }

        public class MediaObject
        {
            public enum DemuxMode { All, StreamsWithoutKbpsMetadata, None }
            public JObject RawJson { get; set; }
            public FileInfo? File { get; set; }
            public List<Stream> Streams { get; set; } = new List<Stream>();
            public Format Format { get; set; } = new Format();
            [JsonIgnore] public List<VideoStream> VidStreams => Streams.Where(s => s is VideoStream).Select(s => (VideoStream)s).ToList();
            [JsonIgnore] public List<AudioStream> AudStreams => Streams.Where(s => s is AudioStream).Select(s => (AudioStream)s).ToList();
            [JsonIgnore] public List<SubtitleStream> SubStreams => Streams.Where(s => s is SubtitleStream).Select(s => (SubtitleStream)s).ToList();
            [JsonIgnore] public bool HasStreams => Streams != null && Streams.Count > 0;

            public MediaObject() { }

            /// <summary> Create a new <see cref="MediaObject"/> from either a <see cref="FileInfo"/> or a <see cref="String"/>. 
            /// Use <paramref name="loadFrameData"/> for frame data analysis (e.g. required to detect HDR10+), use <paramref name="demuxMode"/> for accurate bitrate measurements </summary>
            public MediaObject(object file, bool loadFrameData = false, DemuxMode demuxMode = DemuxMode.None)
            {
                if (file is FileInfo)
                {
                    File = (FileInfo)file;
                }
                else if (file is string)
                {
                    File = new FileInfo((string)file);
                }
                else
                {
                    Logger.LogErr($"{nameof(MediaObject)} can only be created from a {nameof(FileInfo)} or a {nameof(String)} object.");
                    return;
                }

                Load(File.FullName, loadFrameData, demuxMode);
            }

            private void Load(string path, bool loadFrameData, DemuxMode demuxMode)
            {
                string json = FfmpegUtils.GetFfprobeOutputCached(path);
                try
                {
                    RawJson = JObject.Parse(json);
                    var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
                    var parsedData = JsonConvert.DeserializeObject<MediaObject>(json, settings);
                    Streams = parsedData.Streams.Select(CreateStream).ToList();
                    Format = parsedData.Format;

                    if (loadFrameData && VidStreams.Any())
                    {
                        AnalyzeFrameData(VidStreams.First());
                    }

                    if (demuxMode != DemuxMode.None)
                    {
                        foreach (var s in Streams.Where(s => new[] { CodecType.Video, CodecType.Audio }.Contains(s.Type)))
                        {
                            if (demuxMode == DemuxMode.All || s.Bitrate <= 0)
                            {
                                s.BitrateDemuxed = FfmpegUtils.GetKbps(File.FullName, s.Index);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex, "Error loading MediaFile");
                }
            }

            public void AnalyzeFrameData(VideoStream v)
            {
                string json = FfmpegUtils.GetFfprobeOutputCached(File.FullName, args: "-v error -show_frames -read_intervals \"%+#1\" -select_streams v:0 -print_format json");
                json = JObject.Parse(json)["frames"].First().ToString();
                var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
                v.FrameData = JsonConvert.DeserializeObject<FrameData>(json, settings);
                v.ColorData = new ColorMasteringData(v.FrameData.SideData);
            }

            private static Stream CreateStream(Stream genericStream)
            {
                switch (genericStream.Type)
                {
                    case CodecType.Video:
                        return new VideoStream(genericStream);
                    case CodecType.Audio:
                        return new AudioStream(genericStream);
                    case CodecType.Subtitle:
                        return new SubtitleStream(genericStream);
                    case CodecType.Data:
                        return new DataStream(genericStream);
                    case CodecType.Attachment:
                        return new AttachmentStream(genericStream);
                    default:
                        return genericStream; // Return as basic stream if type does not match
                }
            }

            public void Print(bool format = true)
            {
                Logger.Log(File.Name);
                Logger.Log(Format);
                Streams.ForEach(s => Logger.Log(s.Print(format ? this : null))); // Pass this MediaObject to the Print method for relative stream indexes, padding, etc
            }
        }
    }
}
