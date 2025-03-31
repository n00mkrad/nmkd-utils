﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NmkdUtils.Structs;
using System.Drawing;
using System.Globalization;
using System.Runtime.Serialization;
using static NmkdUtils.Media.MediaData;

namespace NmkdUtils.Media
{
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
        [JsonIgnore] public int KbpsDemuxed { get; set; } = 0;
        [JsonIgnore] public string Title => Tags.Get("title", "");
        [JsonIgnore] public int Kbps => (Values.Get("bit_rate").IsNotEmpty() ? Values.Get("bit_rate", "0").GetInt() : Tags.Get("BPS", "0").GetInt()) / 1000;
        [JsonIgnore] public string Language => Tags.Get("language", "");
        [JsonIgnore] public LanguageUtils.Language? LanguageParsed => LanguageUtils.GetLangByCode(Language);
        [JsonIgnore] public DateTime CreationTime => DateTime.Parse(Tags.Get("creation_time", ""), null, DateTimeStyles.RoundtripKind);
        [JsonIgnore] public bool Default => Disposition.Get("default") == 1;
        [JsonIgnore] public string CodecFriendly => Aliases.GetFriendlyCodecName(Codec);

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

            if (Type == CodecType.Video)
            {
                var v = new VideoStream(this, ((VideoStream)this).FrameData);
                infos.Add(Aliases.GetFriendlyCodecName(Codec, v.Profile).PadRight(codecPadding));
                infos.Add(Title.IsNotEmpty() ? $"'{Title.Trunc(maxTitleChars)}'" : "");
                infos.Add(v.Sar.IsNotEmpty() && v.Sar != "1:1" ? $"{v.Width}x{v.Height} -> {v.ScaledRes.ToStr()}" : $"{v.Width}x{v.Height}");
                infos.Add(GetBitrateStr(v));
                infos.Add($"{v.PixFmt.Up()} ({FormatUtils.Media.BitDepthFromPixFmt(v.PixFmt)}-bit)");
                infos.Add(v.Hdr ? "HDR" : "");
                infos.Add(v.DoviProfile >= 0 ? $"Dolby Vision (P{v.DoviProfile})" : "");
                infos.Add(v.Hdr10Plus ? $"HDR10+" : "");
                infos.Add($"{v.Fps.GetString("0.###")}{(v.Fps.Denominator != 1 ? $" ({v.Fps})" : "")} FPS"); ;
                infos.Add(v.Values.Get("closed_captions") == "1" ? "Closed Captions" : "");
                infos.Add(v.Values.Get("film_grain") == "1" ? "Film Grain" : "");
                infos.Add(v.Sar.IsNotEmpty() && v.Sar != "1:1" ? $"SAR {v.Sar}" : "");
                infos.Add(v.Dar.IsNotEmpty() ? $"DAR {v.Dar}" : "");
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
        [JsonIgnore] public int Width => Values.Get("width", "").GetInt();
        [JsonIgnore] public int Height => Values.Get("height", "").GetInt();
        [JsonIgnore] public string PixFmt => Values.Get("pix_fmt", "");
        [JsonIgnore] public bool Hdr => ColorTransfer == "smpte2084" && ColorPrimaries == "bt2020";
        [JsonIgnore] public bool LimitedRange => Values.Get("color_range", "tv") == "tv";
        [JsonIgnore] public string ColorSpace => Values.Get("color_space", "");
        [JsonIgnore] public string ColorTransfer => Values.Get("color_transfer", "");
        [JsonIgnore] public string ColorPrimaries => Values.Get("color_primaries", "");
        [JsonIgnore] public string Sar => Values.Get("sample_aspect_ratio", "");
        [JsonIgnore] public string Dar => Values.Get("display_aspect_ratio", "");
        [JsonIgnore] public Size ScaledRes => GetScaledRes();
        [JsonIgnore] public Fraction Fps => new Fraction(Values.Get("r_frame_rate", ""));
        [JsonIgnore] public Fraction AvgFps => new Fraction(Values.Get("avg_frame_rate", ""));
        [JsonIgnore] public string Profile => Values.Get("profile", "");
        [JsonIgnore] public int DoviProfile => SideData == null ? -1 : SideData.Where(x => x.ContainsKey("dv_profile")).FirstOrDefault().Get("dv_profile", "-1").GetInt();
        // public int Rotation => SideData == null ? 0 : SideData.Where(x => x.ContainsKey("...")).FirstOrDefault().Get("...", "0").GetInt();
        [JsonIgnore] public bool Hdr10Plus => FrameData == null || FrameData.SideData == null ? false : FrameData.SideData.Any(item => item["side_data_type"] != null && item["side_data_type"].ToString().Contains("HDR10+"));
        [JsonIgnore] public TimeSpan Duration => FfmpegUtils.GetTimespanFromFfprobe(Tags);

        public VideoStream(Stream s, FrameData? fd = null, ColorMasteringData cd = null) { Index = s.Index; Type = s.Type; Codec = s.Codec; CodecLong = s.CodecLong; Values = s.Values; Tags = s.Tags; SideData = s.SideData; FrameData = fd; ColorData = cd; KbpsDemuxed = s.KbpsDemuxed; }

        // Scale using SAR (Sample Aspect Ratio) for non-square pixels
        private Size GetScaledRes()
        {
            if (!Sar.SplitOut(":", out string[] split, targetParts: 2) || !int.TryParse(split[0], out int sampleWidth) || !int.TryParse(split[1], out int sampleHeight) || sampleWidth == sampleHeight)
                return new Size(Width, Height);

            bool wider = sampleWidth > sampleHeight; // Is the pixel wider than it is tall?
            double ratio = wider ? (double)sampleWidth / sampleHeight : (double)sampleHeight / sampleWidth;
            int newLength = wider ? (int)Math.Round(Width * ratio) : (int)Math.Round(Height * ratio);
            return wider ? new Size(newLength, Height) : new Size(Width, newLength);
        }
    }

    public class AudioStream : Stream
    {
        [JsonIgnore] public string Profile => Values.Get("profile");
        [JsonIgnore] public string SampleFmt => Values.Get("sample_fmt", "");
        [JsonIgnore] public int SampleRate => Values.Get("sample_rate").GetInt();
        [JsonIgnore] public int Channels => Values.Get("channels").GetInt();
        [JsonIgnore] public string ChannelLayout => Values.Get("channel_layout", "");
        [JsonIgnore] public bool Atmos => Profile.Contains("+ Dolby Atmos");
        [JsonIgnore] public TimeSpan Duration => FfmpegUtils.GetTimespanFromFfprobe(Tags);

        public AudioStream(Stream s) { Index = s.Index; Type = s.Type; Codec = s.Codec; CodecLong = s.CodecLong; Values = s.Values; Tags = s.Tags; KbpsDemuxed = s.KbpsDemuxed; }
    }

    public class SubtitleStream : Stream
    {
        [JsonIgnore] public int Frames => Tags.FirstOrDefault(t => t.Key.StartsWith("NUMBER_OF_FRAMES"), new()).Value.GetInt(); // Use StartsWith to account for language tags, e.g. NUMBER_OF_FRAMES-eng
        [JsonIgnore] public bool ForcedFlag => Disposition.Get("forced") == 1;
        [JsonIgnore] public bool Forced => ForcedFlag || Title.Low().Contains("forced");
        [JsonIgnore] public bool SdhFlag => Disposition.Get("hearing_impaired") == 1;
        [JsonIgnore] public bool Sdh => Disposition.Get("hearing_impaired") == 1 || Title.ContainsAny(["sdh", "(cc)", "[cc]"], true) || Title.Low() == "cc" || Title.Low().EndsWith(" cc");
        [JsonIgnore] public bool TextBased => Data.LibAv.TextSubFormats.Contains(Codec);

        public SubtitleStream(Stream s) { Index = s.Index; Type = s.Type; Codec = s.Codec; CodecLong = s.CodecLong; Values = s.Values; Tags = s.Tags; }
    }

    public class AttachmentStream : Stream
    {
        [JsonIgnore] public string Filename => Tags.Get("filename");
        [JsonIgnore] public string MimeType => Tags.Get("mimetype");

        public AttachmentStream(Stream s) { Index = s.Index; Type = s.Type; Codec = s.Codec; CodecLong = s.CodecLong; Values = s.Values; Tags = s.Tags; }
    }

    public class DataStream : Stream
    {
        [JsonIgnore] public string CodecTagString => Values.Get("codec_tag_string");
        [JsonIgnore] public string HandlerName => Tags.Get("handler_name");

        public DataStream(Stream s) { Index = s.Index; Type = s.Type; Codec = s.Codec; CodecLong = s.CodecLong; Values = s.Values; Tags = s.Tags; }
    }
}