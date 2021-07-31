using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using RRBot.Extensions;
using RRBot.Preconditions;
using RRBot.Systems;

namespace RRBot.Modules
{
    [Summary("Do you want to test your luck? Do you want to probably go broke? Here you go! By the way, you don't need to be 21 or over in this joint ;)")]
    public class Gambling : ModuleBase<SocketCommandContext>
    {
        public CultureInfo CurrencyCulture { get; set; }

        private bool ThreeInARow(int[] results, int emoji)
        {
            return (results[0] == emoji && results[1] == emoji && results[2] == emoji) || (results[1] == emoji && results[2] == emoji && results[3] == emoji);
        }

        private bool TwoInARow(int[] results, int emoji)
        {
            return (results[0] == emoji && results[1] == emoji) || (results[1] == emoji && results[2] == emoji) || (results[2] == emoji && results[3] == emoji);
        }

        public static readonly Random random = new Random();
        public static readonly Emoji SEVEN = new Emoji("7️⃣");
        public static readonly Emoji APPLE = new Emoji("\uD83C\uDF4E");
        public static readonly Emoji GRAPES = new Emoji("\uD83C\uDF47");
        public static readonly Emoji CHERRIES = new Emoji("\uD83C\uDF52");
        public static readonly Dictionary<int, Emoji> emojis = new Dictionary<int, Emoji>
        {
            { 1, SEVEN },
            { 2, APPLE },
            { 3, GRAPES },
            { 4, CHERRIES }
        };

        private async Task StatUpdate(SocketUser user, bool success, double gain)
        {
            if (success)
            {
                await user.AddToStatsAsync(CurrencyCulture, Context.Guild, new Dictionary<string, string>
                {
                    { "Gambles Won", "1" },
                    { "Money Gained from Gambling", gain.ToString("C2", CurrencyCulture) },
                    { "Net Gain/Loss from Gambling", gain.ToString("C2", CurrencyCulture) }
                });
            }
            else
            {
                await user.AddToStatsAsync(CurrencyCulture, Context.Guild, new Dictionary<string, string>
                {
                    { "Gambles Lost", "1" },
                    { "Money Lost to Gambling", gain.ToString("C2", CurrencyCulture) },
                    { "Net Gain/Loss from Gambling", (-gain).ToString("C2", CurrencyCulture) }
                });
            }
        }

        private async Task<RuntimeResult> GenericGamble(double bet, double odds, double mult, bool exactRoll = false)
        {
            if (bet < 0) return CommandResult.FromError($"{Context.User.Mention}, you can't bet nothing!");

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            double cash = snap.GetValue<double>("cash");

            if (cash < bet) return CommandResult.FromError($"{Context.User.Mention}, you can't bet more than what you have!");

            double roll = Math.Round(random.NextDouble(1, 101), 2);
            bool success = !exactRoll ? roll >= odds : roll.CompareTo(odds) == 0;
            if (success)
            {
                double payout = bet * mult;
                double totalCash = cash + payout;
                await StatUpdate(Context.User, true, payout);
                await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, totalCash);
                await Context.User.NotifyAsync(Context.Channel, $"Good shit my guy! You rolled a {roll} and got yourself **{payout.ToString("C2")}**!" +
                    $"\nBalance: {totalCash.ToString("C2")}");
            }
            else
            {
                double totalCash = (cash - bet) > 0 ? cash - bet : 0;
                await StatUpdate(Context.User, false, bet);
                await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, totalCash);
                await Context.User.NotifyAsync(Context.Channel, $"Well damn, you rolled a {roll}, which wasn't enough. You lost **{bet.ToString("C2")}**." +
                    $"\nBalance: {totalCash.ToString("C2")}");
            }

