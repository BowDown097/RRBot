using System;

namespace RRBot.Extensions
{
    public static class TimeSpanExt
    {
        public static string FormatCompound(this TimeSpan ts)
        {
            return ts.TotalSeconds < 60
                ? $"{ts.Seconds} {(ts.Seconds != 1 ? "seconds" : "second")}"
                : $"{ts.Minutes} {(ts.Minutes != 1 ? "minutes" : "minute")} {ts.Seconds} {(ts.Seconds != 1 ? "seconds" : "second")}";
        }
    }
}
