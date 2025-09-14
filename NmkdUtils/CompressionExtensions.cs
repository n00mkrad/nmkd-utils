using System.IO.Compression;

namespace NmkdUtils;
public static class CompressionExtensions
{
    /// <summary> Extract a ZIP archive to <paramref name="destinationDir"/>. <br/> Optionally, extract only a subfolder by passing <paramref name="relPath"/>. </summary>
    public static void ExtractTo(this ZipArchive zip, string destinationDir, bool overwrite = true, string relPath = "")
    {
        if(relPath.IsEmpty())
        {
            zip.ExtractToDirectory(destinationDir, overwrite);
            return;
        }

        zip.ExtractSubfolder(destinationDir, relPath, overwrite: overwrite);
    }

    /// <summary>
    /// Extract the contents of a specified subfolder within the archive, specified with <paramref name="relPath"/>, to <paramref name="destinationDir"/>. <br/> 
    /// If the ZIP has a single root folder at the top, you can use the placeholder ROOT in <paramref name="relPath"/> to have it automatically replaced by the root folder name.
    /// </summary>
    public static void ExtractSubfolder(this ZipArchive zip, string destinationDir, string relPath = "", bool overwrite = true)
    {
        // Get name of root folder if avail.
        var root = zip.Entries.Select(e => e.FullName).Where(s => s.EndsWith('/')).FirstOrDefault();

        if (relPath.IsEmpty() || root == null)
        {
            zip.ExtractTo(destinationDir, overwrite);
            return;
        }

        string rootPath = relPath.TrimEnd('/').Replace("ROOT", root.TrimEnd('/')) + '/';
        var entries = zip.Entries.Where(e => e.FullName.StartsWith(rootPath));

        // extract each entry, stripping off "root/"
        foreach (var entry in entries)
        {
            // skip directories
            if (entry.FullName.EndsWith('/'))
                continue;

            var relativePathOut = entry.FullName.Substring(rootPath.Length);
            if (relativePathOut.IsEmpty())
                continue; // e.g. the root folder entry

            var destPath = Path.Combine(destinationDir, relativePathOut);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
            entry.ExtractToFile(destPath, overwrite);
        }
    }
}
