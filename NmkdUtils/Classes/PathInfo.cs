using static NmkdUtils.Enums;

namespace NmkdUtils.Classes;

public class PathInfo
{
    public string Path { get; }
    public PathKind Kind { get; }
    public bool Exists => Kind is PathKind.File or PathKind.Directory;
    public bool IsDir => Kind == PathKind.Directory;
    public bool IsFile => Kind == PathKind.File;
    public long Size => IsDir ? IoUtils.GetDirSize(Path) : File?.Length ?? 0;
    public FileSystemInfo? Fsi { get; }
    public FileInfo? File { get; }
    public DirectoryInfo? Dir { get; }

    public PathInfo(string path)
    {
        Path = System.IO.Path.GetFullPath(path);
        bool? isDir = IoUtils.IsPathDirectory(path);

        if (isDir == false)
        {
            Kind = PathKind.File;
            Fsi = new FileInfo(path);
            File = (FileInfo)Fsi;
        }
        else if (isDir == true)
        {
            Kind = PathKind.Directory;
            Fsi = new DirectoryInfo(path);
            Dir = (DirectoryInfo)Fsi;
        }
        else
        {
            Kind = PathKind.Missing;
        }
    }
}
