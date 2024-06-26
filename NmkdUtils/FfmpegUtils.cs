

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
            var result = OsUtils.RunCommandShell(new OsUtils.RunConfig(cmd));
            int kbps = result.Output.Split("bitrate=").Last().Split('.').First().GetInt();

            if(kbps <= 0)
            {
                Logger.LogErr($"Failed to get bitrate from stream {streamIndex} of '{Path.GetFileName(file)}' (Got {0})");
            }

            return kbps;
        }
    }
}
