namespace RRBot.Extensions;
public static partial class StringExt
{
    public static string Elide(this string source, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(source))
            return source;
        return source.Length > maxLength ? source[..(maxLength - 1)] + "…" : source;
    }

    public static Tuple<TimeSpan, string> ResolveDuration(this string duration, int time, string action, string reason)
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

    public static string SplitPascalCase(this string source)
        => !string.IsNullOrWhiteSpace(source) ? PascalRegex().Replace(source, "$1 ") : source;

    public static string ToTitleCase(this string source)
        => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(source.ToLower());

    [GeneratedRegex("([a-z,0-9](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", RegexOptions.Compiled)]
    private static partial Regex PascalRegex();
}