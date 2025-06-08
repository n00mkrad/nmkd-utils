using System.Globalization;
using Newtonsoft.Json.Linq;
using NmkdUtils.Media;

namespace NmkdUtils
{
    public class FfmpegUtils
    {
        public static int FfprobeCacheHits = 0;
        public static int FfprobeCacheMisses = 0;
        private static Dictionary<string, string> _ffprobeOutputCache = []; // Key = File hash, Value = Command output

        public static string GetFfprobeOutput(string path, string? executable = null, string args = "-v error -print_format json -show_format -show_streams -show_chapters", bool allowCaching = true)
        {
            if (CodeUtils.Assert(!File.Exists(path), () => Logger.LogErr($"File not found: {executable}")))
                return "";

            executable ??= IoUtils.GetProgram("ffprobe");

            string cacheKey = allowCaching ? new FileInfo(path).GetPseudoHash() + args : "";

            if (allowCaching && _ffprobeOutputCache.ContainsKey(cacheKey))
            {
                FfprobeCacheHits++;
                return _ffprobeOutputCache[cacheKey];
            }

            FfprobeCacheMisses++;
            var cmdResult = OsUtils.RunCommandShell($"{executable} {args} {path.Wrap()}");
            Logger.Log($"Ffprobe ExitCode: {cmdResult.ExitCode} ({FormatUtils.Time(cmdResult.RunTime)})", Logger.Level.Verbose);

            if (allowCaching && cmdResult.ExitCode == 0 && cmdResult.StdOut.Remove(['{', '}']).IsNotEmpty())
            {
                _ffprobeOutputCache[cacheKey] = cmdResult.StdOut;
            }

            return cmdResult.StdOut;
        }

        public static JObject GetFfprobeJson(string path, string? executable = null, string args = "-v error -show_format -show_streams -show_chapters", bool allowCaching = true)
        {
            string json = GetFfprobeOutput(path, executable, $"-print_format json {args}", allowCaching);

            if (json.IsEmpty())
                return [];

            try
            {
                return JObject.Parse(json);
            }
            catch (Exception ex)
            {
                Logger.Log(ex, $"Failed to parse ffprobe output");
                return [];
            }
        }

        /// <summary> Gets the decoded bitrate of a <paramref name="file"/> (or specific stream if <paramref name="streamIndex"/> is used) by demuxing it to NUL </summary>
        public static int GetKbps(MediaObject media, int streamIndex = -1)
        {
            return GetKbps(media.File.FullName, streamIndex);
        }

        /// <summary> Gets the decoded bitrate of a <paramref name="file"/> (or specific stream if <paramref name="streamIndex"/> is used) by demuxing it to NUL </summary>
        public static int GetKbps(string file, int streamIndex = -1)
        {
            string cmd = $"ffmpeg -loglevel panic -stats -y -i {file.Wrap()} -map 0{(streamIndex >= 0 ? $":{streamIndex}" : "")} -c copy -f matroska NUL";
            var result = OsUtils.Run(new OsUtils.RunConfig(cmd));
            int kbps = result.Output.Split("bitrate=").Last().Split('.').First().GetInt();

            if (kbps <= 0)
            {
                Logger.LogErr($"Failed to get bitrate from stream {streamIndex} of '{Path.GetFileName(file)}' (Got {0})");
            }

            return kbps;
        }

        public static TimeSpan GetTimespanFromFfprobe(Dictionary<string, string> tags, int fallbackMs = 0)
        {
            if (tags == null)
                return TimeSpan.FromMilliseconds(fallbackMs);

            string d = tags.Where(tags => tags.Key.StartsWith("DURATION")).Select(tags => tags.Value).FirstOrDefault();
            return GetTimespanFromFfprobe(d, fallbackMs);
        }

        public static TimeSpan GetTimespanFromFfprobe(string ffprobeDuration, int fallbackMs = 0)
        {
            var fallback = TimeSpan.FromMilliseconds(fallbackMs);

            if (ffprobeDuration.IsEmpty())
                return fallback;

            string durationStr = ffprobeDuration.Trim().Trunc(13, false);
            bool parsed = TimeSpan.TryParseExact(durationStr, @"hh\:mm\:ss\.FFFF", CultureInfo.InvariantCulture, out TimeSpan duration);

            if (CodeUtils.Assert(!parsed, () => Logger.Log($"Failed to parse '{durationStr}' to TimeSpan.", Logger.Level.Verbose)))
                return fallback;

            return duration;
        }

        /// <summary>
        /// Escapes a path for use in ffmpeg/ffprobe filters. <paramref name="wrap"/> wraps it in double quotes.
        /// </summary>
        public static string EscapePath(string path, bool wrap = true)
        {
            if (path.IsEmpty())
                return path;

            path = path.Trim().Replace(@"\", @"/").Replace(":", @"\\:");
            return wrap ? path.Wrap() : path;
        }
    }
}
