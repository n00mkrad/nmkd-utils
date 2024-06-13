

namespace NmkdUtils.Structs
{
    public struct Fraction
    {
        public int Numerator { get; private set; }
        public int Denominator { get; private set; }

        public static Fraction Zero => new Fraction(0, 1);

        public Fraction(string s)
        {
            var split = s.Split('/');
            int numerator = split[0].GetInt();
            int denominator = split[1].GetInt();

            if (denominator == 0)
                throw new ArgumentException("Denominator cannot be zero.", nameof(denominator));

            int gcd = GetGcd(numerator, denominator);
            Numerator = denominator < 0 ? -numerator / gcd : numerator / gcd;
            Denominator = Math.Abs(denominator / gcd);
        }

        public Fraction(int numerator, int denominator)
        {
            if (denominator == 0)
                throw new ArgumentException("Denominator cannot be zero.", nameof(denominator));

            int gcd = GetGcd(numerator, denominator);
            Numerator = denominator < 0 ? -numerator / gcd : numerator / gcd;
            Denominator = Math.Abs(denominator / gcd);
        }

        private static int GetGcd(int a, int b)
        {
            a = Math.Abs(a);
            b = Math.Abs(b);
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        public static Fraction operator +(Fraction a, Fraction b) => new(a.Numerator * b.Denominator + b.Numerator * a.Denominator, a.Denominator * b.Denominator);

        public static Fraction operator -(Fraction a, Fraction b) => new(a.Numerator * b.Denominator - b.Numerator * a.Denominator, a.Denominator * b.Denominator);

        public static Fraction operator *(Fraction a, Fraction b) => new(a.Numerator * b.Numerator, a.Denominator * b.Denominator);

        public static Fraction operator /(Fraction a, Fraction b)
        {
            if (b.Numerator == 0)
                throw new DivideByZeroException("Attempt to divide by zero fraction.");
            return new Fraction(a.Numerator * b.Denominator, a.Denominator * b.Numerator);
        }

        public double ToDouble() => (double)Numerator / Denominator;

        public float GetFloat()
        {
            if (Denominator == 0)
                throw new DivideByZeroException("Denominator is zero in fraction when attempting to convert to float.");
            return (float)Numerator / Denominator;
        }

        public long GetLong()
        {
            if (Denominator == 0)
                throw new DivideByZeroException("Denominator is zero in fraction when attempting to convert to long.");
            return Numerator / Denominator;
        }

        public string GetString(string format = "")
        {
            if (Denominator == 0)
                throw new DivideByZeroException("Denominator is zero in fraction when attempting to format string.");
            return ((float)Numerator / Denominator).ToString(format);
        }

        public override string ToString() => $"{Numerator}/{Denominator}";
    }
}
