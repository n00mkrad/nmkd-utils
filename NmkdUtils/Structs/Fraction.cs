

namespace NmkdUtils.Structs
{
    public class Fraction
    {
        public long Numerator = 0;
        public long Denominator = 1;
        public static Fraction Zero = new Fraction(0, 1);

        public Fraction() { }

        public Fraction(long numerator, long denominator)
        {
            Numerator = numerator;
            Denominator = denominator;

            //If denominator negative...
            if (Denominator < 0)
            {
                //...move the negative up to the numerator
                Numerator = -Numerator;
                Denominator = -Denominator;
            }
        }

        public Fraction(Fraction fraction)
        {
            Numerator = fraction.Numerator;
            Denominator = fraction.Denominator;
        }

        /// <summary>
        /// Initializes a new Fraction by approximating the <paramref name="value"/> as a fraction using up to 4 digits.
        /// </summary>
        public Fraction(float value)
        {
            int maxDigits = 4;
            var (num, den) = FloatToApproxFraction(value, maxDigits);
            Numerator = num;
            Denominator = den;
        }

        /// <summary>
        /// Initializes a new Fraction from a string <paramref name="text"/>. If the text represents a single number or a fraction, it parses accordingly.
        /// </summary>
        public Fraction(string text)
        {
            try
            {
                if (text.IsEmpty())
                {
                    Numerator = 0;
                    Denominator = 1;
                    return;
                }

                text = text.Replace(':', '/'); // Replace colon with slash in case someone thinks it's a good idea to write a fraction like that
                string[] numbers = text.Split('/');

                // If split is only 1 item, it's a single number, not a fraction
                if (numbers.Length == 1)
                {
                    float numFloat = numbers[0].GetFloat();
                    int numInt = numFloat.Round();

                    // If parsed float is equal to the rounded int, it's a whole number
                    if (numbers[0].GetFloat().EqualsRoughly(numInt))
                    {
                        Numerator = numInt;
                        Denominator = 1;
                    }
                    else
                    {
                        // Use float constructor if not a whole number
                        var floatFrac = new Fraction(numFloat);
                        Numerator = floatFrac.Numerator;
                        Denominator = floatFrac.Denominator;
                    }

                    return;
                }

                Numerator = numbers[0].GetFloat().Round();
                Denominator = numbers[1].GetInt();
            }
            catch
            {
                try
                {
                    Numerator = text.GetFloat().Round();
                    Denominator = 1;
                }
                catch
                {
                    Numerator = 0;
                    Denominator = 1;
                }
            }
        }

        /// <summary>
        /// Calculates and returns the greatest common denominator (GCD) for <paramref name="a"/> and <paramref name="b"/> by dropping negative signs and using the modulo operation.
        /// </summary>
        private static long GetGreatestCommonDenominator(long a, long b)
        {
            //Drop negative signs
            a = Math.Abs(a);
            b = Math.Abs(b);

            //Return the greatest common denominator between two longs
            while (a != 0 && b != 0)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }

            if (a == 0)
                return b;
            else
                return a;
        }

        /// <summary>
        /// Calculates and returns the least common denominator for <paramref name="a"/> and <paramref name="b"/> using their greatest common denominator.
        /// </summary>
        private static long GetLeastCommonDenominator(long a, long b)
        {
            return (a * b) / GetGreatestCommonDenominator(a, b);
        }

        /// <summary>
        /// Converts the fraction to have the specified <paramref name="targetDenominator"/> if possible by scaling the numerator accordingly; returns a Fraction with the target denominator or the current fraction if conversion is not possible.
        /// </summary>
        public Fraction ToDenominator(long targetDenominator)
        {
            Fraction modifiedFraction = this;

            // Cannot reduce to smaller denominators & target denominator must be a factor of the current denominator
            if (targetDenominator < Denominator || targetDenominator % Denominator != 0)
                return modifiedFraction;

            if (Denominator != targetDenominator)
            {
                long factor = targetDenominator / Denominator; // Find factor to multiply the fraction by to make the denominator match the target denominator
                modifiedFraction.Denominator = targetDenominator;
                modifiedFraction.Numerator *= factor;
            }

            return modifiedFraction;
        }

        /// <summary>
        /// Reduces the fraction to its lowest terms by repeatedly dividing the numerator and denominator by their greatest common denominator.
        /// </summary>
        public Fraction GetReduced()
        {
            Fraction modifiedFraction = this;

            try
            {
                //While the numerator and denominator share a greatest common denominator, keep dividing both by it
                long gcd = 0;
                while (Math.Abs(gcd = GetGreatestCommonDenominator(modifiedFraction.Numerator, modifiedFraction.Denominator)) != 1)
                {
                    modifiedFraction.Numerator /= gcd;
                    modifiedFraction.Denominator /= gcd;
                }

                //Make sure only a single negative sign is on the numerator
                if (modifiedFraction.Denominator < 0)
                {
                    modifiedFraction.Numerator = -Numerator;
                    modifiedFraction.Denominator = -Denominator;
                }
            }
            catch (Exception e)
            {
                Logger.LogWrn($"Failed to reduce fraction ({modifiedFraction}): {e.Message}");
            }

            return modifiedFraction;
        }

