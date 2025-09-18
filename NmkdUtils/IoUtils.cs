using Microsoft.VisualBasic.FileIO;
using NmkdUtils.Classes;
using System.Drawing;
using System.Text;
using static NmkdUtils.CodeUtils;
using SearchOption = System.IO.SearchOption;

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

        public static List<string> GetPathParts(string path, bool skipFirst = false, bool skipLast = false)
        {
            if (path.IsEmpty())
                return [];
            path = path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/'); // Normalize to forward slashes
            var parts = path.Split('/').Where(s => s.IsNotEmpty()).ToList();
            if (skipFirst && parts.Count > 0)
                parts.RemoveAt(0);
            if (skipLast && parts.Count > 0)
                parts.RemoveAt(parts.Count - 1);
            return parts;
        }

        /// <summary> Sorts file or directory paths. Sorting by size is not supported. </summary>
        public static IEnumerable<string> SortPaths(IEnumerable<string> paths, Enums.Sort mode = Enums.Sort.AToZ)
        {
            if (mode == Enums.Sort.None) return paths;
            if (mode == Enums.Sort.AToZ) return paths.OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase);
            if (mode == Enums.Sort.ZToA) return paths.OrderByDescending(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase);
            if (mode == Enums.Sort.Newest) return paths.OrderByDescending(p => new PathInfo(p).Fsi.LastWriteTime);
            if (mode == Enums.Sort.Oldest) return paths.OrderBy(p => new PathInfo(p).Fsi.LastWriteTime);
            if (mode == Enums.Sort.Biggest) return paths.OrderByDescending(p => new PathInfo(p).Size);
            if (mode == Enums.Sort.Smallest) return paths.OrderBy(p => new PathInfo(p).Size);
            if (mode == Enums.Sort.Longest) return paths.OrderByDescending(p => p.Length);
            if (mode == Enums.Sort.Shortest) return paths.OrderBy(p => p.Length);
            if (mode == Enums.Sort.Shallowest) return paths.OrderBy(p => GetPathParts(p).Count);
            if (mode == Enums.Sort.Deepest) return paths.OrderByDescending(p => GetPathParts(p).Count);
            return paths;
        }

        /// <summary>
        /// Get files as a sorted list using <paramref name="sort"/> and optionally filtered using <paramref name="pattern"/>. <br/>
        /// When <paramref name="recursive"/> is true, <paramref name="maxDepth"/> can be used to limit how deep in the directory structure to search (0 = current directory only).
        /// </summary>
        public static List<string> GetFilePaths(string path, bool recursive = false, string pattern = "*", Enums.Sort sort = Enums.Sort.AToZ, int maxDepth = int.MaxValue)
        {
            try
            {
                if (path.IsEmpty() || !Directory.Exists(path))
                    return [];

                recursive = recursive || maxDepth < int.MaxValue; // A specified maxDepth implies recursion
                SearchOption opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                List<string> paths = SortPaths(Directory.GetFiles(path, pattern, opt), sort).ToList();
                int rootDepth = GetPathParts(Path.GetFullPath(path), false).Count;
                paths = paths.Where(p => (GetPathParts(p, true).Count - rootDepth) <= maxDepth).ToList();
                return paths;
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "Failed to get files");
                return [];
            }
        }

        /// <inheritdoc cref="GetFilePaths(string, bool, string, Enums.Sort, int)"/>
        public static List<FileInfo> GetFiles(string path, bool recursive = false, string pattern = "*", Enums.Sort sort = Enums.Sort.AToZ, int maxDepth = int.MaxValue)
            => GetFilePaths(path, recursive, pattern, sort, maxDepth).Select(p => Try(() => new FileInfo(p))).Where(fi => fi != null).ToList();

        /// <summary>
        /// Get directories as a sorted list using <paramref name="sort"/> and optionally filtered using <paramref name="pattern"/>. <br/>
        /// When <paramref name="recursive"/> is true, <paramref name="maxDepth"/> can be used to limit how deep in the directory structure to search (0 = current directory only).
        /// </summary>
        public static List<string> GetDirPaths(string path, bool recursive = false, string pattern = "*", Enums.Sort sort = Enums.Sort.AToZ, int maxDepth = int.MaxValue)
        {
            recursive = recursive || maxDepth < int.MaxValue; // A specified maxDepth implies recursion

            try
            {
                if (path.IsEmpty() || !Directory.Exists(path))
                    return [];

                if (!recursive)
                    return SortPaths(Directory.GetDirectories(path, pattern, SearchOption.TopDirectoryOnly), sort).ToList();

                var paths = new List<string>();
                var pending = new Stack<(string Path, int Depth)>();
                pending.Push((path, 0));

                while (pending.Count > 0)
                {
                    var (currentDir, depth) = pending.Pop();

                    if (depth > maxDepth)
                        continue;

                    try
                    {
                        foreach (var subDir in Directory.GetDirectories(currentDir, pattern))
                        {
                            paths.Add(subDir);
                            if (depth < maxDepth)
                            {
                                pending.Push((subDir, depth + 1));
                            }
                        }
                    }
                    catch (UnauthorizedAccessException) { /* Ignore and continue */ }
                }

                return SortPaths(paths, sort).ToList();
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "Failed to get directories");
                return [];
            }
        }

        /// <inheritdoc cref="GetDirPaths(string, bool, string, Enums.Sort, int)"/>
        public static List<DirectoryInfo> GetDirs(string path, bool recursive = false, string pattern = "*", Enums.Sort sort = Enums.Sort.AToZ, int maxDepth = int.MaxValue)
            => GetDirPaths(path, recursive, pattern, sort, maxDepth).Select(p => Try(() => new DirectoryInfo(p))).Where(di => di != null).ToList();

        /// <summary> Sends a file to the recycle bin. Returns success bool. </summary>
        private static bool RecycleFile(string path, bool logEx = false)
            => Try(() => RecycleFileBackground(path), logEx: logEx);

        /// <summary> Sends a folder to the recycle bin. Returns success bool. </summary>
        public static bool RecycleDir(string path, bool logEx = false)
            => Try(() => RecycleDirBackground(path), logEx: logEx);

        private static void RecycleFileBackground(string path)
            => Jobs.Fire(() => FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin));

        private static void RecycleDirBackground(string path)
            => Jobs.Fire(() => FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin));

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

        /// <summary>
        /// Deletes a file or directory or sends it to recycle bin. <br/>
        /// If <paramref name="ignoreExceptions"/> is true, exceptions are ignored fully, if null, they are only logged, if false, they are thrown. <br/>
        /// If <paramref name="dryRun"/> is true, no files are actually deleted, only logged.
        /// </summary>
        public static void Delete(string path, bool? ignoreExceptions = true, bool recycle = false, bool dryRun = false)
        {
            if (File.Exists(path))
            {
                Try(() => DeleteFile(path, recycle, dryRun));
            }
            else if (Directory.Exists(path))
            {
                Try(() => DeleteDirectory(path, ignoreExceptions, recycle, dryRun));
            }
            else
            {
                Logger.Log($"Does not exist: {path}", Logger.Level.Verbose);
                return;
            }
        }

        /// <inheritdoc cref="Delete(string, bool?, bool, bool)"/>
        public static void Delete(IEnumerable<string> paths, bool? ignoreExceptions = true, bool recycle = false, bool dryRun = false) =>
            paths.ToList().ForEach(p => Delete(p, ignoreExceptions, recycle, dryRun));

        /// <inheritdoc cref="Delete(string, bool?, bool, bool)"/>
        public static void Delete(IEnumerable<FileInfo> files, bool? ignoreExceptions = true, bool recycle = false, bool dryRun = false) =>
            files.ToList().ForEach(f => Delete(f.FullName, ignoreExceptions, recycle, dryRun));

        /// <inheritdoc cref="Delete(string, bool?, bool, bool)"/>
        public static void Delete(IEnumerable<DirectoryInfo> dirs, bool? ignoreExceptions = true, bool recycle = false, bool dryRun = false) =>
            dirs.ToList().ForEach(d => Delete(d.FullName, ignoreExceptions, recycle, dryRun));

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

                    Logger.Log($"Failed to delete {file}: {ex.Message.Replace($"'{file}' ")}", Logger.Level.Warning, condition: () => ignoreExceptions == false);
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

                Logger.Log($"Failed to delete {path}: {ex.Message.Replace($"'{path}' ")}", Logger.Level.Warning, condition: () => ignoreExceptions == null || !ex.Message.Contains("The directory is not empty"));
            }
        }

        /// <summary> More reliable alternative to File.ReadAllLines, should work for files that are being accessed from another process </summary>
        public static List<string> ReadTextLines(string path, bool ommitEmptyLines = false) => ReadTextFile(path).GetLines(ommitEmptyLines);

        /// <summary> More reliable alternative to File.ReadAllText, should work for files that are being accessed from another process </summary>
        public static string ReadTextFile(string path, string fallback = "")
        {
            try
            {
                if (!File.Exists(path))
                    return fallback;

                using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using StreamReader reader = new StreamReader(fileStream);
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Logger.Log(ex, $"Failed to read file '{path}'");
                return fallback;
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

        /// <summary> A pseudo-hash built purely from the file metadata, thus needing almost zero CPU or I/O resources no matter how big the file is. <br/>
        /// Largely reliable, unless a file was edited without a file size change and without LastWriteTime being changed, which is extremely unlikely. </summary>
        public static string GetPseudoHash(FileInfo file)
        {
            if (file == null || !file.Exists)
            {
                Logger.LogErr($"Failed to get PseudoHash! {(file == null ? "File is null" : $"File does not exist - '{file.FullName}'")}");
                return "";
            }

            return $"{file.FullName}|{file.Length}|{file.LastWriteTimeUtc.ToString("yyyyMMddHHmmss")}";
        }
        /// <inheritdoc cref="GetPseudoHash(FileInfo)"/>
        public static string GetPseudoHash(string file) => GetPseudoHash(new FileInfo(file));

        /// <summary> Gets the combined size of all files in <paramref name="directory"/> in bytes, multithreaded. </summary>
        public static long GetDirSize(DirectoryInfo directory, int? threads = null)
        {
            threads ??= Environment.ProcessorCount;
            long totalSize = 0;

            try
            {
                FileInfo[] files = Try(directory.GetFiles);
                // Logger.LogWrn($"{directory.FullName}: {ex.Message}");

                files.Where(f => (f.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint).ParallelForEach(f =>
                {
                    Try(() => Interlocked.Add(ref totalSize, f.Length));
                }, threads);

                DirectoryInfo[] subdirectories = Try(directory.GetDirectories);
                subdirectories.Where(d => (d.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint).ParallelForEach(subdir =>
                {
                    long subdirSize = Try(() => GetDirSize(subdir, threads));
                    Interlocked.Add(ref totalSize, subdirSize);
                }, threads);
            }
            catch (Exception ex)
            {
                Logger.Log(ex, $"Failed to get size of {directory.FullName}");
            }

            return totalSize;
        }
        public static long GetDirSize(string dir, int? threads = null)
            => GetDirSize(new DirectoryInfo(dir), threads);


        /// <summary> Get size of a file or directory, null on failure. </summary>
        public static long? GetPathSize(string path)
        {
            try
            {
                bool isFile = File.Exists(path);
                return isFile ? new FileInfo(path).Length : GetDirSize(path);
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

        /// <summary>
        /// Checks if enough space is on the drive of <paramref name="path"/>. If an exception occurs, <paramref name="fallback"/> is returned instead. <br/>
        /// Fills variables <paramref name="availSpace"/> (available space in bytes) and <paramref name="drive"/> (drive root path).
        /// </summary>
        public static bool HasEnoughDiskSpace(string path, long bytesNeeded, out long availSpace, out string drive, long fallback = -1, bool log = true)
        {
            availSpace = GetFreeDiskSpace(path, fallback, log: log);
            drive = Path.GetPathRoot(Path.GetFullPath(path));
            return availSpace > bytesNeeded;
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

        /// <summary> Validate a file path depending on <paramref name="existMode"/>. </summary>
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

        /// <summary> Validate a directory path depending on <paramref name="existMode"/>. </summary>
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

        /// <summary> Validate a file or directory path. </summary>
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

        /// <summary> Check if a path is syntactically valid. </summary>
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
        public static void WriteDictToFile<TKey, TValue>(Dictionary<TKey, TValue> dict, string path) where TKey : notnull
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

        /// <summary> Gets the path to an executable by checking (in order): Config, environment (PATH), common installation directories. <br/>
        /// If not found, it can prompt the user for a path (<paramref name="allowPrompt"/>). This user-provided path gets saved in the config by default (<paramref name="allowWriteBack"/>) </summary>
        public static string GetProgram(string executable, bool allowPrompt = false, bool allowWriteBack = true)
        {
            if (executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                executable = executable.Remove(executable.Length - 4);

            string GetExePath(string p, out string full) // If directory was specified, get the exe path
            {
                full = p.EndsWith($"{executable}.exe", StringComparison.OrdinalIgnoreCase) ? p : Path.Combine(p.TrimEnd(Path.DirectorySeparatorChar), $"{executable}.exe");
                return full;
            }

            // 1) Check config
            if (Settings.CommandPaths != null && Settings.CommandPaths.Get(executable, out var confPath, "") && File.Exists(confPath))
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
                            Settings.CommandPaths.Set(executable, exePath);
                        }

                        Logger.Log($"Using {executable}.exe from common install dir: {exePath}", Logger.Level.Debug);
                        return exePath;
                    }

                    Logger.Log($"Could not find {executable}.exe in {subDir}", Logger.Level.Debug);
                }
            }

            if (!allowPrompt)
            {
                Logger.Log($"Could not find {executable}.", Logger.Level.Debug);
                return "";
            }

            Logger.Log($"Could not find {executable}.", Logger.Level.Warning);

            // 4) Ask user for path
            string userCommand = CliUtils.ReadLine($"Enter path (or command) for {executable}:"); // Prompt user to input path

            if (File.Exists(GetExePath(userCommand, out string pInput)) || Path.Exists(OsUtils.GetEnvExecutable(userCommand).FirstOrDefault("")))
            {
                if (Settings.CommandPaths == null)
                    return pInput;

                bool save = CliUtils.ReadLine($"Save this {executable} command to config? (Y/N)").Trim().Low() == "y";

                if (save)
                {
                    Settings.CommandPaths[executable] = pInput;
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

            public int Compare(FileSystemInfo? x, FileSystemInfo? y)
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
