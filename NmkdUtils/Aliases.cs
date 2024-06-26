

namespace NmkdUtils
{
    public class Aliases
    {
        /// <summary> Get friendly name for an ffmpeg codec string </summary>
        public static string GetFriendlyCodecName(string codecName, string profile = "")
        {
            if (codecName == null)
                return "";

            string n = codecName.Low();

            // Video
            if (n == "vc1") return "VC-1";
            if (n == "mjpeg") return "MJPEG";
            if (n == "mpeg4") return "MPEG-4";
            if (n == "mpeg2video") return "MPEG-2";
            if (n == "msmpeg4v3") return "MS MPEG-4 V3";
            if (n == "prores") return $"ProRes {profile}".Trim();
            if (n == "dnxhd") return "DNxHD";
            if (n == "binkvideo") return "Bink Video";
            if (n == "rawvideo") return "Raw Video";

            // Audio
            if (n == "eac3") return profile.Contains("+ Dolby Atmos") ? "EAC3 Atmos" : "EAC3";
            if (n == "dts") return profile.IsNotEmpty() ? profile : "DTS";
            if (n == "opus") return "Opus";
            if (n == "truehd") return profile.Contains("Atmos") ? "TrueHD Atmos" : "TrueHD";
            if (n == "wmav2") return "WMAV2";
            if (n == "wmapro") return "WMA Pro";
            if (n.StartsWith("pcm")) return GetPcmDescription(codecName);
            if (n.StartsWith("adpcm")) return codecName.Replace("_", " ").Up();
            if (n.StartsWith("binkaudio")) return $"Bink Audio {n.Split('_').Last().Up()}";

            // Subtitles
            if (n.StartsWith("hdmv_pgs")) return "HDMV PGS";
            if (n == "subrip") return "SRT";
            if (n == "dvd_subtitle") return "DVD Subtitles";
            if (n == "webvtt") return "WebVTT";
            if (n == "dvb_subtitle") return "DVB Subtitles";

            // Other
            if (n == "timed_id3") return "Timed ID3";
            if (n == "text") return "Text";
            if (n == "msrle") return "MS RLE";
            if (n == "dvb_teletext") return "DVB Teletext";

            return n.Up();
        }

        /// <summary> Get a somewhat readable description of various PCM formats, e.g. "pcm_s16le" </summary>
        private static string GetPcmDescription (string codecName)
        {
            string description = "PCM";
            int bits = codecName.GetInt();

            if (codecName.EndsWith("daud"))
            {
                description += " D-Cinema";
            }   

            if (bits > 0 && bits % 8 == 0)
            {
                description += $" {bits}-bit";
            }
            else
            {
                return codecName.Replace("_", " ").Up();
            }

            if (codecName.StartsWith("pcm_u"))
            {
                description += " Unsigned";
            }

            if (codecName.EndsWith("le") || codecName.EndsWith("le_planar"))
            {
                description += " LE";
            }

            if (codecName.EndsWith("be") || codecName.EndsWith("be_planar"))
            {
                description += " BE";
            }

            if (codecName.EndsWith("_planar"))
            {
                description += " Planar";
            }

            if (codecName.EndsWith("_sga"))
            {
                description += " SGA";
            }

            return description;
        }
    }
}
