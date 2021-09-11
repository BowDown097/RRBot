using Discord;
using Discord.Commands;
using Google.Cloud.Firestore;
using RRBot.Extensions;
using RRBot.Preconditions;
using RRBot.Systems;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace RRBot.Modules
{
    [Summary("Invest in our selection of coins, Bit or Shit. The prices here are updated in REAL TIME with the REAL LIFE values. Experience the fast, entrepreneural life without going broke, having your house repossessed, and having your girlfriend leave you.")]
    public class Investments : ModuleBase<SocketCommandContext>
    {
        public CultureInfo CurrencyCulture { get; set; }

        [Command("invest")]
        [Summary("Invest in a cryptocurrency. Currently accepted currencies are BTC, DOGE, ETH, LTC, and XRP. Here, the amount you put in should be RR Cash.")]
        [Remarks("$invest [crypto] [amount]")]
        [RequireCash]
        public async Task<RuntimeResult> Invest(string crypto, double amount)
        {
            if (amount < 0 || double.IsNaN(amount)) return CommandResult.FromError("You can't invest nothing!");

            string abbreviation;
            switch (crypto.ToLower())
            {
                case "bitcoin":
                case "btc":
                    abbreviation = "BTC"; break;
                case "dogecoin":
                case "doge":
                    abbreviation = "DOGE"; break;
                case "ethereum":
                case "eth":
                    abbreviation = "ETH"; break;
                case "litecoin":
                case "ltc":
                    abbreviation = "LTC"; break;
                case "xrp":
                    abbreviation = "XRP"; break;
                default:
                    return CommandResult.FromError($"**{crypto}** is not a currently accepted currency!");
            }

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            double cash = snap.GetValue<double>("cash");
            double cryptoAmount = Math.Round(amount / await CashSystem.QueryCryptoValue(abbreviation), 4);

            if (cash < amount)
            {
                return CommandResult.FromError("You can't invest more than what you have!");
            }
            if (cryptoAmount < Constants.INVESTMENT_MIN_AMOUNT)
            {
                return CommandResult.FromError($"The amount you specified converts to less than {Constants.INVESTMENT_MIN_AMOUNT} of {abbreviation}, which is not permitted.\n"
                    + $"You'll need to invest at least **{await CashSystem.QueryCryptoValue(abbreviation) * Constants.INVESTMENT_MIN_AMOUNT:C2}**.");
            }

            await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash - amount);
            await CashSystem.AddCrypto(Context.User as IGuildUser, abbreviation.ToLower(), cryptoAmount);
            await Context.User.AddToStatsAsync(CurrencyCulture, Context.Guild, new Dictionary<string, string>
            {
                { $"Money Put Into {abbreviation}", amount.ToString("C2", CurrencyCulture) },
                { $"{abbreviation} Purchased", cryptoAmount.ToString("0.####") }
            });

            await Context.User.NotifyAsync(Context.Channel, $"You have invested in **{cryptoAmount}** {abbreviation}, currently valued at **{amount:C2}**.");
            return CommandResult.FromSuccess();
        }

        [Command("investments")]
        [Summary("Check your investments, or someone else's, and their value.")]
        [Remarks("$investments <user>")]
        public async Task<RuntimeResult> InvestmentsView(IGuildUser user = null)
        {
            if (user?.IsBot == true) return CommandResult.FromError("Nope.");

            ulong userId = user == null ? Context.User.Id : user.Id;
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(userId.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            StringBuilder investmentsBuilder = new();
            if (snap.TryGetValue("btc", out double btc) && btc >= Constants.INVESTMENT_MIN_AMOUNT)
                investmentsBuilder.AppendLine($"**Bitcoin (BTC)**: {btc:0.####} ({await CashSystem.QueryCryptoValue("BTC") * btc:C2})");
            if (snap.TryGetValue("doge", out double doge) && doge >= Constants.INVESTMENT_MIN_AMOUNT)
                investmentsBuilder.AppendLine($"**Dogecoin (DOGE)**: {doge:0.####} ({await CashSystem.QueryCryptoValue("DOGE") * doge:C2})");
            if (snap.TryGetValue("eth", out double eth) && eth >= Constants.INVESTMENT_MIN_AMOUNT)
                investmentsBuilder.AppendLine($"**Ethereum (ETH)**: {eth:0.####} ({await CashSystem.QueryCryptoValue("ETH") * eth:C2})");
            if (snap.TryGetValue("ltc", out double ltc) && ltc >= Constants.INVESTMENT_MIN_AMOUNT)
                investmentsBuilder.AppendLine($"**Litecoin (LTC)**: {ltc:0.####} ({await CashSystem.QueryCryptoValue("LTC") * ltc:C2})");
            if (snap.TryGetValue("xrp", out double xrp) && xrp >= Constants.INVESTMENT_MIN_AMOUNT)
                investmentsBuilder.AppendLine($"**XRP**: {xrp:0.####} ({await CashSystem.QueryCryptoValue("XRP") * xrp:C2})");

            string investments = investmentsBuilder.ToString();

            EmbedBuilder embed = new()
            {
                Color = Color.Red,
                Title = user == null ? "Your Investments" : $"{user}'s Investments",
                Description = string.IsNullOrWhiteSpace(investments) ? "None" : investments
            };

            await ReplyAsync(embed: embed.Build());
            return CommandResult.FromSuccess();
        }

        [Alias("values")]
        [Command("prices")]
        [Summary("Check the values of currently available cryptocurrencies.")]
        [Remarks("$prices")]
        public async Task Prices()
        {
            double btc = await CashSystem.QueryCryptoValue("BTC");
            double doge = await CashSystem.QueryCryptoValue("DOGE");
            double eth = await CashSystem.QueryCryptoValue("ETH");
            double ltc = await CashSystem.QueryCryptoValue("LTC");
            double xrp = await CashSystem.QueryCryptoValue("XRP");

            EmbedBuilder embed = new()
            {
                Color = Color.Red,
                Title = "Cryptocurrency Values",
                Description = $"**Bitcoin (BTC)**: {btc:C2}\n**Dogecoin (DOGE)**: {doge:C2}\n**Ethereum (ETH)**: {eth:C2}\n**Litecoin (LTC)**: {ltc:C2}\n**XRP**: {xrp:C2}"
            };

            await ReplyAsync(embed: embed.Build());
        }

        [Command("withdraw")]
        [Summary("Withdraw a specified cryptocurrency to RR Cash, with a 2% withdrawal fee. Here, the amount you put in should be in the crypto, not RR Cash. See $invest's help info for currently accepted currencies.")]
        [Remarks("$withdraw [crypto] [amount]")]
        public async Task<RuntimeResult> Withdraw(string crypto, double amount)
        {
            if (amount < Constants.INVESTMENT_MIN_AMOUNT || double.IsNaN(amount))
                return CommandResult.FromError($"You must withdraw {Constants.INVESTMENT_MIN_AMOUNT} or more of the crypto!");

            string abbreviation;
            switch (crypto.ToLower())
            {
                case "bitcoin":
                case "btc":
                    abbreviation = "BTC"; break;
                case "dogecoin":
                case "doge":
                    abbreviation = "DOGE"; break;
                case "ethereum":
                case "eth":
                    abbreviation = "ETH"; break;
                case "litecoin":
                case "ltc":
                    abbreviation = "LTC"; break;
                case "xrp":
                    abbreviation = "XRP"; break;
                default:
                    return CommandResult.FromError($"**{crypto}** is not a currently accepted currency!");
            }

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (!snap.TryGetValue(abbreviation.ToLower(), out double cryptoBal) || cryptoBal <= 0)
                return CommandResult.FromError($"You have no {abbreviation}!");
            if (cryptoBal < amount)
                return CommandResult.FromError($"You don't have {amount} {abbreviation}! You've only got **{cryptoBal}** of it.");

            double cash = snap.GetValue<double>("cash");
            double cryptoValue = await CashSystem.QueryCryptoValue(abbreviation) * amount;
            double finalValue = cryptoValue / 100.0 * (100 - Constants.INVESTMENT_FEE_PERCENT);

            await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash + finalValue);
            await CashSystem.AddCrypto(Context.User as IGuildUser, abbreviation.ToLower(), -amount);
            await Context.User.AddToStatsAsync(CurrencyCulture, Context.Guild, new Dictionary<string, string>
            {
                { $"Money Gained From {abbreviation}", finalValue.ToString("C2", CurrencyCulture) }
            });

            await Context.User.NotifyAsync(Context.Channel, $"You have withdrew **{amount}** {abbreviation}, currently valued at **{cryptoValue:C2}**.\n" +
                $"A {Constants.INVESTMENT_FEE_PERCENT}% withdrawal fee was taken from this amount, leaving you **{finalValue:C2}** richer.");
            return CommandResult.FromSuccess();
        }
    }
}
