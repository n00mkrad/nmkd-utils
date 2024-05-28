using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NmkdUtils
{
    public static class FileExtensions
    {
        /// <summary> Checks if a file <paramref name="fi"/> is smaller than <paramref name="mb"/> megabytes </summary>
        public static bool IsSmallerThanMb(this FileInfo fi, int mb)
        {
            return fi.Length < mb * 1024 * 1024;
        }
    }
}
