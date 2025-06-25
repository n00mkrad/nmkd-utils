

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
            catch { }
            //catch (Exception ex)
            //{
            //    Logger.Log(ex, $"Failed to get size of directory {directory.FullName}");
            //}

            return size;
        }

        /// <summary> Gets the combined size of all files in <paramref name="directory"/> in bytes, multithreaded. If <paramref name="threads"/> is 0 or negative, the amount of CPU threads will be used </summary>
        public static long GetSize(this DirectoryInfo directory, int threads = -1)
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
                    Logger.LogWrn($"{directory.FullName}: {ex.Message}");
                    files = []; // Continue with no files if access is denied
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
                    catch
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
                catch
                {
                    // Logger.LogWrn($"{directory.FullName}: {ex.Message}");
                    subdirectories = []; // Continue with no subdirectories if access is denied
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
                    catch
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

        /// <summary> Sum file lengths of <paramref name="files"/>. </summary>
        public static long GetSize(this IEnumerable<FileInfo> files)
        {
            return files.Sum(f => f.Length);
        }

        /// <summary> Check if <paramref name="file"/> has the given <paramref name="extension"/>. </summary>
        public static bool HasExtension(this FileInfo file, string extension)
        {
            return file.Extension.TrimStart('.').Up() == extension.TrimStart('.').Up();
        }

        /// <summary> Check if <paramref name="file"/> has one of the <paramref name="extensions"/>. </summary>
        public static bool HasExtension(this FileInfo file, IEnumerable<string> extensions)
        {
            return extensions.Any(file.HasExtension);
        }

        /// <summary> Wrapper for <see cref="IoUtils.GetPseudoHash"/>. </summary>
        public static string GetPseudoHash (this FileInfo file)
        {
            return IoUtils.GetPseudoHash(file);
        }
    }
}
