namespace RRBot.Extensions;
public static class NameValueCollectionExt
{
    public static bool TryGetValue(this NameValueCollection col, string key, out string value)
    {
        value = col[key];
        return value is null;
    }
}