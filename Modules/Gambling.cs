﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using RRBot.Extensions;
using RRBot.Preconditions;
using RRBot.Systems;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace RRBot.Modules
{
    [Summary("Do you want to test your luck? Do you want to probably go broke? Here you go! By the way, you don't need to be 21 or over in this joint ;)")]
    public class Gambling : ModuleBase<SocketCommandContext>
    {
        public CultureInfo CurrencyCulture { get; set; }
        private static readonly Emoji[] emojis = { new("\uD83C\uDF4E"), new("\uD83C\uDF47"), new("7️⃣"),  new("\uD83C\uDF52"),
            new("\uD83C\uDF4A"), new("\uD83C\uDF48"), new("\uD83C\uDF4B") }; // apple, grape, seven, cherry, orange, melon, lemon

        private static bool TwoInARow(int[] results) => results[0] == results[1] || results[1] == results[2];

        private static bool ThreeInARow(int[] results)
        {
            return (results[0] == results[1] && results[1] == results[2]) ||
                (results[0] - 1 == results[1] && results[1] == results[2] + 1) ||
                (results[0] + 1 == results[1] && results[1] == results[2] - 1);
        }

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
            if (bet <= 0.01 || double.IsNaN(bet))
                return CommandResult.FromError("You can't bet nothing!");

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            double cash = snap.GetValue<double>("cash");

            if (cash < bet)
                return CommandResult.FromError("You can't bet more than what you have!");

            double roll = Math.Round(RandomUtil.NextDouble(1, 101), 2);
            if (snap.TryGetValue("perks", out Dictionary<string, long> perks) && perks.Keys.Contains("Speed Demon"))
                odds *= 0.95;
            bool success = !exactRoll ? roll >= odds : roll.CompareTo(odds) == 0;
            if (success)
            {
                double payout = bet * mult;
                double totalCash = cash + payout;
                await StatUpdate(Context.User, true, payout);
                await CashSystem.SetCash(Context.User, Context.Channel, totalCash);
                await Context.User.NotifyAsync(Context.Channel, $"Good shit my guy! You rolled a {roll} and got yourself **{payout:C2}**!" +
                    $"\nBalance: {totalCash:C2}");
            }
            else
            {
                double totalCash = (cash - bet) > 0 ? cash - bet : 0;
                await StatUpdate(Context.User, false, bet);
                await CashSystem.SetCash(Context.User, Context.Channel, totalCash);
                await Context.User.NotifyAsync(Context.Channel, $"Well damn, you rolled a {roll}, which wasn't enough. You lost **{bet:C2}**." +
                    $"\nBalance: {totalCash:C2}");
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
            if (snap.ContainsField("usingSlots"))
                return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");
            double cash = snap.GetValue<double>("cash");

            if (RandomUtil.Next(1, 101) < Constants.DOUBLE_ODDS)
            {
                await StatUpdate(Context.User, true, cash);
                await CashSystem.SetCash(Context.User, Context.Channel, cash * 2);
            }
            else
            {
                await StatUpdate(Context.User, false, cash);
                await CashSystem.SetCash(Context.User, Context.Channel, 0);
            }

            await Context.User.NotifyAsync(Context.Channel, "​I have doubled your cash.");
            return CommandResult.FromSuccess();
        }

        [Command("slots")]
        [Summary("Take the slot machine for a spin!")]
        [Remarks("$slots [bet]")]
        [RequireCash]
        public async Task<RuntimeResult> Slots(double bet)
        {
            if (bet <= 0.01 || double.IsNaN(bet))
                return CommandResult.FromError("You can't bet nothing!");

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            double cash = snap.GetValue<double>("cash");

            if (cash < bet)
                return CommandResult.FromError("You can't bet more than what you have!");
            if (snap.ContainsField("usingSlots"))
                return CommandResult.FromError("You are already using the slot machine!");

            await doc.SetAsync(new { usingSlots = true }, SetOptions.MergeAll);

            await Task.Factory.StartNew(async () =>
            {
                double payoutMult = 1;
                EmbedBuilder embed = new() { Color = Color.Red, Title = "Slots" };
                IUserMessage slotMsg = await ReplyAsync(embed: embed.Build());
                int[] results = new int[3];

                for (int i = 0; i < 5; i++)
                {
                    results[0] = RandomUtil.Next(1, 6);
                    results[1] = RandomUtil.Next(1, 6);
                    results[2] = RandomUtil.Next(1, 6);

                    embed.WithDescription("------------\n" +
                    $"{emojis[results[0] - 1]}  {emojis[results[1] - 1]}  {emojis[results[2] - 1]}\n" +
                    $"{emojis[results[0]]}  {emojis[results[1]]}  {emojis[results[2]]}\n" +
                    $"{emojis[results[0] + 1]}  {emojis[results[1] + 1]}  {emojis[results[2] + 1]}\n" +
                    "------------");
                    await slotMsg.ModifyAsync(msg => msg.Embed = embed.Build());

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

                int sevens = results.Count(num => num == 2);
                if (sevens == 3)
                    payoutMult = Constants.SLOTS_MULT_THREESEVENS;
                else if (ThreeInARow(results))
                    payoutMult = Constants.SLOTS_MULT_THREEINAROW;
                else if (TwoInARow(results))
                    payoutMult = Constants.SLOTS_MULT_TWOINAROW;

                if (payoutMult > 1)
                {
                    double payout = (bet * payoutMult) - bet;
                    double totalCash = cash + payout;
                    await StatUpdate(Context.User, true, payout);
                    await CashSystem.SetCash(Context.User, Context.Channel, totalCash);

                    if (payoutMult == Constants.SLOTS_MULT_THREESEVENS)
                    {
                        await Context.User.NotifyAsync(Context.Channel, $"​SWEET BABY JESUS, YOU GOT A MOTHERFUCKING JACKPOT! You won **{payout:C2}**!" +
                            $"\nBalance: {totalCash:C2}");
                    }
                    else
                    {
                        await Context.User.NotifyAsync(Context.Channel, $"Nicely done! You won **{payout:C2}**.\nBalance: {totalCash:C2}");
                    }
                }
                else
                {
                    double totalCash = (cash - bet) > 0 ? cash - bet : 0;
                    await StatUpdate(Context.User, false, bet);
                    await CashSystem.SetCash(Context.User, Context.Channel, totalCash);
                    await Context.User.NotifyAsync(Context.Channel, $"You won nothing! Well, you can't win 'em all. You lost **{bet:C2}**." +
                        $"\nBalance: {totalCash:C2}");
                }

                await doc.SetAsync(new { usingSlots = FieldValue.Delete }, SetOptions.MergeAll);
            });

            return CommandResult.FromSuccess();
        }
    }
}
