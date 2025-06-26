namespace NmkdUtils
{
    public static class MathExtensions
    {
        public enum Rounding { Normal, Up, Down }

        /// <summary> Round a double to int with optional rounding mode and multiple. </summary>
        public static int RoundToInt(this double d, Rounding method = Rounding.Normal, int divBy = 1)
        {
            int i = method switch
            {
                Rounding.Up => (int)Math.Ceiling(d),
                Rounding.Down => (int)Math.Floor(d),
                _ => (int)Math.Round(d),
            };

            if (divBy <= 1)
                return i;

            bool? roundFlag = method == Rounding.Normal ? null : method == Rounding.Up;
            return i.RoundToMultiple(divBy, roundFlag);
        }
        public static int Round(this float f, Rounding method = Rounding.Normal, int divBy = 1) => ((double)f).RoundToInt(method, divBy);

        /// <summary> Round a double to long using <paramref name="method"/>. </summary>
        public static long RoundToLong(this double d, Rounding method = Rounding.Normal)
        {
            return method switch
            {
                Rounding.Up => (long)Math.Ceiling(d),
                Rounding.Down => (long)Math.Floor(d),
                _ => (long)Math.Round(d),
            };
        }

        /// <summary>
        /// Rounds <paramref name="value"/> to the specified <paramref name="mult"/>. If <paramref name="roundUp"/> is true, rounds up, if false, rounds down. If null, rounds to the nearest multiple.
        /// </summary>
        public static int RoundToMultiple(this int value, int mult = 2, bool? roundUp = null)
        {
            if (mult < 1)
                throw new ArgumentOutOfRangeException(nameof(mult), "Multiple must be ≥ 1.");

            if (value % mult == 0)
                return value;

            int remainder = Math.Abs(value) % mult;

            if (roundUp == true)
                return value >= 0 ? value + (mult - remainder) : value - remainder;

            if (roundUp == false)
                return value >= 0 ? value - remainder : value - (mult - remainder);

            bool shouldRoundUp = remainder * 2 >= mult;
            return shouldRoundUp ? (value >= 0 ? value + (mult - remainder) : value - remainder) : (value >= 0 ? value - remainder : value - (mult - remainder));
        }

        /// <summary> Ratio of the larger number to the smaller. </summary>
        public static double RatioTo(this int first, int second)
        {
            return (double)Math.Max(first, second) / Math.Min(first, second);
        }

        /// <summary> Limits a number to the given range <paramref name="min"/>-<paramref name="max"/> (both inclusive) </summary>
        public static int Clamp(this int i, int min, int max = int.MaxValue)
        {
            if (i < min)
                i = min;
            if (i > max)
                i = max;
            return i;
        }

        /// <summary> Limits a float to the given range <paramref name="min"/>-<paramref name="max"/> (both inclusive) </summary>
        public static float Clamp(this float f, float min, float max = float.MaxValue)
        {
            if (f < min)
                f = min;
            if (f > max)
                f = max;
            return f;
        }

        /// <summary> Compare two floats within <paramref name="tolerance"/>. </summary>
        public static bool EqualsRoughly(this float a, float b, float tolerance = 0.0001f)
        {
            return Math.Abs(a - b) < tolerance;
        }

        /// <summary> Check if <paramref name="value"/> is within [<paramref name="min"/>, <paramref name="max"/>]. </summary>
        public static bool IsInRange(this int value, int min, int max = int.MaxValue)
        {
            return value >= min && value <= max;
        }

        /// <summary> <inheritdoc cref="MathUtils.GetPercent{T}(IEnumerable{T}, Func{T, bool})"/> </summary>
        public static float GetPercent<T>(this IEnumerable<T> collection, Func<T, bool> predicate) => MathUtils.GetPercent(collection, predicate);
    }
}
