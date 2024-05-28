using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NmkdUtils
{
    public class NmkdStopwatch : Stopwatch
    {
        public long ElapsedMs => ElapsedMilliseconds;

        public NmkdStopwatch(bool startOnCreation = true)
        {
            if (startOnCreation)
                Restart();
        }
    }
}
