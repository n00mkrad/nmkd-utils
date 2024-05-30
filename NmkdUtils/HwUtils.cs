using System.Diagnostics;
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
            public float TotalGb => (float)TotalBytes / 1024 / 1024 / 1024;
            public float UsedGb => (float)UsedBytes / 1024 / 1024 / 1024;
            public float AvailGb => (float)AvailBytes / 1024 / 1024 / 1024;
        }

        private static PerformanceCounter? PerfCounterRam = null;

        public static RamInfo GetRamInfo()
        {
            if (OsUtils.IsWindows)
            {
                string availOutput = OsUtils.RunCommand("wmic OS get FreePhysicalMemory", false);
                var availBytes = availOutput.SplitIntoLines().Where(l => l.IsNotEmpty()).Last().GetLong() * 1024;

                if (availBytes <= 0 && false)
                {
                    Logger.LogWrn("Getting RAM using PerformanceCounter instead of wmic command, this is a lot slower on the first run!");
                    PerfCounterRam ??= new PerformanceCounter("Memory", "Available Bytes");
                    availBytes = (long)PerfCounterRam.NextValue();
                }

                var totalPhysicalMemory = (long)(GetPhysicallyInstalledSystemMemory(out ulong totalMemKb) ? totalMemKb : 0) * 1024;
                var usedMemory = totalPhysicalMemory - availBytes;
                return new RamInfo() { TotalBytes = totalPhysicalMemory, UsedBytes = usedMemory, AvailBytes = availBytes };
            }

            if (OsUtils.IsLinux)
            {
                string freeOutput = OsUtils.RunCommand("free | grep Mem", false); // "Mem: <total> <used> <free> <shared> <buff/cache> <available>" with spacing
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
