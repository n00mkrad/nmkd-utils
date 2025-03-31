using System.Management;
using System.Text.RegularExpressions;

namespace NmkdUtils
{
    public class HwUtils
    {
        public class RamInfo
        {
            public long TotalBytes { get; set; } = 0;
            public long UsedBytes { get; set; } = 0;
            public long AvailBytes { get; set; } = 0;
            public float TotalGb => TotalBytes / 1024f / 1024f / 1024f;
            public float UsedGb => UsedBytes / 1024f / 1024f / 1024f;
            public float AvailGb => AvailBytes / 1024f / 1024f / 1024f;

            public override string ToString() => $"{FormatUtils.FileSize(UsedBytes)} / {FormatUtils.FileSize(TotalBytes)} ({FormatUtils.FileSize(AvailBytes)} Free)";
        }

        public static RamInfo GetRamInfo()
        {
            if (OsUtils.IsWindows)
            {
                using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
                foreach (var obj in searcher.Get())
                {
                    var totalVisibleMemory = long.Parse(obj["TotalVisibleMemorySize"].ToString()) * 1024; // Convert from KB to B
                    var freePhysicalMemory = long.Parse(obj["FreePhysicalMemory"].ToString()) * 1024; // Convert from KB to B
                    return new RamInfo() { TotalBytes = totalVisibleMemory, UsedBytes = totalVisibleMemory - freePhysicalMemory, AvailBytes = freePhysicalMemory }; ;
                }
            }

            if (OsUtils.IsLinux)
            {
                string freeOutput = OsUtils.RunCommand("free | grep Mem"); // "Mem: <total> <used> <free> <shared> <buff/cache> <available>" with spacing
                freeOutput = Regex.Replace(freeOutput, @"\s+", ";"); // Replace empty space with delimiters
                var numbers = freeOutput.Split(';').Skip(1).Where(s => s.Length > 0).Select(n => n.GetLong()).ToList(); // Split by that delimiter and convert to numbers
                return new RamInfo() { TotalBytes = numbers[0] * 1024, UsedBytes = numbers[1] * 1024, AvailBytes = numbers.Last() * 1024 };
            }

            return new RamInfo();
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out ulong totalMemoryInKilobytes);
    }
}
