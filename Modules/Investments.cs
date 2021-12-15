namespace RRBot.Modules;
[Summary("Invest in our selection of coins, Bit or Shit. The prices here are updated in REAL TIME with the REAL LIFE values. Experience the fast, entrepreneural life without going broke, having your house repossessed, and having your girlfriend leave you.")]
public class Investments : ModuleBase<SocketCommandContext>
{
    public static async Task<double> QueryCryptoValue(string crypto)
    {
        using HttpClient client = new();
        string current = DateTime.Now.ToString("yyyy-MM-ddTHH:mm");
        string today = DateTime.Now.ToString("yyyy-MM-dd") + "T00:00";
        string data = await client.GetStringAsync($"https://production.api.coindesk.com/v2/price/values/{crypto}?start_date={today}&end_date={current}");
        dynamic obj = JsonConvert.DeserializeObject(data);
        JToken latestEntry = JArray.FromObject(obj.data.entries).Last;
        return Math.Round(latestEntry[1].Value<double>(), 2);
    }

    public static string ResolveAbbreviation(string crypto) => crypto.ToLower() switch
    {
        "bitcoin" or "btc" => "BTC",
        "dogecoin" or "doge" => "DOGE",
        "ethereum" or "eth" => "ETH",
        "litecoin" or "ltc" => "LTC",
        "xrp" => "XRP",
        _ => null,
    };

    [Command("invest")]
    [Summary("Invest in a cryptocurrency. Currently accepted currencies are BTC, DOGE, ETH, LTC, and XRP. Here, the amount you put in should be RR Cash.")]
    [Remarks("$invest [crypto] [amount]")]
    [RequireCash]
    public async Task<RuntimeResult> Invest(string crypto, double amount)
    {
        if (amount < Constants.TRANSACTION_MIN || double.IsNaN(amount))
            return CommandResult.FromError($"You need to invest at least {Constants.TRANSACTION_MIN:C2}.");

        string abbreviation = ResolveAbbreviation(crypto);
        if (abbreviation is null)
            return CommandResult.FromError($"**{crypto}** is not a currently accepted currency!");

        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        if (user.UsingSlots)
            return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");
        if (user.Cash < amount)
            return CommandResult.FromError("You can't invest more than what you have!");

        double cryptoAmount = amount / await QueryCryptoValue(abbreviation);
        if (cryptoAmount < Constants.INVESTMENT_MIN_AMOUNT)
        {
            return CommandResult.FromError($"The amount you specified converts to less than {Constants.INVESTMENT_MIN_AMOUNT} of {abbreviation}, which is not permitted.\n"
                + $"You'll need to invest at least **{await QueryCryptoValue(abbreviation) * Constants.INVESTMENT_MIN_AMOUNT:C2}**.");
        }

        CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
        culture.NumberFormat.CurrencyNegativePattern = 2;
        await user.SetCash(Context.User, user.Cash - amount);
        user[abbreviation] = (double)user[abbreviation] + Math.Round(cryptoAmount, 4);
        user.AddToStats(new()
        {
            { $"Money Put Into {abbreviation}", amount.ToString("C2", culture) },
            { $"{abbreviation} Purchased", cryptoAmount.ToString("0.####") }
        });

        await Context.User.NotifyAsync(Context.Channel, $"You have invested in **{cryptoAmount:0.####}** {abbreviation}, currently valued at **{amount:C2}**.");
        return CommandResult.FromSuccess();
    }

    [Command("investments")]
    [Summary("Check your investments, or someone else's, and their value.")]
    [Remarks("$investments <user>")]
    public async Task<RuntimeResult> InvestmentsView(IGuildUser user = null)
    {
        if (user?.IsBot == true)
            return CommandResult.FromError("Nope.");

        ulong userId = user == null ? Context.User.Id : user.Id;
        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, userId);

