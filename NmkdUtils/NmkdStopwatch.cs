using System.Diagnostics;

namespace NmkdUtils
{
    public class NmkdStopwatch : Stopwatch
    {
        /// <summary> Shortcut for ElapsedMilliseconds </summary>
        public long Ms => ElapsedMilliseconds;

        /// <summary> Formatted elapsed time </summary>
        public string ElapsedStr => FormatUtils.Time(ElapsedMilliseconds);

        public NmkdStopwatch(bool startOnCreation = true)
        {
            if (startOnCreation)
                Restart();
        }
    }
}
