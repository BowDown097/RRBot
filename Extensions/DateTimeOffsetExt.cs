#pragma warning disable IDE0060, RCS1175 // why does these fire for "this" parameters?
namespace RRBot.Extensions;
public static class DateTimeOffsetExt
{
    public static long ToUnixTimeSeconds(this DateTimeOffset ts, long addSecs)
    {
        TimeSpan epoch = DateTime.UtcNow - DateTime.UnixEpoch;
        return (long)(epoch.TotalSeconds + addSecs);
    }
}