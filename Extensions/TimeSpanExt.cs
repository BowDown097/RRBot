namespace RRBot.Extensions;
public static class TimeSpanExt
{
    public static string Condense(this TimeSpan ts)
        => (int)ts.TotalHours > 0 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"m\:ss");

    public static string FormatCompound(this TimeSpan ts)
    {
        StringBuilder formatted = new();

        if (ts.TotalSeconds == -1) return "Indefinite";
        if (ts.Days > 0) formatted.Append($" {ts.Days} {(ts.Days > 1 ? "days" : "day")}");
        if (ts.Hours > 0) formatted.Append($" {ts.Hours} {(ts.Hours > 1 ? "hours" : "hour")}");
        if (ts.Minutes > 0) formatted.Append($" {ts.Minutes} {(ts.Minutes > 1 ? "minutes" : "minute")}");
        if (ts.Seconds > 0) formatted.Append($" {ts.Seconds} {(ts.Seconds > 1 ? "seconds" : "second")}");

        return formatted.Length > 0 ? formatted.ToString()[1..] : "N/A";
    }
}