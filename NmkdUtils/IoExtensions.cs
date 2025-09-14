

namespace NmkdUtils
{
    public static class IoExtensions
    {
        /// <summary> Gets the combined size of all files in <paramref name="directory"/> in bytes, multithreaded. </summary>
        public static long GetSize(this DirectoryInfo directory, int? threads = null)
            => IoUtils.GetDirSize(directory, threads);

        /// <summary> Sum file lengths of <paramref name="files"/>. </summary>
        public static long GetSize(this IEnumerable<FileInfo> files)
            => files.Sum(f => f.Length);

        /// <summary> Check if <paramref name="file"/> has the given <paramref name="extension"/>. </summary>
        public static bool HasExtension(this FileInfo file, string extension)
            => file.Extension.TrimStart('.').Up() == extension.TrimStart('.').Up();

        /// <summary> Check if <paramref name="file"/> has one of the <paramref name="extensions"/>. </summary>
        public static bool HasExtension(this FileInfo file, IEnumerable<string> extensions)
            => extensions.Any(file.HasExtension);

        /// <summary> Wrapper for <see cref="IoUtils.GetPseudoHash"/>. </summary>
        public static string GetPseudoHash(this FileInfo file)
            => IoUtils.GetPseudoHash(file);

        /// <summary> Checks if a file <paramref name="fi"/> is smaller than <paramref name="mb"/> megabytes </summary>
        public static bool IsSmallerThanMb(this FileInfo fi, int mb)
            => fi.Length < mb * 1024 * 1024;
    }
}
