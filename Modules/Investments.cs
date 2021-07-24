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
        [Command("invest")]
        [Summary("Invest in a cryptocurrency. Currently accepted currencies are \"BTC\", \"DOGE\", \"ETH\", and \"XRP\". It's important to mention that cash values are rounded to 2 decimals, therefore investing in amounts of crypto with very specific or low decimals may be detrimental.")]
        [Remarks("``$invest [crypto] [amount]``")]
        [RequireCash]
        public async Task<RuntimeResult> Invest(string crypto, float amount)
        {
            string cUp = crypto.ToUpper();

            if (amount < 0.1f || float.IsNaN(amount)) return CommandResult.FromError($"{Context.User.Mention}, you must invest in a tenth or more of the crypto!");
            if (cUp != "BTC" && cUp != "DOGE" && cUp != "ETH" && cUp != "XRP") return CommandResult.FromError($"{Context.User.Mention}, **{crypto}** is not a currently accepted currency!");

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            float cash = snap.GetValue<float>("cash");
            float cryptoValue = await CashSystem.QueryCryptoValue(crypto) * amount;
            if (cash < cryptoValue) return CommandResult.FromError($"{Context.User.Mention}, you don't have enough money for this! You need at least **{cryptoValue.ToString("C2")}**.");

            await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash - cryptoValue);
            await CashSystem.AddCrypto(Context.User as IGuildUser, crypto.ToLower(), amount);

            await Context.User.NotifyAsync(Context.Channel, $"You have invested in **{amount}** {cUp}, currently valued at **{cryptoValue.ToString("C2")}**.");
            return CommandResult.FromSuccess();
        }

        [Command("investments")]
        [Summary("Check your investments, or someone else's, and their value.")]
        [Remarks("``$investments <user>``")]
        public async Task<RuntimeResult> InvestmentsView(IGuildUser user = null)
        {
            if (user != null && user.IsBot) return CommandResult.FromError("Nope.");

            ulong userId = user == null ? Context.User.Id : user.Id;
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(userId.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            StringBuilder investmentsBuilder = new StringBuilder();
            if (snap.TryGetValue("btc", out float btc) && btc > 0)
                investmentsBuilder.AppendLine($"**Bitcoin (BTC)**: {btc} ({(await CashSystem.QueryCryptoValue("BTC") * btc).ToString("C2")})");
            if (snap.TryGetValue("doge", out float doge) && doge > 0)
                investmentsBuilder.AppendLine($"**Dogecoin (DOGE)**: {doge} ({(await CashSystem.QueryCryptoValue("DOGE") * doge).ToString("C2")})");
            if (snap.TryGetValue("eth", out float eth) && eth > 0)
                investmentsBuilder.AppendLine($"**Ethereum (ETH)**: {eth} ({(await CashSystem.QueryCryptoValue("ETH") * eth).ToString("C2")})");
            if (snap.TryGetValue("xrp", out float xrp) && xrp > 0)
                investmentsBuilder.AppendLine($"**XRP**: {xrp} ({(await CashSystem.QueryCryptoValue("XRP") * xrp).ToString("C2")})");

            string investments = investmentsBuilder.ToString();

            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Red,
                Title = user == null ? "Your Investments" : $"{user.ToString()}'s Investments",
                Description = string.IsNullOrWhiteSpace(investments) ? "None" : investments
            };

            await ReplyAsync(embed: embed.Build());
            return CommandResult.FromSuccess();
        }

        [Alias("values")]
        [Command("prices")]
        [Summary("Check the values of currently available cryptocurrencies.")]
        [Remarks("``$prices``")]
        public async Task Prices()
        {
            float btc = await CashSystem.QueryCryptoValue("BTC");
            float doge = await CashSystem.QueryCryptoValue("DOGE");
            float eth = await CashSystem.QueryCryptoValue("ETH");
            float xrp = await CashSystem.QueryCryptoValue("XRP");

            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Red,
                Title = "Cryptocurrency Values",
                Description = $"**Bitcoin (BTC)**: {btc.ToString("C2")}\n**Dogecoin (DOGE)**: {doge.ToString("C2")}\n**Ethereum (ETH)**: {eth.ToString("C2")}\n**XRP**: {xrp.ToString("C2")}"
            };

            await ReplyAsync(embed: embed.Build());
        }

        [Command("withdraw")]
        [Summary("Withdraw a specified cryptocurrency to RR Cash, with a 2% withdrawal fee. See help info for $invest on currently accepted currencies.")]
        [Remarks("``$withdraw [crypto] [amount]``")]
        public async Task<RuntimeResult> Withdraw(string crypto, float amount)
        {
            string cUp = crypto.ToUpper();

            if (amount < 0.1f || float.IsNaN(amount)) return CommandResult.FromError($"{Context.User.Mention}, you must withdraw a tenth or more of the crypto!");
            if (cUp != "BTC" && cUp != "DOGE" && cUp != "ETH" && cUp != "XRP") return CommandResult.FromError($"{Context.User.Mention}, **{crypto}** is not a currently accepted currency!");

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (!snap.TryGetValue(crypto.ToLower(), out float cryptoBal) || cryptoBal <= 0) return CommandResult.FromError($"{Context.User.Mention}, you have no {crypto}!");
            if (cryptoBal < amount) return CommandResult.FromError($"{Context.User.Mention}, you don't have {amount} {crypto}! You've only got **{cryptoBal}** of it.");

            float cash = snap.GetValue<float>("cash");
            float cryptoValue = await CashSystem.QueryCryptoValue(crypto) * amount;
            float finalValue = cryptoValue * 0.98f;

            await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash + finalValue);
            await CashSystem.AddCrypto(Context.User as IGuildUser, crypto.ToLower(), -amount);

            await Context.User.NotifyAsync(Context.Channel, $"You have withdrew **{amount}** {cUp}, currently valued at **{cryptoValue.ToString("C2")}**. " +
                $"A 2% withdrawal fee was taken from this amount, leaving you **{finalValue.ToString("C2")}** richer.");
            return CommandResult.FromSuccess();
        }
    }
}
