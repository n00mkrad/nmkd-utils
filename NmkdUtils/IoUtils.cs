﻿using Microsoft.VisualBasic.FileIO;
using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public static bool IsFileValid(string path)
        {
            if (path.IsEmpty())
                return false;

            if (!File.Exists(path))
                return false;

            return true;
        }

        public static bool IsDirValid(string path)
        {
            if (path.IsEmpty())
                return false;

            if (!Directory.Exists(path))
                return false;

            return true;
        }

        public static bool IsPathValid(string path)
        {
            if (path.IsEmpty())
                return false;

            if (IsPathDirectory(path) == true)
                return IsDirValid(path);
            else
                return IsFileValid(path);
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
        public static bool RecycleFile (string path)
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

        public static List<FileInfo> DeletePath(string path, bool ignoreExceptions = true, bool recycle = false, bool dryRun = false)
        {
            List<FileInfo> deletedFiles = new();
            try
            {
                // Check if the path exists
                if (!File.Exists(path) && !Directory.Exists(path))
                {
                    Logger.Log($"Does not exist: {path}", Logger.Level.Verbose);
                    return deletedFiles; // Path does not exist, return empty list.
                }

                if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
                {
                    // Is a directory, so call DeleteDirectory and add its file results to deletedFiles
                    deletedFiles.AddRange(DeleteDirectory(path: path, ignoreExceptions: ignoreExceptions, recycle: recycle, dryRun: dryRun));
                }
                else
                {
                    DeleteFile(path, recycle, dryRun);
                    deletedFiles.Add(new FileInfo(path));
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

            Logger.Log($"Deleted {path} ({FormatUtils.FileSize(deletedFiles.GetSize())})", Logger.Level.Verbose);
            return deletedFiles;
        }

        private static List<FileInfo> DeleteDirectory(string path, bool ignoreExceptions = true, bool recycle = false, bool dryRun = false)
        {
            List<FileInfo> deletedFiles = new();
            // Get all files and directories in the directory
            string[] files = Directory.GetFiles(path);
            string[] directories = Directory.GetDirectories(path);

            foreach (string file in files)
            {
                try
                {
                    DeleteFile(path, recycle, dryRun);
                    deletedFiles.Add(new FileInfo(file));
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
                deletedFiles.AddRange(DeleteDirectory(path: dir, ignoreExceptions: ignoreExceptions, recycle: recycle, dryRun: dryRun));
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

            return deletedFiles;
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

        public static bool TransferFileTimestamps(string pathSource, string pathTarget)
        {
            try
            {
                var source = new FileInfo(pathSource);
                var target = new FileInfo(pathTarget);
                target.CreationTime = source.CreationTime;
                target.LastWriteTime = source.LastWriteTime;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(ex, $"Failed to transfer timestamps from {pathSource} to {pathTarget}");
                return false;
            }
        }

        public static bool SetFileTimestamps(DateTime timestamp, string pathTarget)
        {
            try
            {
                var target = new FileInfo(pathTarget);
                target.CreationTime = timestamp;
                target.LastWriteTime = timestamp;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(ex, $"Failed to set timestamp son {pathTarget}");
                return false;
            }
        }
    }
}
