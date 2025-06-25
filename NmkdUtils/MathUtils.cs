

using System.Drawing;

namespace NmkdUtils
{
    public class MathUtils
    {
        /// <summary> Returns a random integer between <paramref name="minValue"/> and <paramref name="maxValue"/> (both inclusive). </summary>
        public static int GetRandomInt(int minValue = 0, int maxValue = int.MaxValue)
        {
            long exclusiveUpper = (long)maxValue + 1;
            long result = Random.Shared.NextInt64(minValue, exclusiveUpper);
            return (int)result;
        }

        /// <summary> Returns a random bool (true or false) with a 50% chance of each. </summary>
        public static bool GetRandomBool(int probabilityPercent = 50) => probabilityPercent >= GetRandomInt(1, 100);

        /// <summary> Returns a random float between <paramref name="minValue"/> and <paramref name="maxValue"/> (both inclusive). </summary>
        public static float GetRandomFloat(float minValue = 0f, float maxValue = 1f)
        {
            float range = maxValue - minValue;
            float result = Random.Shared.NextSingle() * range + minValue;
            return result;
        }

        /// <summary> Gets the ratio of <paramref name="num1"/> to <paramref name="num2"/> as a percentage (0-100) </summary>
        public static float GetPercent(float num1, float num2 = 1f)
        {
            return num2 == 0 ? 0f : (num1 / num2) * 100f;
        }

        /// <summary> <inheritdoc cref="GetPercent(float, float)"/> rounded to int </summary>
        public static int GetPercentInt(float num1, float num2) => GetPercent(num1, num2).Round();

        /// <summary> Gets the percentage of entries in the <paramref name="collection"/> that match the given <paramref name="predicate"/> </summary>
        public static float GetPercent<T>(IEnumerable<T> collection, Func<T, bool> predicate)
        {
            int all = collection.Count();
            int matching = collection.Where(predicate).Count();
            return GetPercent(matching, all);
        }

        /// <summary> Fits the <paramref name="original"/> size into the <paramref name="target"/> size, maintaining aspect ratio. </summary>
        public static Size FitIntoArea(Size original, Size target)
        {
            float ratioX = (float)target.Width / original.Width;
            float ratioY = (float)target.Height / original.Height;
            float ratio = Math.Min(ratioX, ratioY);
            return new Size((original.Width * ratio).Round(), (original.Height * ratio).Round());
        }
        public static Size FitIntoArea(int originalW, int originalH, int targetW, int targetH)
                        => FitIntoArea(new Size(originalW, originalH), new Size(targetW, targetH));
    }
}
