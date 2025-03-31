﻿using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using static NmkdUtils.Media.MediaData;
using Stream = NmkdUtils.Media.Stream;

namespace NmkdUtils.Media
{
    public class MediaObject
    {
        public enum DemuxMode { All, StreamsWithoutKbpsMetadata, None }
        public JObject? RawJson { get; set; } = null;
        public FileInfo? File { get; set; }
        public List<Stream> Streams { get; set; } = [];
        public Format Format { get; set; } = new Format();
        [JsonIgnore] public List<VideoStream> VidStreams => Streams.Where(s => s is VideoStream).Select(s => (VideoStream)s).ToList();
        [JsonIgnore] public List<AudioStream> AudStreams => Streams.Where(s => s is AudioStream).Select(s => (AudioStream)s).ToList();
        [JsonIgnore] public List<SubtitleStream> SubStreams => Streams.Where(s => s is SubtitleStream).Select(s => (SubtitleStream)s).ToList();
        [JsonIgnore] public bool HasStreams => Streams != null && Streams.Count > 0;

        public MediaObject() { }

        /// <summary>
        /// Creates a new <see cref="MediaObject"/> from either a <see cref="FileInfo"/> or a <see cref="string"/>.<br/>
        /// Use <paramref name="loadFrameData"/> for frame data analysis (e.g. required to detect HDR10+), use <paramref name="demuxMode"/> for accurate bitrate measurements.<br/>
        /// Control demuxing progress prints with <paramref name="demuxPrints"/>.
        /// </summary>
        public MediaObject(object file, bool loadFrameData = false, DemuxMode demuxMode = DemuxMode.None, bool demuxPrints = false)
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
                Logger.LogErr($"{nameof(MediaObject)} can only be created from a {nameof(FileInfo)} or a {nameof(String)} object. You tried to create it from a {file.GetType()} object.");
                return;
            }

            string json = FfmpegUtils.GetFfprobeOutputCached(File.FullName);
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
                        if (demuxMode == DemuxMode.All || s.Kbps <= 0)
                        {
                            if (demuxPrints)
                            {
                                Logger.Log($"Demuxing stream #{s.Index} to get actual bitrate{(File.Length / 1024f / 1024f > 100f ? " - This could take a while" : "")}");
                            }

                            s.KbpsDemuxed = FfmpegUtils.GetKbps(File.FullName, s.Index);
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

        public void Print(bool format = true, bool colorizeStreams = true)
        {
            Logger.Log(File.Name);
            Logger.Log(new Logger.Entry(format ? $" {Format}" : Format) { CustomColor = format ? ConsoleColor.White : null });

            foreach(var s in Streams)
            {
                ConsoleColor? color = colorizeStreams ? s.Type switch
                {
                    CodecType.Video => ConsoleColor.Green,
                    CodecType.Audio => ConsoleColor.DarkCyan,
                    CodecType.Subtitle => ConsoleColor.DarkYellow,
                    CodecType.Data => ConsoleColor.DarkMagenta,
                    CodecType.Attachment => ConsoleColor.Magenta,
                    _ => null
                } : null;

                Logger.Log(new Logger.Entry(s.Print(format ? this : null)) { CustomColor = color }); // Pass this MediaObject to the Print method for relative stream indexes, padding, etc
            }
        }
    }
}
