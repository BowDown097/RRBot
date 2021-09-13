using System;
using System.Security.Cryptography;

namespace RRBot
{
    public static class RandomUtil
    {
        private const long intMax = 1 + (long)uint.MaxValue;
        private static readonly RNGCryptoServiceProvider rng = new();

        public static int Next(int maxValue) => Next(0, maxValue);
        public static int Next(int minValue, int maxValue)
        {
            // from https://docs.microsoft.com/en-us/archive/msdn-magazine/2007/september/net-matters-tales-from-the-cryptorandom
            long diff = maxValue - minValue;
            while (true)
            {
                byte[] buf = new byte[4];
                rng.GetBytes(buf);
                uint rand = BitConverter.ToUInt32(buf, 0);

                long remainder = intMax % diff;
                if (rand < intMax - remainder)
                    return (int)(minValue + (rand % diff));
            }
        }

        public static double NextDouble(double maxValue) => NextDouble(0, maxValue);
        public static double NextDouble(double minValue, double maxValue)
        {
            byte[] buf = new byte[8];
            rng.GetBytes(buf);

            // we do some bit manipulation here to get more value diversity ig, idk i saw it online
            ulong shiftedRand = BitConverter.ToUInt64(buf, 0) >> 11;
            double doubleRand = shiftedRand / (double)(1UL << 53);
            return minValue + (doubleRand * (maxValue - minValue));
        }
    }
}