namespace RRBot.Extensions;
public static class DateTimeOffsetExt
{
    public static long ToUnixTimeSeconds(this DateTimeOffset _, long addSecs)
    {
        TimeSpan epoch = DateTime.UtcNow - DateTime.UnixEpoch;
        return (long)(epoch.TotalSeconds + addSecs);
    }
}