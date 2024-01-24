namespace RRBot.Modules;
public partial class Moderation
{
    private static Tuple<TimeSpan, string> ResolveDuration(string duration, int time, string action, string reason)
    {
        TimeSpan ts = char.ToLower(duration[^1]) switch
        {
            's' => TimeSpan.FromSeconds(time),
            'm' => TimeSpan.FromMinutes(time),
            'h' => TimeSpan.FromHours(time),
            'd' => TimeSpan.FromDays(time),
            _ => TimeSpan.Zero
        };

        string response = $"{action} for {ts.FormatCompound()}";
        if (!string.IsNullOrWhiteSpace(reason))
            response += $" for \"{reason}\"";
        response += ".";

        return new Tuple<TimeSpan, string>(ts, response);
    }
}