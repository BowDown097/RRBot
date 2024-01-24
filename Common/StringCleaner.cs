namespace RRBot.Common;
public static class StringCleaner
{
    private static readonly string[] SensitiveCharacters = ["*", "_", "`", "~", ">"];

    public static string Sanitize(string text)
    {
        return string.IsNullOrEmpty(text) ? ""
            : SensitiveCharacters.Aggregate(text,
                (curr, unsafeChar) => curr.Replace(unsafeChar, "\\" + unsafeChar));
    }

    public static string Sanitize(string text, IEnumerable<string> characters)
    {
        return string.IsNullOrEmpty(text) ? ""
            : characters.Aggregate(text,
                (curr, unsafeChar) => curr.Replace(unsafeChar, "\\" + unsafeChar));
    }
}