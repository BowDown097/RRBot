namespace RRBot.Extensions;
public static class StringExt
{
    private static readonly Regex PascalRegex = new("([a-z,0-9](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", RegexOptions.Compiled);

    public static string Elide(this string source, int maxLength)
        => source.Length > maxLength ? source[..(maxLength - 1)] + "…" : source;
    public static string SplitPascalCase(this string source)
        => source != null ? PascalRegex.Replace(source, "$1 ") : null;
    public static string ToTitleCase(this string source)
        => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(source.ToLower());
}