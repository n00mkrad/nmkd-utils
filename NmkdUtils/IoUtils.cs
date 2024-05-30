using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            if (path == null)
                return false;

            if (!File.Exists(path))
                return false;

            return true;
        }

        public static bool IsDirValid(string path)
        {
            if (path == null)
                return false;

            if (!Directory.Exists(path))
                return false;

            return true;
        }

        public static bool IsPathValid(string path)
        {
            if (path == null)
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
                if (path == null || !Directory.Exists(path))
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
                if (path == null || !Directory.Exists(path))
                    return new FileInfo[0];


                SearchOption opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var dir = new DirectoryInfo(path);
                return dir.GetFiles(pattern, opt).OrderBy(x => x.Name).ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetFileInfosSorted error: {ex.Message}");
                return new FileInfo[0];
            }
        }

        /// <summary> Delete a path if it exists. Works for files and directories. Returns success status. </summary>
        public static bool TryDeleteIfExists(string path)
        {
            try
            {
                if ((IsPathDirectory(path) == true && !Directory.Exists(path)) || (IsPathDirectory(path) == false && !File.Exists(path)))
                    return true;

                DeleteIfExists(path);
                return true;
            }
            catch
            {
                try
                {
                    SetAttributes(path);
                    DeleteIfExists(path);
                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"TryDeleteIfExists: Error trying to delete {path}: {e.Message}", true);
                    return false;
                }
            }
        }

        public static bool DeleteIfExists(string path, bool log = false)
        {
            if (log)
                Console.WriteLine($"DeleteIfExists({path})", true);

            if (IsPathDirectory(path) == false && File.Exists(path))
            {
                File.Delete(path);
                return true;
            }

            if (IsPathDirectory(path) == true && Directory.Exists(path))
            {
                Directory.Delete(path, true);
                return true;
            }

            return false;
        }

        public static bool SetAttributes(string rootDir, FileAttributes newAttributes = FileAttributes.Normal, bool recursive = true)
        {
            try
            {
                GetFileInfosSorted(rootDir, recursive).ToList().ForEach(x => x.Attributes = newAttributes);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

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
    }
}
