using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using static NmkdUtils.MediaData;
using System.Reflection;

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
            public int NbStreams { get; set; }
            public long Size { get; set; }
            public int BitRate { get; set; }
            public Dictionary<string, string> Tags { get; set; } = new();
            public string Title => Tags.Get("title", "");
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
            public string Title => Tags.Get("title", "");
            public string Language => Tags.Get("language", "");
            public bool Default => Disposition.Get("default") == 1;

            [JsonExtensionData]
            private IDictionary<string, JToken> _values;

            public Stream () { }

            [JsonConstructor]
            public Stream(int index, string codec_name, string codec_long_name, string codec_type, Dictionary<string, int> disposition, Dictionary<string, string> tags)
            {
                Index = index;
                Codec = codec_name;
                CodecLong = codec_long_name;
                Type = Enum.TryParse<CodecType>(codec_type, true, out var result) ? result : CodecType.Unknown;
                Disposition = disposition;
                Tags = tags;
            }

            [OnDeserialized]
            private void OnDeserialized(StreamingContext context)
            {
                Values = _values.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
            }
        }

        public class VideoStream : Stream
        {
            public int Width => Values.Get("width").GetInt();
            public int Height => Values.Get("height").GetInt();
            public string PixFmt => Values.Get("pix_fmt", "");
            public string Fps => Values.Get("r_frame_rate", "");
            public string AvgFps => Values.Get("avg_frame_rate", "");

            public VideoStream(Stream s)
            {
                Index = s.Index;
                Type = s.Type;
                Codec = s.Codec;
                CodecLong = s.CodecLong;
                Values = s.Values;
                Tags = s.Tags;
            }
        }

        public class AudioStream : Stream
        {
            public string Profile => Values.Get("profile");
            public string SampleFmt => Values.Get("sample_fmt", "");
            public int SampleRate => Values.Get("sample_rate").GetInt();
            public int Channels => Values.Get("channels").GetInt();
            public string ChannelLayout => Values.Get("channel_layout", "");

            public AudioStream(Stream s)
            {
                Index = s.Index;
                Type = s.Type;
                Codec = s.Codec;
                CodecLong = s.CodecLong;
                Values = s.Values;
                Tags = s.Tags;
            }
        }

        public class SubtitleStream : Stream
        {
            public bool Forced => Disposition.Get("forced") == 1;
            public bool Sdh => Disposition.Get("hearing_impaired") == 1;

            public SubtitleStream(Stream s)
            {
                Index = s.Index;
                Type = s.Type;
                Codec = s.Codec;
                CodecLong = s.CodecLong;
                Values = s.Values;
                Tags = s.Tags;
            }
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
                RawJson = JObject.Parse(json);
                var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
                var parsedData = JsonConvert.DeserializeObject<MediaObject>(json, settings);
                Streams = parsedData.Streams.Select(CreateStream).ToList();
                Format = parsedData.Format;
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

            public static string GetString(Stream s)
            {
                string t = s.Type.ToString();

                if (s.Type == CodecType.Audio)
                {
                    var a = (AudioStream)s;
                    return $"{t} Stream {s.Index}: {s.Codec.Up()} {s.Language.Up()} '{s.Title}' {a.SampleRate} Hz";
                }

                return $"{t} Stream {s.Index}: {s.Codec.Up()}";
            }
        }
    }
}
