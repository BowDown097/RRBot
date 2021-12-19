namespace RRBot.Common;
public static class RandomUtil
{
    public static int Next(int max) => RandomNumberGenerator.GetInt32(max);
    public static int Next(int min, int max) => RandomNumberGenerator.GetInt32(min, max);

    public static double NextDouble(double min, double max)
    {
        byte[] buf = RandomNumberGenerator.GetBytes(8);
        ulong shiftedRand = BitConverter.ToUInt64(buf, 0) >> 11;
        double doubleRand = shiftedRand / (double)(1UL << 53);
        return min + (doubleRand * (max - 1 - min));
    }
}