        /// <summary>
        /// Returns a new Fraction that is the reciprocal of the current fraction by swapping the numerator and denominator.
        /// </summary>
        public Fraction GetReciprocal()
        {
            return new Fraction(Denominator, Numerator);
        }

        /// <summary>
        /// Combines two fractions <paramref name="f1"/> and <paramref name="f2"/> using the specified <paramref name="combine"/> function after converting them to a common denominator; returns the reduced combined Fraction.
        /// </summary>
        private static Fraction Combine(Fraction f1, Fraction f2, Func<long, long, long> combine)
        {
            if (f1.Denominator == 0)
                return f2;
            if (f2.Denominator == 0)
                return f1;

            long lcd = GetLeastCommonDenominator(f1.Denominator, f2.Denominator);
            f1 = f1.ToDenominator(lcd);
            f2 = f2.ToDenominator(lcd);
            return new Fraction(combine(f1.Numerator, f2.Numerator), lcd).GetReduced();
        }

        public override string ToString()
        {
            return Denominator == 1 ? $"{Numerator}" : $"{Numerator}/{Denominator}";
        }

        // Conversion properties
        public float Float => Denominator < 1 ? 0f : (float)Numerator / (float)Denominator;
        public double Double => (double)Numerator / Denominator;
        public long Long => Denominator < 1 ? 0L : (long)Numerator / (long)Denominator;

        // Operators
        public static bool operator >(Fraction frac, float value) => frac.Double > value;
        public static bool operator <(Fraction frac, float value) => frac.Double < value;
        public static bool operator >(float value, Fraction frac) => value > frac.Double;
        public static bool operator <(float value, Fraction frac) => value < frac.Double;
        public static Fraction operator +(Fraction frac1, Fraction frac2) => Combine(frac1, frac2, (a, b) => a + b);
        public static Fraction operator -(Fraction frac1, Fraction frac2) => Combine(frac1, frac2, (a, b) => a - b);
        public static Fraction operator *(Fraction frac1, Fraction frac2) => new Fraction(frac1.Numerator * frac2.Numerator, frac1.Denominator * frac2.Denominator).GetReduced();
        public static Fraction operator /(Fraction frac1, Fraction frac2) => new Fraction(frac1 * frac2.GetReciprocal()).GetReduced();
        public static Fraction operator *(Fraction frac, long mult) => new Fraction(frac.Numerator * mult, frac.Denominator).GetReduced();
        public static Fraction operator *(Fraction frac, double mult) => new Fraction((long)Math.Round(frac.Numerator * mult), frac.Denominator).GetReduced();
        public static Fraction operator *(Fraction frac, float mult) => new Fraction((frac.Numerator * mult).Round(), frac.Denominator).GetReduced();

        public string GetString(string format = "0.#####")
        {
            return ((float)Numerator / Denominator).ToString(format);
        }

        // FractionHelper

        /// <summary>
        /// Converts a float (<paramref name="value"/>) to an approximated fraction that is as close to the original value as possible, with a limit on the number of digits for numerator and denominator (<paramref name="maxDigits"/>).
        /// </summary>
        public static (int Numerator, int Denominator) FloatToApproxFraction(float value, int maxDigits = 4)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentException("Value must be a finite float.");

            // Special case: zero
            if (Math.Abs(value) < float.Epsilon)
                return (0, 1);

            // Determine the sign and work with absolute value for searching.
            int sign = Math.Sign(value);
            double target = Math.Abs((double)value);

            // Upper bound for numerator/denominator based on max digits
            // e.g. if maxDigits = 4, limit = 9999
            int limit = (int)Math.Pow(10, maxDigits) - 1;

            // We'll track the best fraction found
            double bestError = double.MaxValue;
            int bestNum = 0;
            int bestDen = 1;

            // Simple brute-force search over all possible denominators
            for (int d = 1; d <= limit; d++)
            {
                // Round the numerator for the current denominator
                int n = (int)Math.Round(target * d);

                // If n is 0, skip (except the value might be < 0.5/d, but continue searching)
                if (n == 0)
                    continue;

                // If the numerator exceeds the limit, skip
                if (n > limit)
                    continue;

                // Evaluate how close n/d is to the target
                double fractionValue = (double)n / d;
                double error = Math.Abs(fractionValue - target);

                // If it's closer, record it as our best
                if (error < bestError)
                {
                    bestError = error;
                    bestNum = n;
                    bestDen = d;
                }
            }

            // Reapply the sign to the numerator
            bestNum *= sign;

            // Reduce fraction by GCD (to get simplest form)
            int gcd = GCD(bestNum, bestDen);
            bestNum /= gcd;
            bestDen /= gcd;

            // If the denominator is 1 after reduction, just return the integer
            if (bestDen == 1)
            {
                return (bestNum, 1);
            }

            // Otherwise return "numerator/denominator"
            // Logger.Log($"Approximated fraction for {value}: {bestNum}/{bestDen} (={((float)bestNum / bestDen).ToString("0.0#######")})", true);
            return (bestNum, bestDen);
        }

        /// <summary> Computes the greatest common divisor (Euclid's algorithm). </summary>
        private static int GCD(int a, int b)
        {
            a = Math.Abs(a);
            b = Math.Abs(b);
            while (b != 0)
            {
                int t = b;
                b = a % b;
                a = t;
            }
            return a;
        }
    }
}