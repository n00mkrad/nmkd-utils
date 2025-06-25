
namespace NmkdUtils
{
    public class RollingAverage<T> where T : struct
    {
        public int CurrentSize { get => _values.Count; }
        public double Average { get => GetAverage(); }

        private Queue<T> _values;
        public Queue<T> Queue { get => _values; }
        private int _size;

        /// <summary> Initialize with a maximum of <paramref name="size"/> samples. </summary>
        public RollingAverage(int size)
        {
            _values = new Queue<T>(size);
            _size = size;
        }

        /// <summary> Append a new data point to the rolling window. </summary>
        public void AddDataPoint(T dataPoint)
        {
            if (_values.Count >= _size)
            {
                _values.Dequeue();
            }

            _values.Enqueue(dataPoint);
        }

        /// <summary> Average of all stored samples. </summary>
        public double GetAverage()
        {
            if (_values == null || _values.Count == 0)
            {
                return 0d;
            }

            // Convert the values to double before averaging, this is necessary because Average() does not work directly on generic types
            return _values.Select(val => Convert.ToDouble(val)).Average();
        }

        /// <summary> Average of the last <paramref name="lastXSamples"/> samples. </summary>
        public double GetAverage(int lastXSamples)
        {
            if (lastXSamples <= 0)
            {
                lastXSamples = 1;
            }
            else if (lastXSamples > _values.Count)
            {
                lastXSamples = _values.Count;
            }

            // Take the last X samples and calculate the average
            return _values.Skip(Math.Max(0, _values.Count - lastXSamples)).Select(val => Convert.ToDouble(val)).Average();
        }

        /// <summary> Average of the most recent <paramref name="percentile"/> of samples. </summary>
        public double GetAverage(float percentile)
        {
            int lastXSamples = (int)Math.Ceiling(_size * percentile);
            return GetAverage(lastXSamples);
        }

        /// <summary> Clear all stored samples. </summary>
        public void Reset()
        {
            _values.Clear();
        }
    }

    public class RollingAverageBool
    {
        public int CurrentSize { get => _values.Count; }
        public double Average { get => GetAverage(); }

        private Queue<bool> _values;
        public Queue<bool> Queue { get => _values; }
        private int _size;

        /// <summary> Initialize with a maximum of <paramref name="size"/> samples. </summary>
        public RollingAverageBool(int size)
        {
            this._values = new Queue<bool>(size);
            this._size = size;
        }

        /// <summary> Append a new boolean sample. </summary>
        public void AddDataPoint(bool dataPointValue)
        {
            if (_values.Count >= _size)
            {
                _values.Dequeue();
            }

            _values.Enqueue(dataPointValue);
        }

        /// <summary> Average of all stored samples. </summary>
        public double GetAverage()
        {
            if (_values == null || _values.Count == 0)
            {
                return 0d;
            }

            return _values.Select(val => val == true ? 1d : 0d).Average();
        }

        /// <summary> Average of the last <paramref name="lastXSamples"/> samples. </summary>
        public double GetAverage(int lastXSamples)
        {
            if (lastXSamples <= 0)
            {
                lastXSamples = 1;
            }
            else if (lastXSamples > _values.Count)
            {
                lastXSamples = _values.Count;
            }

            // Take the last X samples and calculate the average
            return _values.Skip(Math.Max(0, _values.Count - lastXSamples)).Select(val => val ? 1d : 0d).Average();
        }

        /// <summary> Average of the most recent <paramref name="percentile"/> of samples. </summary>
        public double GetAverage(float percentile)
        {
            int lastXSamples = (int)Math.Ceiling(_size * percentile);
            return GetAverage(lastXSamples);
        }

        /// <summary> Clear all stored samples. </summary>
        public void Reset()
        {
            _values.Clear();
        }
    }
}
