using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NmkdUtils.Structs;
using System.Drawing;
using System.Globalization;
using System.Runtime.Serialization;
using static NmkdUtils.CodeUtils;
using static NmkdUtils.Media.MediaData;

namespace NmkdUtils.Media
{
    public class Stream
    {
        [JsonIgnore] public int Index = -1;
        [JsonIgnore] public CodecType Type = CodecType.Unknown;
        [JsonIgnore] public string Codec = "";
        [JsonIgnore] public string CodecLong = "";
        [JsonIgnore] public Dictionary<string, string> Values = [];
        [JsonIgnore] public Dictionary<string, int> Disposition = [];
        [JsonIgnore] public Dictionary<string, string> Tags = [];
        [JsonIgnore] public List<Dictionary<string, string>> SideData = [];
        [JsonIgnore] public TimeSpan Duration;
        [JsonIgnore] public TimeSpan StartTime;
        [JsonIgnore] public int KbpsDemuxed = 0;
        [JsonIgnore] public string Title = "";
        [JsonIgnore] public int Kbps;
        [JsonIgnore] public string Language = "";
        [JsonIgnore] public LanguageUtils.Language? LanguageParsed;
        [JsonIgnore] public DateTime? CreationTime;
        [JsonIgnore] public bool Default;
        [JsonIgnore] public string CodecFriendly = "";

        [JsonExtensionData]
        public IDictionary<string, JToken>? JValues = default!;

        public Stream() { }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            LoadValues();
        }

        public void LoadValues(Dictionary<string, JToken>? values = null)
        {
            var v = values ?? JValues;
            Values = v.Where(v => !v.Key.IsOneOf("tags", "disposition", "side_data_list")).ToDictionary(kvp => kvp.Key, kvp => $"{kvp.Value}");
            Tags = v.GetEntryAs<Dictionary<string, string>>("tags", []);
            Disposition = v.GetEntryAs<Dictionary<string, int>>("disposition", []);
            SideData = v.GetEntryAs<List<Dictionary<string, string>>>("side_data_list", []);
            Index = Values.Get("index").GetInt();
            Codec = Values.Get("codec_name");
            CodecLong = Values.Get("codec_long_name");
            Type = Enum.TryParse<CodecType>(Values.Get("codec_type"), true, out var result) ? result : CodecType.Unknown;
            Duration = Values.Get("duration", out var dur) ? TimeSpan.FromSeconds(dur.GetFloat()) : FfmpegUtils.GetTimespanFromFfprobe(Tags);
            StartTime = Values.Get("start_time", out var startTime) ? TimeSpan.FromSeconds(startTime.GetFloat()) : new TimeSpan();
            Title = Tags.GetStr("title");
            Kbps = (Values.GetStr("bit_rate").IsNotEmpty() ? Values.Get("bit_rate").GetInt() : Tags.Get("BPS").GetInt()) / 1000;
            Language = Tags.GetStr("language");
            LanguageParsed = LanguageUtils.GetLangByCode(Language);
            string creationTimeStr = Tags.GetStr("creation_time");
            CreationTime = creationTimeStr.IsEmpty() ? null : Try(() => DateTime.Parse(creationTimeStr, null, DateTimeStyles.RoundtripKind), errNote: $"Failed to parse media creation time {creationTimeStr}", fallback: (DateTime?)null);
            Default = Disposition.Get("default") == 1;
            CodecFriendly = Aliases.GetFriendlyCodecName(Codec);
        }

        public override string ToString() => Print();

        public int GetRelativeIndex(MediaObject parent)
        {
            if (Type == CodecType.Video) return parent.VidStreams.IndexOf((VideoStream)this);
            else if (Type == CodecType.Audio) return parent.AudStreams.IndexOf((AudioStream)this);
            else if (Type == CodecType.Subtitle) return parent.SubStreams.IndexOf((SubtitleStream)this);
            return -1;
        }

