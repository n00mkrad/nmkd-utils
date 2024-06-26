

namespace NmkdUtils
{
    public static class MathExtensions
    {
        public enum Rounding
        {
            Normal,
            Up,
            Down
        }

        public static int RoundToInt(this float f, Rounding method = Rounding.Normal)
        {
            return method switch
            {
                Rounding.Up => (int)Math.Ceiling(f),
                Rounding.Down => (int)Math.Floor(f),
                _ => (int)Math.Round(f),
            };
        }

        public static int RoundToInt(this double d, Rounding method = Rounding.Normal)
        {
            return method switch
            {
                Rounding.Up => (int)Math.Ceiling(d),
                Rounding.Down => (int)Math.Floor(d),
                _ => (int)Math.Round(d),
            };
        }

        public static long RoundToLong(this double d, Rounding method = Rounding.Normal)
        {
            return method switch
            {
                Rounding.Up => (long)Math.Ceiling(d),
                Rounding.Down => (long)Math.Floor(d),
                _ => (long)Math.Round(d),
            };
        }

        public static int RoundToNearestMultiple(this int number, int x)
        {
            return (int)Math.Round((double)number / x) * x;
        }

        public static double RatioTo(this int first, int second)
        {
            return (double)Math.Max(first, second) / Math.Min(first, second);
        }
    }
}
