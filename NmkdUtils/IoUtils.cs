using Microsoft.VisualBasic.FileIO;
using System.Drawing;
using System.IO;
using System.Text;
using SearchOption = System.IO.SearchOption;
using static NmkdUtils.CodeUtils;

namespace NmkdUtils
{
    public class IoUtils
    {
        /// <summary> Checks if path is a file or directory </summary>
        /// <returns> true if the path is a directory, false if it is a file, null if it's neither (e.g. invalid or empty) </returns>
        public static bool? IsPathDirectory(string path)
        {
            if (path.IsEmpty())
                return null;

            if (Directory.Exists(path))
                return true;

            if (File.Exists(path))
                return false;

            return null;
        }

        /// <summary> Get file paths sorted by filename </summary>
        public static string[] GetFilesSorted(string path, bool recursive = false, string pattern = "*")
        {
            try
            {
                if (path.IsEmpty() || !Directory.Exists(path))
                    return [];

                SearchOption opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                return Directory.GetFiles(path, pattern, opt).OrderBy(x => Path.GetFileName(x)).ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetFilesSorted error: {ex.Message}", true);
                return [];
            }
        }

        /// <summary> Get files sorted by name </summary>
        public static FileInfo[] GetFileInfosSorted(string path, bool recursive = false, string pattern = "*")
        {
            try
            {
                if (path.IsEmpty() || !Directory.Exists(path))
                    return [];


                SearchOption opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var dir = new DirectoryInfo(path);
                return dir.GetFiles(pattern, opt).OrderBy(x => x.Name).ToArray();
            }
            catch (Exception ex)
            {
                Logger.Log(ex, $"GetFileInfosSorted error");
                return [];
            }
        }

        /// <summary> Get directories sorted by name (manual recursion to ignore inaccessible entries) </summary>
        public static DirectoryInfo[] GetDirInfosSorted(string root, bool recursive = false, string pattern = "*", bool noWarnings = true)
        {
            return GetDirInfosSorted(root, recursive ? int.MaxValue : 1, pattern, noWarnings);
        }

        /// <summary>
        /// Get directories in <paramref name="root"/>, filtered with wildcard <paramref name="pattern"/>, sorted by name, with a maximum recursion depth <paramref name="maxDepth"/> (0 = No recursion).<br/>
        /// </summary>
        public static DirectoryInfo[] GetDirInfosSorted(string root, int maxDepth = 128, string pattern = "*", bool noWarnings = true)
        {
            // Guard clause: if root is invalid or doesn't exist, return empty
            if (root.IsEmpty() || !Directory.Exists(root))
                return [];

            var directories = new List<DirectoryInfo>();

            // Use a stack to track directories along with their current depth
            var pending = new Stack<(string Path, int Depth)>();
            pending.Push((root, 0));

            while (pending.Count > 0)
            {
                var (currentDir, depth) = pending.Pop();

                try
                {
                    // Add current directory info to the list
                    if (depth > 0)
                        directories.Add(new DirectoryInfo(currentDir));

                    if (depth >= maxDepth)
                        continue;

                    // Add subdirectories to the stack
                    foreach (var directory in Directory.GetDirectories(currentDir, pattern))
                    {
                        pending.Push((directory, depth + 1));
                    }
                }
                catch (Exception ex)
                {
                    if (!noWarnings)
                    {
                        Logger.LogWrn($"{nameof(GetDirInfosSorted)} - {currentDir}: {ex.Message}");
                    }
                }
            }

            // Sort the directories by name and return
            return directories.OrderBy(d => d.Name).ToArray();
        }


        /// <summary> Sends a file to the recycle bin. Returns success bool. </summary>
        private static bool RecycleFile(string path, bool logEx = false)
            => Try(() => FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin), logEx: logEx);