        public string Print(MediaObject? parentMedia = null, bool padCodec = false)
        {
            bool validParentMedia = parentMedia != null && parentMedia.Streams.Count > 0;
            int maxTitleChars = 120; // Max chars of a stream/format title to display, longer gets truncated
            int streamTypePad = validParentMedia ? parentMedia.Streams.Max(s => s.Type.ToString().Length) : 0;
            string streamType = Type.ToString().PadRight(streamTypePad);
            int indexPad = validParentMedia ? (parentMedia.Streams.Count - 1).ToString().Length : 1;
            string str = $"[{Index.ToString().PadLeft(indexPad)}] {streamType}:";
            var lang = LanguageUtils.GetLangByCode(Language);

            List<string> infos = new();

            int GetCodecPadding()
            {
                var codecStrings = new List<string>();

                foreach (var stream in parentMedia.Streams)
                {
                    string profile = "";
                    if (stream is VideoStream vStream) profile = vStream.Profile;
                    else if (stream is AudioStream aStream) profile = aStream.Profile;
                    codecStrings.Add(Aliases.GetFriendlyCodecName(stream.Codec, profile));
                }

                return codecStrings.Max(s => s.Length);
            }

            int codecPadding = padCodec && validParentMedia ? GetCodecPadding() : 0;

            string GetBitrateSize(int kbps, TimeSpan? duration, bool parentheses = true)
            {
                if (kbps <= 0 || duration == null || duration?.TotalSeconds <= 0)
                    return "";
                string s = $"{FormatUtils.FileSize(((double)(kbps / 8 * (duration?.TotalSeconds ?? 0d))).RoundToLong() * 1024)}";
                return parentheses ? $"({s})" : s;
            }

            string GetBitrateStr(Stream s)
            {
                int br = s.Kbps;
                int brDemux = s.KbpsDemuxed;
                TimeSpan? duration = null;
                if (s is VideoStream v)
                    duration = v.Duration;
                else if (s is AudioStream a)
                    duration = a.Duration;
                if (br > 0 && brDemux > 0)
                    return br.RatioTo(brDemux) < 1.1f ? FormatUtils.Media.Bitrate(brDemux * 1000) : $"{FormatUtils.Media.Bitrate(br * 1000)} (metadata), {FormatUtils.Media.Bitrate(brDemux * 1000)} (measured)";
                else if (br > 0 && brDemux <= 0)
                    return $"{FormatUtils.Media.Bitrate(br * 1000)} {GetBitrateSize(br, duration)}".Trim();
                else if (br <= 0 && brDemux > 0)
                    return $"{FormatUtils.Media.Bitrate(brDemux * 1000)} {GetBitrateSize(brDemux, duration)}".Trim();
                return "";
            }

            bool videoIsAttachment = Type == CodecType.Video && Tags.Get("filename").IsNotEmpty();

            if (this is VideoStream v && !videoIsAttachment)
            {
                infos.Add(Aliases.GetFriendlyCodecName(Codec, v.Profile).PadRight(codecPadding));
                infos.Add(Title.IsNotEmpty() ? $"'{Title.Trunc(maxTitleChars)}'" : "");
                infos.Add(v.Sar.IsNotEmpty() && v.Sar != "1:1" ? $"{v.Width}x{v.Height} -> {v.ScaledRes.ToStr()}" : $"{v.Width}x{v.Height}");
                infos.Add(GetBitrateStr(v));
                infos.Add($"{v.PixFmt.Up()} ({FormatUtils.Media.BitDepthFromPixFmt(v.PixFmt)}-bit)");
                infos.Add(v.Color.IsHdr ? "HDR" : "");
                infos.Add(v.DoviProfile > 0 ? $"Dolby Vision (P{v.DoviProfile})" : "");
                infos.Add(v.Hdr10Plus ? $"HDR10+" : "");
                infos.Add(v.SeemsVfr ? $"{v.AvgFps.Float.ToString("0.########")} FPS (Specified: {v.Fps})" : $"{v.Fps.GetString("0.###")}{(v.Fps.Denominator != 1 ? $" ({v.Fps})" : "")} FPS");
                infos.Add(v.Values.Get("closed_captions") == "1" ? "Closed Captions" : "");
                infos.Add(v.Values.Get("film_grain") == "1" ? "Film Grain" : "");
                infos.Add(v.Sar.IsNotEmpty() && v.Sar != "1:1" ? $"SAR {v.Sar}" : "");
                infos.Add(v.Dar.IsNotEmpty() ? $"DAR {v.Dar}" : "");
                infos.Add(v.Tags.Get("filename", out var fname, "") ? $"'{fname}'" : "");
            }
            else if (this is AudioStream a)
            {
                infos.Add(Aliases.GetFriendlyCodecName(Codec, a.Profile).PadRight(codecPadding));
                infos.Add(a.Profile.Contains("Atmos") ? "Dolby Atmos" : "");
                infos.Add(lang == null ? "" : lang.Name);
                infos.Add(Title.IsNotEmpty() ? $"'{Title.Trunc(maxTitleChars)}'" : "");
                infos.Add(GetBitrateStr(a));
                infos.Add($"{a.Channels} Channels");
                infos.Add(FormatUtils.Media.AudioLayout(a.ChannelLayout, FormatUtils.Media.LayoutStringFormat.Prettier));
                infos.Add($"{(a.SampleRate / 1000).ToString("0.0###")} kHz");
            }
            else if (this is SubtitleStream s)
            {
                infos.Add(Aliases.GetFriendlyCodecName(Codec).PadRight(codecPadding));
                infos.Add(lang == null ? "" : lang.Name);
                infos.Add(Title.IsNotEmpty() ? $"'{Title.Trunc(maxTitleChars)}'" : "");
                infos.Add(s.Frames > 0 ? $"{s.Frames} Frames" : "");
                infos.Add(s.Forced ? "Forced" : "");
                infos.Add(s.Sdh ? "SDH" : "");
            }
            else if (this is DataStream d)
            {
                infos.Add(d.HandlerName.IsEmpty() ? d.CodecTagString.Up() : $"{d.CodecTagString.Up()} ({d.HandlerName})");
            }
            else if (this is VideoStream or AttachmentStream || videoIsAttachment)
            {
                var at = (AttachmentStream)this;
                infos.Add(at.MimeType.IsEmpty() ? at.Filename : $"{at.Filename} ({at.MimeType})");
            }

            return $"{str} {string.Join(", ", infos.Where(s => s.IsNotEmpty()))}";
        }
    }

    public class VideoStream : Stream
    {
        public FrameData? FrameData { get; set; } = null;
        public ColorMasteringData? ColorData { get; set; } = null;
        public ColorInfo? Color = null;
        public int Width;
        public int Height;
        public int ResSum;
        public float Aspect;
        public Size ScaledRes;
        public string PixFmt = "";
        public string Sar = "1:1";
        public string Dar = "";
        public string Profile = "";
        public Fraction Fps;
        public Fraction AvgFps;
        public bool SeemsVfr;
        public int DoviProfile;
        public bool Hdr10Plus;
        public int Rotation;

        public VideoStream(Stream s)
        {
            LoadValues(new Dictionary<string, JToken>(s.JValues));
            // Color
            Color = new ColorInfo(Values.Get("color_space"), Values.Get("color_transfer"), Values.Get("color_primaries"), Values.Get("color_range", "tv") != "tv");
            PixFmt = Values.Get("pix_fmt");
            // Resolution/Scaling
            Width = Values.Get("width", "").GetInt();
            Height = Values.Get("height", "").GetInt();
            ResSum = Width + Height;
            GetScaledRes(out Size scaledRes);
            ScaledRes = scaledRes;
            Aspect = scaledRes.Width / (float)scaledRes.Height;
            Sar = Values.Get("sample_aspect_ratio", "1:1");
            Dar = Values.Get("display_aspect_ratio");
            // HDR
            DoviProfile = SideData?.FirstOrDefault(x => x.ContainsKey("dv_profile")).Get("dv_profile", "-1").GetInt() ?? -1;
            Hdr10Plus = FrameData?.SideData?.Any(e => $"{e["side_data_type"]}".Contains("HDR10+")) ?? false;
            // FPS & Other
            Fps = new Fraction(Values.Get("r_frame_rate", ""));
            AvgFps = new Fraction(Values.Get("avg_frame_rate", ""));
            SeemsVfr = Math.Abs(Fps.Float - AvgFps.Float) / ((Fps.Float + AvgFps.Float) / 2f) > 0.2f;
            Profile = Values.Get("profile");
            Rotation = SideData?.FirstOrDefault(x => x.ContainsKey("rotation")).Get("rotation", "0").GetInt() ?? 0;
        }

        // Scale using SAR (Sample Aspect Ratio) for non-square pixels
        private Size GetScaledRes(out Size size)
        {
            size = new Size(Width, Height);

            if (!Sar.SplitOut(":", out string[] split, targetParts: 2) || !int.TryParse(split[0], out int sampleWidth) || !int.TryParse(split[1], out int sampleHeight) || sampleWidth == sampleHeight)
                return size;

            bool wider = sampleWidth > sampleHeight; // Is the pixel wider than it is tall?
            double ratio = wider ? (double)sampleWidth / sampleHeight : (double)sampleHeight / sampleWidth;
            int newLength = wider ? (int)Math.Round(Width * ratio) : (int)Math.Round(Height * ratio);
            size = wider ? new Size(newLength, Height) : new Size(Width, newLength);
            return size;
        }
    }

    public class AudioStream : Stream
    {
        public string Profile = "";
        public string SampleFmt = "";
        public int SampleRate = 0;
        public int Channels = 0;
        public string ChannelLayout = "";
        public bool Atmos = false;

        public AudioStream(Stream s)
        {
            LoadValues(new Dictionary<string, JToken>(s.JValues));
            Profile = Values.Get("profile");
            SampleFmt = Values.Get("sample_fmt");
            SampleRate = Values.Get("sample_rate").GetInt();
            Channels = Values.Get("channels").GetInt();
            ChannelLayout = Values.Get("channel_layout");
            Atmos = Profile.Contains("+ Dolby Atmos");
        }
    }

    public class SubtitleStream : Stream
    {
        public int Frames;
        public bool ForcedFlag;
        public bool Forced;
        public bool SdhFlag;
        public bool Sdh;
        public bool TextBased;

        public SubtitleStream(Stream s)
        {
            LoadValues(new Dictionary<string, JToken>(s.JValues));
            Frames = Tags?.FirstOrDefault(t => t.Key.StartsWith("NUMBER_OF_FRAMES"), new()).Value.GetInt() ?? 0;
            ForcedFlag = Disposition.Get("forced") == 1;
            Forced = ForcedFlag || Title.ContainsCi("forced");
            SdhFlag = Disposition.Get("hearing_impaired") == 1;
            Sdh = SdhFlag || Title.ContainsAny(["sdh", "(cc)", "[cc]"], true) || Title.Low() == "cc" || Title.Low().EndsWith(" cc");
            TextBased = Data.LibAv.TextSubFormats.Contains(Codec);
        }
    }

    public class AttachmentStream : Stream
    {
        public string Filename = "";
        public string MimeType = "";

        public AttachmentStream(Stream s)
        {
            LoadValues(new Dictionary<string, JToken>(s.JValues));
            Filename = Tags.Get("filename");
            MimeType = Tags.Get("mimetype");
        }
    }

    public class DataStream : Stream
    {
        public string CodecTagString = "";
        public string HandlerName = "";

        public DataStream(Stream s)
        {
            LoadValues(new Dictionary<string, JToken>(s.JValues));
            CodecTagString = Values.Get("codec_tag_string");
            HandlerName = Tags.Get("handler_name");
        }
    }
}