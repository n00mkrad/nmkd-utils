

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
    }
}
