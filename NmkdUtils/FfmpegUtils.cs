

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

            string hash = new FileInfo(path).GetPseudoHash();

            if (FfprobeOutputCache.ContainsKey(hash))
            {
                Logger.Log($"Cached: {path}", Logger.Level.Verbose);
                return FfprobeOutputCache[hash];
            }

            var cmdResult = OsUtils.RunCommandShell($"{executable} {args} {path.Wrap()}");
            Logger.Log($"Ffprobe ExitCode: {cmdResult.ExitCode} ({FormatUtils.Time(cmdResult.RunTime)})", Logger.Level.Verbose);

            if (cmdResult.ExitCode == 0 && cmdResult.StdOut.IsNotEmpty() && cmdResult.StdOut.Remove("{").Remove("}").IsNotEmpty())
            {
                FfprobeOutputCache[hash] = cmdResult.StdOut;
            }

            return cmdResult.StdOut;
        }
    }
}
