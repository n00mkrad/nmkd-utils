﻿using Microsoft.VisualBasic.FileIO;
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

        /// <summary> Get file paths sorted by filename </summary>
        public static string[] GetFilesSorted(string path, bool recursive = false, string pattern = "*")
        {
            try
            {
                if (path.IsEmpty() || !Directory.Exists(path))
                    return new string[0];

                SearchOption opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                return Directory.GetFiles(path, pattern, opt).OrderBy(x => Path.GetFileName(x)).ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetFilesSorted error: {ex.Message}", true);
                return new string[0];
            }
        }

        /// <summary> Get files sorted by name </summary>
        public static FileInfo[] GetFileInfosSorted(string path, bool recursive = false, string pattern = "*")
        {
            try
            {
                if (path.IsEmpty() || !Directory.Exists(path))
                    return new FileInfo[0];


                SearchOption opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var dir = new DirectoryInfo(path);
                return dir.GetFiles(pattern, opt).OrderBy(x => x.Name).ToArray();
            }
            catch (Exception ex)
            {
                Logger.Log(ex, $"GetFileInfosSorted error");
                return new FileInfo[0];
            }
        }

        /// <summary> Get directories sorted by name (manual recursion to ignore inaccessible entries) </summary>
        public static DirectoryInfo[] GetDirInfosSorted(string root, bool recursive = false, string pattern = "*", bool noWarnings = false)
        {
            List<DirectoryInfo> directories = new List<DirectoryInfo>();

            if (root == null || !Directory.Exists(root))
                return directories.ToArray();

            Stack<string> pending = new Stack<string>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                string currentDir = pending.Pop();
                try
                {
                    // Add current directory to the list
                    DirectoryInfo dirInfo = new DirectoryInfo(currentDir);
                    directories.Add(dirInfo);

                    if (recursive)
                    {
                        // Add subdirectories to stack
                        foreach (var directory in Directory.GetDirectories(currentDir, pattern))
                        {
                            pending.Push(directory);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log the error and continue with the next directory
                    // Logger.LogWrn($"{currentDir}: {ex.Message}");
                }
            }

            // Sort the directories by name and return
            return directories.OrderBy(d => d.Name).ToArray();
        }

        /// <summary> Sends a file to the recycle bin </summary>
        public static bool RecycleFile(string path)
        {
            try
            {
                FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary> Sends a folder to the recycle bin </summary>
        public static bool RecycleDir(string path)
        {
            try
            {
                FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary> Deletes a file (or sends it to recycle bin if <paramref name="recycle"/> is true). Does nothing if <paramref name="dryRun"/> is true. </summary>
        public static void DeleteFile(string path, bool recycle = false, bool dryRun = false, Logger.Level logLvl = Logger.Level.Verbose)
        {
            if (dryRun)
                return;

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

        public static void DeletePath(string path, bool ignoreExceptions = true, bool recycle = false, bool dryRun = false)
        {
            try
            {
                // Check if the path exists
                if (!File.Exists(path) && !Directory.Exists(path))
                {
                    Logger.Log($"Does not exist: {path}", Logger.Level.Verbose);
                    return; // Path does not exist
                }

                if (Directory.Exists(path))
                {
                    if (recycle)
                    {
                        RecycleDir(path);
                    }
                    else
                    {
                        // Is a directory, so call DeleteDirectory and add its file results to deletedFiles
                        DeleteDirectory(path: path, ignoreExceptions: ignoreExceptions, recycle: recycle, dryRun: dryRun);
                    }
                }
                else if (File.Exists(path))
                {
                    DeleteFile(path, recycle, dryRun);
                }
            }
            catch (Exception ex)
            {
                if (!ignoreExceptions)
                {
                    throw; // Rethrow the exception if not ignoring them
                }
                Logger.LogConditional($"Failed to delete {path}: {ex.Message}", !ex.Message.Contains("The directory is not empty"), Logger.Level.Warning);
            }

            Logger.Log((dryRun ? $"Would have deleted {path}" : $"Deleted {path}"), Logger.Level.Verbose);
        }

        private static void DeleteDirectory(string path, bool ignoreExceptions = true, bool recycle = false, bool dryRun = false)
        {
            // Get all files and directories in the directory
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
                    if (!ignoreExceptions)
                    {
                        throw;
                    }
                    Logger.Log($"Failed to delete {file}: {ex.Message}", Logger.Level.Warning);
                }
            }

            foreach (string dir in directories)
            {
                DeleteDirectory(path: dir, ignoreExceptions: ignoreExceptions, recycle: recycle, dryRun: dryRun);
            }

            if (!dryRun)
            {
                try
                {
                    Directory.Delete(path);
                    // No FileInfo for directories since only files are tracked
                }
                catch (Exception ex)
                {
                    if (!ignoreExceptions)
                    {
                        throw;
                    }
                    Logger.LogConditional($"Failed to delete {path}: {ex.Message}", !ex.Message.Contains("The directory is not empty"), Logger.Level.Warning);
                }
            }
        }

        /// <summary> Delete a path if it exists. Works for files and directories. Returns success status. </summary>
        // public static bool TryDeleteIfExists(string path)
        // {
        //     try
        //     {
        //         if ((IsPathDirectory(path) == true && !Directory.Exists(path)) || (IsPathDirectory(path) == false && !File.Exists(path)))
        //             return true;
        // 
        //         DeleteIfExists(path);
        //         return true;
        //     }
        //     catch
        //     {
        //         try
        //         {
        //             SetAttributes(path);
        //             DeleteIfExists(path);
        //             return true;
        //         }
        //         catch (Exception e)
        //         {
        //             Console.WriteLine($"TryDeleteIfExists: Error trying to delete {path}: {e.Message}", true);
        //             return false;
        //         }
        //     }
        // }
        // 
        // public static bool DeleteIfExists(string path, bool log = false)
        // {
        //     if (log)
        //         Console.WriteLine($"DeleteIfExists({path})", true);
        // 
        //     if (IsPathDirectory(path) == false && File.Exists(path))
        //     {
        //         File.Delete(path);
        //         return true;
        //     }
        // 
        //     if (IsPathDirectory(path) == true && Directory.Exists(path))
        //     {
        //         Directory.Delete(path, true);
        //         return true;
        //     }
        // 
        //     return false;
        // }
        // 
        // public static bool SetAttributes(string rootDir, FileAttributes newAttributes = FileAttributes.Normal, bool recursive = true)
        // {
        //     try
        //     {
        //         GetFileInfosSorted(rootDir, recursive).ToList().ForEach(x => x.Attributes = newAttributes);
        //         return true;
        //     }
        //     catch (Exception ex)
        //     {
        //         return false;
        //     }
        // }

        /// <summary> More reliable alternative to File.ReadAllLines, should work for files that are being accessed from another process </summary>
        public static List<string> ReadFileLinesSafe(string path)
        {
            // Ensure that other processes can read and write to the file while it is open
            try
            {
                var lines = new List<string>();
                using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using StreamReader reader = new StreamReader(fileStream);
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    lines.Add(line);
                }
                return lines.Where(l => l != null).ToList();
            }
            catch (IOException e)
            {
                Console.WriteLine("An error occurred while reading the file: " + e.Message);
            }

            return new List<string>();
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
    }
}
