namespace RRBot.Extensions;
public static class TimeSpanExt
{
    public static string FormatCompound(this TimeSpan ts)
    {
        StringBuilder formatted = new();

        if (ts.TotalSeconds == -1) return "Indefinite";
        if (ts.Days != 0) formatted.Append($" {ts.Days} {(ts.Days != 1 ? "days" : "day")}");
        if (ts.Hours != 0) formatted.Append($" {ts.Hours} {(ts.Hours != 1 ? "hours" : "hour")}");
        if (ts.Minutes != 0) formatted.Append($" {ts.Minutes} {(ts.Minutes != 1 ? "minutes" : "minute")}");
        if (ts.Seconds != 0) formatted.Append($" {ts.Seconds} {(ts.Seconds != 1 ? "seconds" : "second")}");

        return formatted.ToString()[1..];
    }

    public static TimeSpan Round(this TimeSpan ts) => TimeSpan.FromSeconds(Math.Round(ts.TotalSeconds));
}