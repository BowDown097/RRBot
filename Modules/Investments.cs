using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Google.Cloud.Firestore;
using RRBot.Extensions;
using RRBot.Preconditions;
using RRBot.Systems;

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
            if (amount < 0 || double.IsNaN(amount)) return CommandResult.FromError($"{Context.User.Mention}, you can't invest nothing!");

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
                    return CommandResult.FromError($"{Context.User.Mention}, **{crypto}** is not a currently accepted currency!");
            }

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            double cash = snap.GetValue<double>("cash");
            double cryptoAmount = Math.Round(amount / await CashSystem.QueryCryptoValue(abbreviation), 4);

            if (cash < amount)
            {
                return CommandResult.FromError($"{Context.User.Mention}, you can't invest more than what you have!");
            }
            if (cryptoAmount < 0.01)
            {
                return CommandResult.FromError($"{Context.User.Mention}, the amount you specified converts to less than a hundredth of {abbreviation}'s value, which is not permitted.\n"
                    + $"You'll need to invest at least **{await CashSystem.QueryCryptoValue(abbreviation) * 0.01:C2}**.");
            }

            await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash - amount);
            await CashSystem.AddCrypto(Context.User as IGuildUser, abbreviation.ToLower(), cryptoAmount);
            await Context.User.AddToStatsAsync(CurrencyCulture, Context.Guild, new Dictionary<string, string>
            {
                { $"Money Put Into {abbreviation}", amount.ToString("C2", CurrencyCulture) },
                { $"{abbreviation} Purchased", cryptoAmount.ToString() }
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
            if (snap.TryGetValue("btc", out double btc) && btc >= 0.01)
                investmentsBuilder.AppendLine($"**Bitcoin (BTC)**: {btc} ({await CashSystem.QueryCryptoValue("BTC") * btc:C2})");
            if (snap.TryGetValue("doge", out double doge) && doge >= 0.01)
                investmentsBuilder.AppendLine($"**Dogecoin (DOGE)**: {doge} ({await CashSystem.QueryCryptoValue("DOGE") * doge:C2})");
            if (snap.TryGetValue("eth", out double eth) && eth >= 0.01)
                investmentsBuilder.AppendLine($"**Ethereum (ETH)**: {eth} ({await CashSystem.QueryCryptoValue("ETH") * eth:C2})");
            if (snap.TryGetValue("ltc", out double ltc) && ltc >= 0.01)
                investmentsBuilder.AppendLine($"**Litecoin (LTC)**: {ltc} ({await CashSystem.QueryCryptoValue("LTC") * ltc:C2})");
            if (snap.TryGetValue("xrp", out double xrp) && xrp >= 0.01)
                investmentsBuilder.AppendLine($"**XRP**: {xrp} ({await CashSystem.QueryCryptoValue("XRP") * xrp:C2})");

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
            if (amount < 0.01 || double.IsNaN(amount)) return CommandResult.FromError($"{Context.User.Mention}, you must withdraw a hundredth or more of the crypto!");

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
                    return CommandResult.FromError($"{Context.User.Mention}, **{crypto}** is not a currently accepted currency!");
            }

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (!snap.TryGetValue(abbreviation.ToLower(), out double cryptoBal) || cryptoBal <= 0)
                return CommandResult.FromError($"{Context.User.Mention}, you have no {abbreviation}!");
            if (cryptoBal < amount)
                return CommandResult.FromError($"{Context.User.Mention}, you don't have {amount} {abbreviation}! You've only got **{cryptoBal}** of it.");

            double cash = snap.GetValue<double>("cash");
            double cryptoValue = await CashSystem.QueryCryptoValue(abbreviation) * amount;
            double finalValue = cryptoValue * 0.985;

            await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash + finalValue);
            await CashSystem.AddCrypto(Context.User as IGuildUser, abbreviation.ToLower(), -amount);
            await Context.User.AddToStatsAsync(CurrencyCulture, Context.Guild, new Dictionary<string, string>
            {
                { $"Money Gained From {abbreviation}", finalValue.ToString("C2", CurrencyCulture) }
            });

            await Context.User.NotifyAsync(Context.Channel, $"You have withdrew **{amount}** {abbreviation}, currently valued at **{cryptoValue:C2}**.\n" +
                $"A 1.5% withdrawal fee was taken from this amount, leaving you **{finalValue:C2}** richer.");
            return CommandResult.FromSuccess();
        }
    }
}
