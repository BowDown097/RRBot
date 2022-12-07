namespace RRBot.Common;

public static class StringCleaner
{
    private static readonly string[] SensitiveCharacters =
    {
        "*", "_", "`", "~", ">"
    };

    public static string Sanitize(string text)
    {
        return string.IsNullOrEmpty(text) ? ""
            : SensitiveCharacters.Aggregate(text,
                (current, unsafeChar) => current.Replace(unsafeChar, "\\" + unsafeChar));
    }
}