            return CommandResult.FromSuccess();
        }

        [Command("55x2")]
        [Summary("Roll 55 or higher on a 100 sided die, get 2x what you put in.")]
        [Remarks("$55x2 [bet]")]
        [RequireCash]
        public async Task<RuntimeResult> Roll55(double bet) => await GenericGamble(bet, 55, 1);

        [Command("69.69")]
        [Summary("Roll 69.69 on a 100 sided die, get 6969x what you put in.")]
        [Remarks("$69.69 [bet]")]
        [RequireCash]
        public async Task<RuntimeResult> Roll6969(double bet) => await GenericGamble(bet, 69.69, 6968, true);

        [Command("75+")]
        [Summary("Roll 75 or higher on a 100 sided die, get 3.6x what you put in.")]
        [Remarks("$75+ [bet]")]
        [RequireCash]
        public async Task<RuntimeResult> Roll75(double bet) => await GenericGamble(bet, 75, 2.6);

        [Command("99+")]
        [Summary("Roll 99 or higher on a 100 sided die, get 90x whatyou put in.")]
        [Remarks("$99+ [bet]")]
        [RequireCash]
        public async Task<RuntimeResult> Roll99(double bet) => await GenericGamble(bet, 99, 89);

        [Command("double")]
        [Summary("Double your cash...?")]
        [Remarks("$double")]
        [RequireCash]
        public async Task<RuntimeResult> Double()
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("usingSlots", out bool usingSlots) && usingSlots)
                return CommandResult.FromError($"{Context.User.Mention}, you appear to be currently gambling. I cannot do any transactions at the moment.");
            double cash = snap.GetValue<double>("cash");

            if (random.Next(0, 2) != 0)
            {
                await StatUpdate(Context.User, true, cash);
                await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash * 2);
            }
            else
            {
                await StatUpdate(Context.User, false, cash);
                await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, 0);
            }

            await Context.User.NotifyAsync(Context.Channel, "I have doubled your cash.");
            return CommandResult.FromSuccess();
        }

        [Command("slots")]
        [Summary("Take the slot machine for a spin!")]
        [Remarks("$slots [bet]")]
        [RequireCash]
        public async Task<RuntimeResult> Slots(double bet)
        {
            if (bet < 0) return CommandResult.FromError($"{Context.User.Mention}, you can't bet nothing!");

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            double cash = snap.GetValue<double>("cash");

            if (cash < bet) return CommandResult.FromError($"{Context.User.Mention}, you can't bet more than what you have!");

            if (snap.TryGetValue("usingSlots", out bool usingSlots) && usingSlots) 
                return CommandResult.FromError($"{Context.User.Mention}, you are already using the slot machine!");

            await doc.SetAsync(new { usingSlots = true }, SetOptions.MergeAll);

            await Task.Factory.StartNew(async () =>
            {
                int[] results = new int[4];
                double payoutMult = 1;

                EmbedBuilder embed = new EmbedBuilder
                {
                    Color = Color.Red,
                    Title = "Slots"
                };

                IUserMessage slotMsg = await ReplyAsync(embed: embed.Build());
                for (int i = 0; i < 5; i++)
                {
                    results[0] = random.Next(1, 5);
                    results[1] = random.Next(1, 5);
                    results[2] = random.Next(1, 5);
                    results[3] = random.Next(1, 5);

                    embed.WithDescription($"{emojis[results[0]]} {emojis[results[1]]} {emojis[results[2]]} {emojis[results[3]]}");
                    await slotMsg.ModifyAsync(msg => msg.Embed = embed.Build());

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

                int sevens = results.Count(num => num == 1);
                int apples = results.Count(num => num == 2);
                int grapes = results.Count(num => num == 3);
                int cherries = results.Count(num => num == 4);
                if (sevens == 4) payoutMult = 25;
                else if (apples == 4 || grapes == 4 || cherries == 4) payoutMult = 5;
                else if (ThreeInARow(results, 1)) payoutMult = 3;
                else if (ThreeInARow(results, 2) || ThreeInARow(results, 3) || ThreeInARow(results, 4)) payoutMult = 2;
                else
                {
                    if (TwoInARow(results, 1)) payoutMult += 0.5;
                    if (TwoInARow(results, 2)) payoutMult += 0.25;
                    if (TwoInARow(results, 3)) payoutMult += 0.25;
                    if (TwoInARow(results, 4)) payoutMult += 0.25;
                }

                if (payoutMult > 1)
                {
                    double payout = (bet * payoutMult) - bet;
                    double totalCash = cash + payout;
                    await StatUpdate(Context.User, true, payout);
                    await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, totalCash);

                    if (payoutMult == 25)
                    {
                        await Context.User.NotifyAsync(Context.Channel, $"SWEET BABY JESUS, YOU GOT A MOTHERFUCKING JACKPOT! You won **{payout.ToString("C2")}**!" +
                            $"\nBalance: {totalCash.ToString("C2")}");
                    }
                    else
                    {
                        await Context.User.NotifyAsync(Context.Channel, $"Nicely done! You won **{payout.ToString("C2")}**.\nBalance: {totalCash.ToString("C2")}");
                    }
                }
                else
                {
                    double totalCash = (cash - bet) > 0 ? cash - bet : 0;
                    await StatUpdate(Context.User, false, bet);
                    await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, totalCash);
                    await Context.User.NotifyAsync(Context.Channel, $"You won nothing! Well, you can't win 'em all. You lost **{bet.ToString("C2")}**." +
                        $"\nBalance: {totalCash.ToString("C2")}");
                }

                await doc.SetAsync(new { usingSlots = false }, SetOptions.MergeAll);
            });

            return CommandResult.FromSuccess();
        }
    }
}
