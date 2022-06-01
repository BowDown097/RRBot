namespace RRBot.Extensions;
public static class StringExt
{
    private static readonly Regex reg = new("([a-z,0-9](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", RegexOptions.Compiled);
    public static string SplitPascalCase(this string source) => source != null ? reg.Replace(source, "$1 ") : null;
}