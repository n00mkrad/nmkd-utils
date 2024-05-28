using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NmkdUtils
{
    public static class MathExtensions
    {
        public static int RoundToInt(this float f)
        {
            return (int)Math.Round(f);
        }

        public static int RoundToInt(this double d)
        {
            return (int)Math.Round(d);
        }

        public static long RoundToLong(this double d)
        {
            return (long)Math.Round(d);
        }
    }
}