        StringBuilder investments = new();
        if (dbUser.BTC >= Constants.INVESTMENT_MIN_AMOUNT)
            investments.AppendLine($"**Bitcoin (BTC)**: {dbUser.BTC:0.####} ({await QueryCryptoValue("BTC") * dbUser.BTC:C2})");
        if (dbUser.DOGE >= Constants.INVESTMENT_MIN_AMOUNT)
            investments.AppendLine($"**Dogecoin (DOGE)**: {dbUser.DOGE:0.####} ({await QueryCryptoValue("DOGE") * dbUser.DOGE:C2})");
        if (dbUser.ETH >= Constants.INVESTMENT_MIN_AMOUNT)
            investments.AppendLine($"**Ethereum (ETH)**: {dbUser.ETH:0.####} ({await QueryCryptoValue("ETH") * dbUser.ETH:C2})");
        if (dbUser.LTC >= Constants.INVESTMENT_MIN_AMOUNT)
            investments.AppendLine($"**Litecoin (LTC)**: {dbUser.LTC:0.####} ({await QueryCryptoValue("LTC") * dbUser.LTC:C2})");
        if (dbUser.XRP >= Constants.INVESTMENT_MIN_AMOUNT)
            investments.AppendLine($"**XRP**: {dbUser.XRP:0.####} ({await QueryCryptoValue("XRP") * dbUser.XRP:C2})");

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle(user == null ? "Your Investments" : $"{user.Sanitize()}'s Investments")
            .WithDescription(investments.Length > 0 ? investments.ToString() : "None");
        await ReplyAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }

    [Alias("values")]
    [Command("prices")]
    [Summary("Check the values of currently available cryptocurrencies.")]
    [Remarks("$prices")]
    public async Task Prices()
    {
        double btc = await QueryCryptoValue("BTC");
        double doge = await QueryCryptoValue("DOGE");
        double eth = await QueryCryptoValue("ETH");
        double ltc = await QueryCryptoValue("LTC");
        double xrp = await QueryCryptoValue("XRP");

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Cryptocurrency Values")
            .RRAddField("Bitcoin (BTC)", btc.ToString("C2"))
            .RRAddField("Dogecoin (DOGE)", doge.ToString("C2"))
            .RRAddField("Ethereum (ETH)", eth.ToString("C2"))
            .RRAddField("Litecoin (LTC)", ltc.ToString("C2"))
            .RRAddField("XRP", xrp.ToString("C2"));
        await ReplyAsync(embed: embed.Build());
    }

    [Command("withdraw")]
    [Summary("Withdraw a specified cryptocurrency to RR Cash, with a 2% withdrawal fee. Here, the amount you put in should be in the crypto, not RR Cash. See $invest's help info for currently accepted currencies.")]
    [Remarks("$withdraw [crypto] [amount]")]
    public async Task<RuntimeResult> Withdraw(string crypto, double amount)
    {
        if (amount < Constants.INVESTMENT_MIN_AMOUNT || double.IsNaN(amount))
            return CommandResult.FromError($"You must withdraw {Constants.INVESTMENT_MIN_AMOUNT} or more of the crypto.");

        string abbreviation = ResolveAbbreviation(crypto);
        if (abbreviation is null)
            return CommandResult.FromError($"**{crypto}** is not a currently accepted currency!");

        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        if (user.UsingSlots)
            return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");

        double cryptoBal = (double)user[abbreviation];
        if (cryptoBal < Constants.INVESTMENT_MIN_AMOUNT)
            return CommandResult.FromError($"You have no {abbreviation}!");
        if (cryptoBal < amount)
            return CommandResult.FromError($"You don't have {amount} {abbreviation}! You've only got **{cryptoBal:0.####}** of it.");

        double cryptoValue = await QueryCryptoValue(abbreviation) * amount;
        double finalValue = cryptoValue / 100.0 * (100 - Constants.INVESTMENT_FEE_PERCENT);

        CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
        culture.NumberFormat.CurrencyNegativePattern = 2;

        await user.SetCash(Context.User, user.Cash + finalValue);
        user[abbreviation] = (double)user[abbreviation] - Math.Round(amount, 4);
        user.AddToStat($"Money Gained From {abbreviation}", finalValue.ToString("C2", culture));

        await Context.User.NotifyAsync(Context.Channel, $"You withdrew **{amount:0.####}** {abbreviation}, currently valued at **{cryptoValue:C2}**.\n" +
            $"A {Constants.INVESTMENT_FEE_PERCENT}% withdrawal fee was taken from this amount, leaving you **{finalValue:C2}** richer.");
        return CommandResult.FromSuccess();
    }
}