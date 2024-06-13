using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using static NmkdUtils.MediaData;
using System.Reflection;
using NmkdUtils.Structs;

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
            public string Title => Tags.Get("title", "");
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

            public int GetRelativeIndex(MediaObject parent)
            {
                if(Type == CodecType.Video) return parent.VidStreams.IndexOf((VideoStream)this);
                else if(Type == CodecType.Audio) return parent.AudStreams.IndexOf((AudioStream)this);
                else if(Type == CodecType.Subtitle) return parent.SubStreams.IndexOf((SubtitleStream)this);
                return -1;
            }

            [OnDeserialized]
            private void OnDeserialized(StreamingContext context)
            {
                Values = _values.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
            }

            public override string ToString()
            {
                string streamType = Type.ToString();
                string str = $"[{Index}] {streamType}:";
                var lang = LanguageUtils.GetLang(Language);

                List<string> infos = new();

                if (Type == CodecType.Video)
                {
                    var v = new VideoStream(this);
                    infos.Add(Aliases.GetFriendlyCodecName(Codec, v.Profile));
                    infos.Add(Title.IsNotEmpty() ? $"'{Title}'" : "");
                    infos.Add($"{v.Width}x{v.Height}");
                    infos.Add(v.PixFmt.Up());
                    infos.Add(v.DoviProfile >= 0 ? $"Dolby Vision (P{v.DoviProfile})" : "");
                    infos.Add(v.Hdr ? "HDR" : "");
                    infos.Add($"{v.Fps.GetString("0.###")} ({v.Fps}) FPS");
                    infos.Add(v.Values.Get("closed_captions") == "1" ? "Closed Captions" : "");
                    infos.Add(v.Values.Get("film_grain") == "1" ? "Film Grain" : "");
                    infos.Add($"SAR {v.Values.Get("sample_aspect_ratio", "?")}");
                    infos.Add($"DAR {v.Values.Get("display_aspect_ratio", "?")}");
                }
                else if (Type == CodecType.Audio)
                {
                    var a = new AudioStream(this);
                    infos.Add(Aliases.GetFriendlyCodecName(Codec, a.Profile));
                    infos.Add($"{(a.SampleRate / 1000).ToString("0.0###")} kHz");
                    infos.Add($"{a.Channels} Channels");
                    infos.Add(a.ChannelLayout.RemoveTextInParentheses().CapitalizeFirstChar());
                }
                else if (Type == CodecType.Subtitle)
                {
                    infos.Add(Aliases.GetFriendlyCodecName(Codec));
                    var s = new SubtitleStream(this);
                    infos.Add(s.Forced ? "Forced" : "");
                    infos.Add(s.Sdh ? "SDH" : "");
                }
                else if (Type == CodecType.Attachment)
                {
                    var att = new AttachmentStream(this);
                    infos.Add($"{att.Filename} ({att.MimeType})");
                }

                return $"{str} {string.Join(", ", infos.Where(s => s.IsNotEmpty()))}";
            }
        }

        public class VideoStream : Stream
        {
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

            public VideoStream(Stream s) { Index = s.Index; Type = s.Type; Codec = s.Codec; CodecLong = s.CodecLong; Values = s.Values; Tags = s.Tags; SideData = s.SideData; }
        }

        public class AudioStream : Stream
        {
            public string Profile => Values.Get("profile");
            public string SampleFmt => Values.Get("sample_fmt", "");
            public int SampleRate => Values.Get("sample_rate").GetInt();
            public int Channels => Values.Get("channels").GetInt();
            public string ChannelLayout => Values.Get("channel_layout", "");

            public AudioStream(Stream s) { Index = s.Index; Type = s.Type; Codec = s.Codec; CodecLong = s.CodecLong; Values = s.Values; Tags = s.Tags; }
        }

        public class SubtitleStream : Stream
        {
            public bool Forced => Disposition.Get("forced") == 1;
            public bool Sdh => Disposition.Get("hearing_impaired") == 1;

            public SubtitleStream(Stream s) { Index = s.Index; Type = s.Type; Codec = s.Codec; CodecLong = s.CodecLong; Values = s.Values; Tags = s.Tags; }
        }

        public class AttachmentStream : Stream
        {
            public string Filename => Tags.Get("filename");
            public string MimeType => Tags.Get("mimetype");

            public AttachmentStream(Stream s) { Index = s.Index; Type = s.Type; Codec = s.Codec; CodecLong = s.CodecLong; Values = s.Values; Tags = s.Tags; }
        }

        public class MediaObject
        {
            public JObject RawJson { get; set; }
            public FileInfo? File { get; set; }
            public List<Stream> Streams { get; set; } = new List<Stream>();
            public List<VideoStream> VidStreams => Streams.Where(s => s is VideoStream).Select(s => (VideoStream)s).ToList();
            public List<AudioStream> AudStreams => Streams.Where(s => s is AudioStream).Select(s => (AudioStream)s).ToList();
            public List<SubtitleStream> SubStreams => Streams.Where(s => s is SubtitleStream).Select(s => (SubtitleStream)s).ToList();
            public Format Format { get; set; } = new Format();
            [JsonIgnore]
            public bool HasStreams => Streams != null && Streams.Count > 0;

            public MediaObject() { }

            public MediaObject(FileInfo file)
            {
                File = file;
                Load(file.FullName);
            }

            public MediaObject(string path)
            {
                File = new FileInfo(path);
                Load(path);
            }

            private void Load(string path)
            {
                string json = FfmpegUtils.GetFfprobeOutputCached(path);
                try
                {
                    RawJson = JObject.Parse(json);
                    var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
                    var parsedData = JsonConvert.DeserializeObject<MediaObject>(json, settings);
                    Streams = parsedData.Streams.Select(CreateStream).ToList();
                    Format = parsedData.Format;
                }
                catch(Exception ex)
                {
                    Logger.Log(ex, "Error loading MediaFile");
                }
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
                    default:
                        return genericStream; // Return as basic stream if type does not match
                }
            }
        }
    }
}
