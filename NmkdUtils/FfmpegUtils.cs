

using System.Globalization;

namespace NmkdUtils
{
    public class FfmpegUtils
    {

        public static Dictionary<string, string> FfprobeOutputCache = new(); // Key = File hash, Value = Command output

        public static string GetFfprobeOutputCached(string path, string executable = "", string args = "-v error -print_format json -show_format -show_streams")
        {
            if(executable == "")
            {
                executable = Settings.FfprobePath;
            }

            string cacheKey = new FileInfo(path).GetPseudoHash() + args;

            if (FfprobeOutputCache.ContainsKey(cacheKey))
            {
                Logger.Log($"Cached: {path}", Logger.Level.Verbose);
                return FfprobeOutputCache[cacheKey];
            }

            var cmdResult = OsUtils.RunCommandShell($"{executable} {args} {path.Wrap()}");
            Logger.Log($"Ffprobe ExitCode: {cmdResult.ExitCode} ({FormatUtils.Time(cmdResult.RunTime)})", Logger.Level.Verbose);

            if (cmdResult.ExitCode == 0 && cmdResult.StdOut.IsNotEmpty() && cmdResult.StdOut.Remove("{").Remove("}").IsNotEmpty())
            {
                FfprobeOutputCache[cacheKey] = cmdResult.StdOut;
            }

            return cmdResult.StdOut;
        }

        /// <summary> Gets the decoded bitrate of a <paramref name="file"/> (or specific stream if <paramref name="streamIndex"/> is used) by demuxing it to NUL </summary>
        public static int GetKbps(MediaData.MediaObject media, int streamIndex = -1)
        {
            return GetKbps(media.File.FullName, streamIndex);
        }

        /// <summary> Gets the decoded bitrate of a <paramref name="file"/> (or specific stream if <paramref name="streamIndex"/> is used) by demuxing it to NUL </summary>
        public static int GetKbps(string file, int streamIndex = -1)
        {
            string cmd = $"ffmpeg -loglevel panic -stats -y -i {file.Wrap()} -map 0{(streamIndex >= 0 ? $":{streamIndex}" : "")} -c copy -f matroska NUL";
            var result = OsUtils.Run(new OsUtils.RunConfig(cmd));
            int kbps = result.Output.Split("bitrate=").Last().Split('.').First().GetInt();

            if(kbps <= 0)
            {
                Logger.LogErr($"Failed to get bitrate from stream {streamIndex} of '{Path.GetFileName(file)}' (Got {0})");
            }

            return kbps;
        }

        public static TimeSpan GetTimespanFromFfprobe(Dictionary<string, string> tags, int fallbackMs = 0)
        {
            if(tags == null)
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
    }
}