        /// <summary> Sends a folder to the recycle bin. Returns success bool. </summary>
        public static bool RecycleDir(string path, bool logEx = false)
            => Try(() => FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin), logEx: logEx);

        /// <summary> Deletes a file (or sends it to recycle bin if <paramref name="recycle"/> is true). Only logs the file if <paramref name="dryRun"/> is true. </summary>
        public static void DeleteFile(string path, bool recycle = false, bool dryRun = false, Logger.Level logLvl = Logger.Level.Verbose)
        {
            if (!File.Exists(path))
                return;

            if (dryRun)
            {
                Logger.Log($"Would {(recycle ? "recycle" : "delete")} file {path}");
                return;
            }

            if (recycle && OsUtils.IsWindows)
            {
                Logger.Log($"Recycling file {path}", logLvl);
                RecycleFile(path);
            }
            else
            {
                Logger.Log($"Deleting file {path}", logLvl);
                File.Delete(path);
            }
        }

        /// <summary> Deletes a file or directory (or sends it to recycle bin if <paramref name="recycle"/> is true). Only logs the files that would be deleted if <paramref name="dryRun"/> is true. </summary>
        public static void Delete(string path, bool? ignoreExceptions = true, bool recycle = false, bool dryRun = false)
        {
            if (File.Exists(path))
            {
                DeleteFile(path, recycle, dryRun);
            }
            else if (Directory.Exists(path))
            {
                DeleteDirectory(path, ignoreExceptions, recycle, dryRun);
            }
            else
            {
                Logger.Log($"Does not exist: {path}", Logger.Level.Verbose);
                return;
            }
        }

        /// <summary>
        /// Deletes a directory and its contents or sends it to recycle bin. <br/>
        /// If <paramref name="ignoreExceptions"/> is true, exceptions are ignored fully, if null, they are only logged, if false, they are thrown. <br/>
        /// If <paramref name="dryRun"/> is true, no files are actually deleted, only logged.
        /// </summary>
        private static void DeleteDirectory(string path, bool? ignoreExceptions = true, bool recycle = false, bool dryRun = false)
        {
            if (dryRun)
            {
                Logger.Log($"Would {(recycle ? "recycle" : "delete")} directory {path}");
                return;
            }

            if (recycle)
            {
                RecycleDir(path, logEx: ignoreExceptions != false);
                return;
            }

            string[] files = Directory.GetFiles(path);
            string[] directories = Directory.GetDirectories(path);

            foreach (string file in files)
            {
                try
                {
                    DeleteFile(file, recycle, dryRun);
                }
                catch (Exception ex)
                {
                    if (ignoreExceptions == false)
                        throw;

                    Logger.Log($"Failed to delete {file}: {ex.Message.Remove($"'{file}' ")}", Logger.Level.Warning, condition: () => ignoreExceptions == false);
                }
            }

            foreach (string dir in directories)
            {
                DeleteDirectory(path: dir, ignoreExceptions: ignoreExceptions, recycle: recycle, dryRun: dryRun);
            }

            if (dryRun)
                return;

            try
            {
                Directory.Delete(path);
            }
            catch (Exception ex)
            {
                if (ignoreExceptions == false)
                    throw;

                Logger.Log($"Failed to delete {path}: {ex.Message.Remove($"'{path}' ")}", Logger.Level.Warning, condition: () => ignoreExceptions == null || !ex.Message.Contains("The directory is not empty"));
            }
        }

        /// <summary> More reliable alternative to File.ReadAllLines, should work for files that are being accessed from another process </summary>
        public static List<string> ReadTextLines(string path, bool ommitEmptyLines = false) => ReadTextFile(path).GetLines(ommitEmptyLines);

        /// <summary> More reliable alternative to File.ReadAllText, should work for files that are being accessed from another process </summary>
        public static string ReadTextFile(string path)
        {
            try
            {
                using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using StreamReader reader = new StreamReader(fileStream);
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Logger.Log(ex, $"Failed to read file '{path}'");
                return "";
            }
        }

        /// <summary> Transfer "Created" and "Last Modified" timestamps from <paramref name="pathSource"/> to <paramref name="pathTarget"/> </summary>
        public static DateTime? TransferFileTimestamps(string pathSource, string pathTarget, bool applyLastAccessedTime = false)
        {
            try
            {
                var source = new FileInfo(pathSource);
                var target = new FileInfo(pathTarget);
                target.CreationTime = source.CreationTime;
                target.LastWriteTime = source.LastWriteTime;

                if (applyLastAccessedTime)
                {
                    target.LastAccessTime = source.LastAccessTime;
                }

                return target.LastWriteTime;
            }
            catch (Exception ex)
            {
                Logger.Log(ex, $"Failed to transfer timestamps from {pathSource} to {pathTarget}");
                return null;
            }
        }

        /// <summary> Set "Created" and "Last Modified" timestamps of a <paramref name="file"/> to <paramref name="timestamp"/> </summary>
        public static DateTime? SetFileTimestamps(DateTime timestamp, string file)
        {
            try
            {
                var target = new FileInfo(file);
                target.CreationTime = timestamp;
                target.LastWriteTime = timestamp;
                return timestamp;
            }
            catch (Exception ex)
            {
                Logger.Log(ex, $"Failed to set timestamps on {file}");
                return null;
            }
        }

        /// <summary> A pseudo-hash built purely from the file metadata, thus not needing almost zero CPU or I/O resources no matter how big the file is.
        /// <br/>Largely reliable, unless a file was edited without a file size change and without LastWriteTime being changed, which is extremely unlikely. </summary>
        public static string GetPseudoHash(FileInfo file)
        {
            if (file == null || !file.Exists)
            {
                Logger.LogErr($"Failed to get PseudoHash! {(file == null ? "File is null" : $"File does not exist - '{file.FullName}'")}");
                return "";
            }

            return $"{file.FullName}|{file.Length}|{file.LastWriteTimeUtc.ToString("yyyyMMddHHmmss")}";
        }

        public static long GetDirSize(string path, bool recursive = true, IEnumerable<string>? patterns = null)
        {
            IEnumerable<string> files = Directory.EnumerateFiles(path, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            files = patterns == null ? files : files.Where(f => patterns.Any(pattern => f.MatchesWildcard(pattern)));

            long totalSize = 0;
            foreach (string file in files)
            {
                try
                {
                    totalSize += new FileInfo(file).Length;
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip files we can't read, no need to log this
                }
                catch (Exception ex)
                {
                    Logger.Log(ex, $"Error getting size of file '{file}'");
                }
            }

            return totalSize;
        }


        public static long? GetPathSize(string path, bool recursive = true)
        {
            try
            {
                bool isFile = File.Exists(path);
                return isFile ? new FileInfo(path).Length : GetDirSize(path, recursive);
            }
            catch
            {
                return null;
            }
        }

        /// <summary> Free disk space on the drive of <paramref name="path"/>. If it fails (e.g. invalid path passed), <paramref name="fallback"/> is returned instead. </summary>
        public static long GetFreeDiskSpace(string path, long fallback = -1, bool log = true)
        {
            try
            {
                DriveInfo drive = new DriveInfo(Path.GetPathRoot(path));
                return drive.AvailableFreeSpace;
            }
            catch (Exception ex)
            {
                if (log)
                {
                    Logger.Log(ex, $"Failed to get free disk space for '{path}'");
                }

                return -1;
            }
        }

        /// <summary> Free disk space on the drive of <paramref name="path"/> in GiB (or GB if <paramref name="divisor"/> is set to 1000). If it fails (e.g. invalid path passed), <paramref name="fallback"/> is returned instead. </summary>
        public static float GetFreeDiskSpaceGb(string path, float fallback = -1f, float divisor = 1024f, bool log = true)
        {
            float f = GetFreeDiskSpace(path, -1, log) / divisor / divisor / divisor;
            return f == -1 ? fallback : f;
        }

        public enum ExistMode
        {
            MustExist,
            MustNotExist,
            NotEmpty,
            Irrelevant
        }

        public enum PathType
        {
            File,
            Dir,
            Irrelevant
        }

        public static bool ValidateFilePath(string path, ExistMode existMode = ExistMode.Irrelevant)
        {
            if (existMode == ExistMode.MustExist && !File.Exists(path))
                return false;

            if (existMode == ExistMode.MustNotExist && File.Exists(path))
                return false;

            if (existMode == ExistMode.NotEmpty && (!File.Exists(path) || new FileInfo(path).Length == 0))
                return false;

            return true;
        }

        public static bool ValidateDirPath(string path, ExistMode existMode = ExistMode.Irrelevant)
        {
            if (existMode == ExistMode.MustExist && !Directory.Exists(path))
                return false;

            if (existMode == ExistMode.MustNotExist && Directory.Exists(path))
                return false;

            if (existMode == ExistMode.NotEmpty && (!Directory.Exists(path) || (Directory.GetFiles(path).Length == 0 && Directory.GetDirectories(path).Length == 0)))
                return false;

            return true;
        }

        public static bool ValidatePath(string path, PathType type = PathType.Irrelevant, ExistMode existMode = ExistMode.Irrelevant, bool validate = true, int maxLength = 220)
        {
            if (path.IsEmpty() || path.Trim().Length > maxLength)
                return false;

            if (validate && !PathIsValid(path))
                return false;

            bool? isDirectory = IsPathDirectory(path);

            if (isDirectory == null)
                return false;

            if (type == PathType.File && isDirectory == true)
                return false;

            if (type == PathType.Dir && isDirectory == false)
                return false;

            return isDirectory == true ? ValidateDirPath(path, existMode) : ValidateFilePath(path, existMode);
        }

        public static bool PathIsValid(string path, bool allowRelativePaths = true)
        {
            bool isValid = true;

            try
            {
                string fullPath = Path.GetFullPath(path);

                if (allowRelativePaths)
                {
                    isValid = Path.IsPathRooted(path);
                }
                else
                {
                    string root = Path.GetPathRoot(path);
                    isValid = $"{root}".Trim(['\\', '/']).IsNotEmpty();
                }
            }
            catch
            {
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// Writes a dictionary to a file in the format "key=value"
        /// </summary>
        public static void WriteDictToFile<TKey, TValue>(Dictionary<TKey, TValue> dict, string path)
        {
            var lines = dict.Select(kvp => $"{kvp.Key}={kvp.Value}").ToList();
            File.WriteAllLines(path, lines);
        }

        /// <summary>
        /// Reads a dictionary from a file in the format "key=value". Returns empty dictionary if the file does not exist.
        /// </summary>
        public static Dictionary<string, string> ReadDictFromFile(string path)
        {
            var dict = new Dictionary<string, string>();

            if (!File.Exists(path))
                return dict;

            try
            {
                var lines = File.ReadAllLines(path);

                foreach (string line in lines)
                {
                    if (line.Trim().StartsWith("#") || line.IsEmpty())
                        continue;

                    var split = line.Split('=');
                    if (split.Length == 2)
                    {
                        dict[split[0]] = split[1];
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogErr($"Failed to read dictionary from file '{path}': {ex.Message}");
            }

            return dict;
        }

        /// <summary>
        /// Returns a path with a number appended to it if the original path already exists. The format can be customized with <paramref name="pattern"/>, the counter starts at <paramref name="startingNum"/>.<br/>
        /// If <paramref name="alwaysUseSuffix"/> is true, the pattern will always be appended to the path even if it would've been valid.
        /// </summary>
        public static string GetAvailablePath(string path, string pattern = "_{0}", int startingNum = 1, int maxAttempts = 10000, bool alwaysUseSuffix = false)
        {
            if (path.IsEmpty() || (!alwaysUseSuffix && !File.Exists(path)))
                return path;

            string dir = Path.GetDirectoryName(path);
            string name = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);

            for (int i = startingNum; i < maxAttempts; i++)
            {
                string newPath = Path.Combine(dir, $"{name}{string.Format(pattern, i)}{ext}");

                if (!File.Exists(newPath))
                    return newPath;
            }

            Logger.LogWrn($"Failed to find an available path for '{path}' after {maxAttempts} attempts! Returning original path!");
            return path;
        }

        /// <summary> Loads an image from a file without locking it. </summary>
        public static Image LoadImage(string path)
        {
            using var img = Image.FromFile(path);
            return new Bitmap(img); // clones it in memory.
        }

        public static string GetProgram(string executable, bool allowPrompt = false) => GetProgram(executable, ref Settings.CommandPathsDict, allowPrompt);

        /// <summary> Gets the path to an executable by checking (in order): Config, environment (PATH), common installation directories. <br/>
        /// If not found, it can prompt the user for a path (<paramref name="allowInteraction"/>). This user-provided path gets saved in the config by default (<paramref name="allowWriteBack"/>) </summary>
        public static string GetProgram(string executable, ref Dictionary<string, string>? configDict, bool allowInteraction = false, bool allowWriteBack = true)
        {
            if (executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                executable = executable.Remove(executable.Length - 4);

            string GetExePath(string p, out string full) // If directory was specified, get the exe path
            {
                full = p.EndsWith($"{executable}.exe", StringComparison.OrdinalIgnoreCase) ? p : Path.Combine(p.TrimEnd(Path.DirectorySeparatorChar), $"{executable}.exe");
                return full;
            }

            // 1) Check config
            if (configDict != null && configDict.Get(executable, out var confPath, "") && File.Exists(confPath))
                return confPath;

            // 2) Check if in PATH
            string exeFromEnv = OsUtils.GetEnvExecutable(executable).FirstOrDefault("");
            if (File.Exists(exeFromEnv))
                return exeFromEnv;

            // 3) Check common default locations
            foreach (var path in new[] { "C:\\Program Files", "C:\\Program Files (x86)", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) })
            {
                // Get all subdirectories that could be the program's folder by checking first 3 characters while removing spaces
                foreach (var subDir in Directory.GetDirectories(path))
                {
                    if (!Path.GetFileName(subDir).Low().Replace(" ", "").StartsWith(executable.Replace(" ", "").Low().Substring(0, 3)))
                        continue;

                    string exePath = Path.Combine(subDir, $"{executable}.exe");

                    if (File.Exists(exePath))
                    {
                        if (allowWriteBack)
                        {
                            configDict.Set(executable, exePath);
                        }

                        Logger.Log($"Using {executable} from common install dir: {exePath}", Logger.Level.Debug);
                        return exePath;
                    }

                    Logger.Log($"Could not find {executable}.exe in {subDir}", Logger.Level.Debug);
                }
            }

            if (!allowInteraction)
            {
                return "";
            }

            Logger.Log($"Unable to find {executable}.", Logger.Level.Warning);

            // 4) Ask user for path
            string userCommand = CliUtils.ReadLine($"Enter path (or command) to {executable}:"); // Prompt user to input path

            if (File.Exists(GetExePath(userCommand, out string pInput)) || Path.Exists(OsUtils.GetEnvExecutable(userCommand).FirstOrDefault("")))
            {
                if (configDict == null)
                    return pInput;

                bool save = CliUtils.ReadLine($"Save this {executable} command to config? (Y/N)").Trim().Low() == "y";

                if (save)
                {
                    configDict[executable] = pInput;
                }

                return pInput;
            }

            return "";
        }

        /// <summary>
        /// Gets the file extension from a path or FileInfo object, removing the dot and lowercasing it if <paramref name="lower"/> and <paramref name="stripDot"/> are true, respectively.
        /// </summary>
        public static string GetExt(object file, bool lower = true, bool stripDot = true)
        {
            string ext = file is FileInfo fi ? fi.Extension : Path.GetExtension((string)file);

            if (stripDot)
                ext = ext.TrimStart('.');

            if (lower)
                ext = ext.ToLowerInvariant();

            return ext;
        }

        public static bool ExtIsInList(object file, params string[] validExts) => ExtIsInList(file, validExts.AsEnumerable());

        public static bool ExtIsInList(object file, IEnumerable<string> validExts)
        {
            validExts = validExts.Select(ext => ext.TrimStart('.').ToLowerInvariant()).ToArray();
            string ext = GetExt(file, lower: true, stripDot: true);
            return validExts.Contains(ext);
        }

        public static List<FileInfo> SortByPathHierarchy(List<FileInfo> files, bool dirsFirst = true) => files.OrderBy(f => f, new PathHierarchyComparer(dirsFirst)).ToList();

        private class PathHierarchyComparer : IComparer<FileSystemInfo>
        {
            private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;
            private readonly bool _foldersFirst;

            public PathHierarchyComparer(bool dirsFirst) { _foldersFirst = dirsFirst; }

            public int Compare(FileSystemInfo x, FileSystemInfo y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (x is null) return -1;
                if (y is null) return 1;

                var segA = x.FullName.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var segB = y.FullName.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                int minLen = Math.Min(segA.Length, segB.Length);
                for (int i = 0; i < minLen; i++)
                {
                    int cmp = Comparer.Compare(segA[i], segB[i]);
                    if (cmp != 0) return cmp;
                }

                if (_foldersFirst)
                {
                    bool aIsDir = x is DirectoryInfo;
                    bool bIsDir = y is DirectoryInfo;
                    if (aIsDir && !bIsDir) return -1;
                    if (!aIsDir && bIsDir) return 1;
                }

                if (segA.Length != segB.Length) return segA.Length.CompareTo(segB.Length);
                return Comparer.Compare(segA[^1], segB[^1]);
            }
        }

        private static void BuildFileTree(StringBuilder sb, DirectoryInfo dir, string indent, bool includeFiles)
        {
            var entries = dir.GetDirectories().Cast<FileSystemInfo>().OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToList();
            if (includeFiles)
            {
                entries.AddRange(dir.GetFiles().Cast<FileSystemInfo>().OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase));
            }
            for (int i = 0; i < entries.Count; i++)
            {
                bool isLast = i == entries.Count - 1;
                FileSystemInfo entry = entries[i];
                string pointer = isLast ? "└── " : "├── ";
                sb.AppendLine(indent + pointer + entry.Name);
                if (entry is DirectoryInfo subDir)
                {
                    string newIndent = indent + (isLast ? "    " : "│   ");
                    BuildFileTree(sb, subDir, newIndent, includeFiles);
                }
            }
        }

        public static bool IsTextFile(string path, int sampleSize = 4096)
        {
            byte[] buffer = new byte[sampleSize];
            int read;

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                read = fs.Read(buffer, 0, sampleSize);

            var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            return buffer.Take(read).None(b => b == 0) || Try(() => utf8.GetString(buffer, 0, read), logEx: false).IsNotEmpty(); // Check for null bytes, then try UTF-8 decode
        }
    }
}
