namespace RRBot.Extensions;

public static class JObjectExt
{
    public static bool TryParse(string json, out JObject? obj)
    {
        try
        {
            obj = JObject.Parse(json);
            return true;
        }
        catch (JsonReaderException)
        {
            obj = null;
            return false;
        }
    }
}