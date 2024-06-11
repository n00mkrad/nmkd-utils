using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace NmkdUtils
{
    public class FfmpegUtils
    {

        public static Dictionary<string, string> FfprobeOutputCache = new(); // Key = File hash, Value = Command output

        public static string GetFfprobeOutputCached(string path, string executable = "ffprobe", string args = "-v panic -print_format json -show_format -show_streams")
        {
            string hash = new FileInfo(path).GetPseudoHash();

            if (FfprobeOutputCache.ContainsKey(hash))
            {
                Logger.Log($"Cached: {path}", Logger.Level.Verbose);
                return FfprobeOutputCache[hash];
            }

            string output = OsUtils.RunCommand($"{executable} {args} {path.Wrap()}");

            if (output.IsNotEmpty() && output.Remove("{").Remove("}").IsNotEmpty())
            {
                FfprobeOutputCache[hash] = output;
            }

            return output;
        }
    }
}
