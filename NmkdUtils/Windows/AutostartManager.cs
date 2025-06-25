using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace NmkdUtils.Windows
{
    public static class AutostartManager
    {
        // Interfaces and classes needed for shortcut creation (same as your reference code):
        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink { }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLink
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            void Resolve(IntPtr hwnd, int fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        public static string ExePath = "";
        public static bool IsAutostartEnabled => File.Exists(GetLnkPath());

        static AutostartManager()
        {
            ExePath = Environment.ProcessPath;
        }

        /// <summary> Gets the path to the autostart shortcut (.lnk file) </summary>
        public static string GetLnkPath()
        {
            string exeName = Path.GetFileNameWithoutExtension(ExePath);
            string linkName = exeName + ".lnk";
            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            return Path.Combine(startupFolder, linkName);
        }

        /// <summary>
        /// Toggles autostart by creating or removing a shortcut in the user's Startup folder. <br/> The shortcut name is inferred from this application's executable name.
        /// </summary>
        public static void SetAutostart(bool autostart)
        {
            string shortcutPath = GetLnkPath();

            if (autostart && !File.Exists(shortcutPath))
            {
                CreateShortcut(targetPath: ExePath, shortcutPath: shortcutPath, description: $"{Path.GetFileNameWithoutExtension(ExePath)} Autostart");
            }
            else
            {
                IoUtils.DeleteFile(shortcutPath);
            }
        }

        /// <summary> <inheritdoc cref="SetAutostart(bool)"/> </summary>
        public static void ToggleAutostart() => SetAutostart(!IsAutostartEnabled);

        /// <summary>
        /// Creates a shortcut to at <paramref name="targetPath"/> the executable <paramref name="targetPath"/>. <br/>
        /// The <paramref name="description"/> defaults to the filename w/o extension, <paramref name="workingDir"/> defaults to the file's directory.
        /// </summary>
        private static void CreateShortcut(string targetPath, string shortcutPath, string? description = null, string? workingDir = null)
        {
            IShellLink link = (IShellLink)new ShellLink();
            link.SetPath(targetPath);
            link.SetDescription(description ?? Path.GetFileNameWithoutExtension(targetPath));
            link.SetWorkingDirectory(workingDir.IsEmpty() || !Directory.Exists($"{workingDir}") ? Path.GetDirectoryName(targetPath) : workingDir);
            IPersistFile file = (IPersistFile)link;
            file.Save(shortcutPath, false);
        }
    }
}
