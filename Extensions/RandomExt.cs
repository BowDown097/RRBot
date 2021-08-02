using System;

namespace RRBot.Extensions
{
    public static class RandomExt
    {
        public static double NextDouble(this Random random, double maxValue) => NextDouble(random, 0, maxValue);

        public static double NextDouble(this Random random, double minValue, double maxValue)
        {
            maxValue--;
            return (random.NextDouble() * (maxValue - minValue)) + minValue;
        }
    }
}
