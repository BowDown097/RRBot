#pragma warning disable IDE0060, RCS1175 // why does these fire for "this" parameters?
namespace RRBot.Extensions;
public static class DateTimeOffsetExt
{
    public static long ToUnixTimeSeconds(this DateTimeOffset ts, double addSecs)
    {
        TimeSpan epoch = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
        return (long)(epoch.TotalSeconds + addSecs);
    }
}