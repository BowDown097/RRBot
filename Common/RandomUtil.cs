namespace RRBot.Common;
public static class RandomUtil
{
    public static int Next(int max) => RandomNumberGenerator.GetInt32(max);
    public static int Next(int min, int max) => RandomNumberGenerator.GetInt32(min, max);
    public static double NextDouble(double max) => NextFloatingPoint(0D, max);
    public static double NextDouble(double min, double max) => NextFloatingPoint(min, max);
    public static decimal NextDecimal(decimal max) => NextFloatingPoint(0m, max);
    public static decimal NextDecimal(decimal min, decimal max) => NextFloatingPoint(min, max);

    public static T GetRandomElement<T>(T[] arr) => arr[Next(arr.Length)];
    public static T GetRandomElement<T>(IEnumerable<T> enumerable) => GetRandomElement(enumerable.ToArray());

    private static T NextFloatingPoint<T>(T min, T max) where T : IFloatingPoint<T>
    {
        byte[] buf = RandomNumberGenerator.GetBytes(8);
        ulong shiftedRand = BitConverter.ToUInt64(buf, 0) >> 11;
        T funnyRand = T.CreateChecked(shiftedRand) / T.CreateChecked(1UL << 53);
        return min + funnyRand * (max - T.One - min);
    }
}