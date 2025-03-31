

namespace NmkdUtils
{
    public class PathUtils
    {
        private static string _exePath = "";

        /// <summary> Full path to the executable file </summary>
        public static string ExePath
        {
            get
            {
                if (_exePath.IsEmpty()) _exePath = $"{Environment.ProcessPath}";
                return _exePath;
            }
        }

        /// <summary> Name of the executable without extension </summary>
        public static string ExeName => AppDomain.CurrentDomain.FriendlyName;

        private static string _exeDir = "";

        /// <summary> Directory of the executable </summary>
        public static string ExeDir
        {
            get
            {
                if (_exeDir.IsEmpty()) _exeDir = $"{new FileInfo(ExePath).DirectoryName}";
                return _exeDir;
            }
        }

        private static string _exeFileName = "";
        /// <summary> Name of the executable file including extension </summary>
        public static string ExeFileName
        {
            get
            {
                if (_exeFileName.IsEmpty()) _exeFileName = new FileInfo(ExePath).Name;
                return _exeFileName;
            }
        }

        public enum CommonDir { Cache, Logs, Temp };

        public static string GetCommonSubdir(CommonDir subdir)
        {
            return GetAppSubdir(subdir.ToString());
        }

        public static string GetAppSubdir(string subdir, bool create = true)
        {
            string dir = Path.Combine(ExeDir, subdir);
            return create ? Directory.CreateDirectory(Path.Combine(ExeDir, subdir)).FullName : dir;
        }
    }
}
