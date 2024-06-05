using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NmkdUtils
{
    public static class IoExtensions
    {
        /// <summary> Gets the combined size of all files in <paramref name="directory"/> in bytes </summary>
        public static long GetSize(this DirectoryInfo directory)
        {
            long size = 0;

            try
            {
                // Add file sizes.
                FileInfo[] files = directory.GetFiles();
                foreach (FileInfo file in files)
                {
                    // Check if the file is not a symbolic link
                    if ((file.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                    {
                        size += file.Length;
                    }
                }

                // Add subdirectory sizes.
                DirectoryInfo[] subdirectories = directory.GetDirectories();
                foreach (DirectoryInfo subdir in subdirectories)
                {
                    // Check if the directory is not a symbolic link
                    if ((subdir.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                    {
                        size += GetSize(subdir);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex, $"Failed to get size of directory {directory.FullName}");
            }

            return size;
        }

        /// <summary> Gets the combined size of all files in <paramref name="directory"/> in bytes, multithreaded. If <paramref name="threads"/> is <=0, the amount of CPU threads will be used </summary>
        public static long GetSize(this DirectoryInfo directory, int threads)
        {
            if (threads <= 0)
            {
                threads = Environment.ProcessorCount;
            }

            long totalSize = 0;

            try
            {
                // Process all files in the directory
                FileInfo[] files;
                try
                {
                    files = directory.GetFiles();
                }
                catch (Exception ex)
                {
                    // Logger.LogWrn($"{directory.FullName}: {ex.Message}");
                    files = new FileInfo[0]; // Continue with no files if access is denied
                }

                Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = threads }, file =>
                {
                    try
                    {
                        if ((file.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                        {
                            // Synchronize the addition to avoid race conditions
                            Interlocked.Add(ref totalSize, file.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Logger.LogWrn($"{file.FullName}: {ex.Message}");
                        // Skip inaccessible file and continue
                    }
                });

                // Process all subdirectories in parallel
                DirectoryInfo[] subdirectories;
                try
                {
                    subdirectories = directory.GetDirectories();
                }
                catch (Exception ex)
                {
                    // Logger.LogWrn($"{directory.FullName}: {ex.Message}");
                    subdirectories = new DirectoryInfo[0]; // Continue with no subdirectories if access is denied
                }

                Parallel.ForEach(subdirectories, new ParallelOptions { MaxDegreeOfParallelism = threads }, subdir =>
                {
                    try
                    {
                        if ((subdir.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                        {
                            // Recursively calculate size and add to total
                            long subdirSize = subdir.GetSize(threads);
                            Interlocked.Add(ref totalSize, subdirSize);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Logger.LogWrn($"{subdir.FullName}: {ex.Message}");
                        // Skip inaccessible subdirectory and continue
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Log(ex, $"Failed to get size of {directory.FullName}");
            }

            return totalSize;
        }

        public static long GetSize(this IEnumerable<FileInfo> files)
        {
            return files.Sum(f => f.Length);
        }
    }
}
