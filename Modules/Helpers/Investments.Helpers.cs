namespace RRBot.Modules;
public partial class Investments
{
    public static async Task<decimal> QueryCryptoValue(string crypto)
    {
        using HttpClient client = new();
        string current = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm");
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd") + "T00:00";
        string data = await client.GetStringAsync($"https://production.api.coindesk.com/v2/price/values/{crypto.ToUpper()}?start_date={today}&end_date={current}");
        dynamic? obj = JsonConvert.DeserializeObject(data);
        if (obj is null) return decimal.MaxValue;
        JToken latestEntry = JArray.FromObject(obj.data.entries).Last;
        return Math.Round(latestEntry[1]?.Value<decimal>() ?? decimal.MaxValue, 2);
    }

    public static string? ResolveAbbreviation(string crypto) => crypto.ToLower() switch
    {
        "bitcoin" or "btc" => "Btc",
        "ethereum" or "eth" => "Eth",
        "litecoin" or "ltc" => "Ltc",
        "xrp" => "Xrp",
        _ => null,
    };
}