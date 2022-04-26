namespace RRBot.Modules;
[Summary("Do you want to test your luck? Do you want to probably go broke? Here you go! By the way, you don't need to be 21 or over in this joint ;)")]
public class Gambling : ModuleBase<SocketCommandContext>
{
    private static readonly Emoji[] emojis = { new("\uD83C\uDF4E"), new("\uD83C\uDF47"), new("7️⃣"),  new("\uD83C\uDF52"),
        new("\uD83C\uDF4A"), new("\uD83C\uDF48"), new("\uD83C\uDF4B") }; // apple, grape, seven, cherry, orange, melon, lemon

    [Command("55x2")]
    [Summary("Roll 55 or higher on a 100 sided die, get 2x what you put in.")]
    [Remarks("$55x2 1000")]
    [RequireCash]
    public async Task<RuntimeResult> Roll55(double bet) => await GenericGamble(bet, 55, 1);

    [Command("6969")]
    [Summary("Roll 69.69 on a 100 sided die, get 6969x what you put in.")]
    [Remarks("$6969 all")]
    [RequireCash]
    public async Task<RuntimeResult> Roll6969(double bet) => await GenericGamble(bet, 69.69, 6968, true);

    [Command("75+")]
    [Summary("Roll 75 or higher on a 100 sided die, get 3.6x what you put in.")]
    [Remarks("$75+ all")]
    [RequireCash]
    public async Task<RuntimeResult> Roll75(double bet) => await GenericGamble(bet, 75, 2.6);

    [Command("99+")]
    [Summary("Roll 99 or higher on a 100 sided die, get 90x whatyou put in.")]
    [Remarks("$99+ 120")]
    [RequireCash]
    public async Task<RuntimeResult> Roll99(double bet) => await GenericGamble(bet, 99, 89);

    [Command("double")]
    [Summary("Double your cash...?")]
    [RequireCash]
    public async Task<RuntimeResult> Double()
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        if (user.UsingSlots)
            return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");

        if (RandomUtil.Next(1, 101) < Constants.DOUBLE_ODDS)
        {
            StatUpdate(user, true, user.Cash);
            await user.SetCash(Context.User, user.Cash * 2);
        }
        else
        {
            StatUpdate(user, false, user.Cash);
            await user.SetCash(Context.User, 0);
        }

        await Context.User.NotifyAsync(Context.Channel, "​I have doubled your cash.");
        return CommandResult.FromSuccess();
    }

    [Command("pot")]
    [Summary("View the pot or add money into the pot.")]
    [Remarks("$pot 2000")]
    public async Task<RuntimeResult> Pot(double amount = double.NaN)
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        DbPot pot = await DbPot.GetById(Context.Guild.Id);

        if (double.IsNaN(amount))
        {
            if (pot.EndTime < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                return CommandResult.FromError("The pot is currently empty.");

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Pot")
                .RRAddField("Total Value", pot.Value.ToString("C2"))
                .RRAddField("Draws At", $"<t:{pot.EndTime}>");

            StringBuilder memberInfo = new();
            foreach (KeyValuePair<string, double> mem in pot.Members)
            {
                SocketGuildUser guildUser = Context.Guild.GetUser(Convert.ToUInt64(mem.Key));
                memberInfo.AppendLine($"**{guildUser.Sanitize()}**: {mem.Value:C2} ({pot.GetMemberOdds(mem.Key)}%)");
            }

            embed.RRAddField("Members", memberInfo);
            await ReplyAsync(embed: embed.Build());
        }
        else
        {
            if (user.UsingSlots)
                return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");
            if (amount < Constants.TRANSACTION_MIN)
                return CommandResult.FromError($"You need to pitch in at least {Constants.TRANSACTION_MIN:C2}.");
            if (user.Cash < amount)
                return CommandResult.FromError($"You don't have {amount:C2}!");

            if (pot.EndTime < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                pot.EndTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(86400);
                pot.Members = new();
                pot.Value = 0;
            }

            string userId = Context.User.Id.ToString();
            pot.Members[userId] = pot.Members.TryGetValue(userId, out double value) ? value + amount : amount;
            pot.Value += amount;
            await Context.User.NotifyAsync(Context.Channel, $"Added **{amount:C2}** into the pot.");
            await user.SetCash(Context.User, user.Cash - amount);
        }

        return CommandResult.FromSuccess();
    }

    [Command("slots", RunMode = RunMode.Async)]
    [Summary("Take the slot machine for a spin!")]
    [Remarks("$slots 4391039")]
    [RequireCash]
    public async Task<RuntimeResult> Slots(double bet)
    {
        if (bet < Constants.TRANSACTION_MIN || double.IsNaN(bet))
            return CommandResult.FromError($"You need to bet at least {Constants.TRANSACTION_MIN:C2}.");

        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);

        if (user.Cash < bet)
            return CommandResult.FromError("You can't bet more than what you have!");
        if (user.UsingSlots)
            return CommandResult.FromError("You are already using the slot machine!");

        user.UsingSlots = true;

        double payoutMult = 1;
        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Slots");
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

        user.UsingSlots = false;
        if (payoutMult > 1)
        {
            double payout = (bet * payoutMult) - bet;
            double totalCash = user.Cash + payout;
            StatUpdate(user, true, payout);
            await user.SetCash(Context.User, totalCash);

            if (payoutMult == Constants.SLOTS_MULT_THREESEVENS)
            {
                await Context.User.NotifyAsync(Context.Channel, $"​SWEET BABY JESUS, YOU GOT A MOTHERFUCKING JACKPOT! You won **{payout:C2}**!" +
                    $"\nBalance: {totalCash:C2}");
                await user.UnlockAchievement("Jackpot!", "Get a jackpot with $slots.", Context.User, Context.Channel);
            }
            else
            {
                await Context.User.NotifyAsync(Context.Channel, $"Nicely done! You won **{payout:C2}**.\nBalance: {totalCash:C2}");
            }
        }
        else
        {
            double totalCash = (user.Cash - bet) > 0 ? user.Cash - bet : 0;
            StatUpdate(user, false, bet);
            await user.SetCash(Context.User, totalCash);
            await Context.User.NotifyAsync(Context.Channel, $"You won nothing! Well, you can't win 'em all. You lost **{bet:C2}**." +
                $"\nBalance: {totalCash:C2}");
        }

        return CommandResult.FromSuccess();
    }

    private static bool TwoInARow(int[] results) => results[0] == results[1] || results[1] == results[2];

    private static bool ThreeInARow(int[] results)
    {
        return (results[0] == results[1] && results[1] == results[2]) ||
            (results[0] - 1 == results[1] && results[1] == results[2] + 1) ||
            (results[0] + 1 == results[1] && results[1] == results[2] - 1);
    }

    private static void StatUpdate(DbUser user, bool success, double gain)
    {
        CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
        culture.NumberFormat.CurrencyNegativePattern = 2;
        if (success)
        {
            user.AddToStats(new()
            {
                { "Gambles Won", "1" },
                { "Money Gained from Gambling", gain.ToString("C2", culture) },
                { "Net Gain/Loss from Gambling", gain.ToString("C2", culture) }
            });
        }
        else
        {
            user.AddToStats(new()
            {
                { "Gambles Lost", "1" },
                { "Money Lost to Gambling", gain.ToString("C2", culture) },
                { "Net Gain/Loss from Gambling", (-gain).ToString("C2", culture) }
            });
        }
    }

    private async Task<RuntimeResult> GenericGamble(double bet, double odds, double mult, bool exactRoll = false)
    {
        if (bet < Constants.TRANSACTION_MIN || double.IsNaN(bet))
            return CommandResult.FromError($"You need to bet at least {Constants.TRANSACTION_MIN:C2}.");

        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        if (user.UsingSlots)
            return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");
        if (user.Cash < bet)
            return CommandResult.FromError("You can't bet more than what you have!");

        double roll = Math.Round(RandomUtil.NextDouble(1, 101), 2);
        if (user.Perks.ContainsKey("Speed Demon"))
            odds *= 1.05;
        bool success = !exactRoll ? roll >= odds : roll.CompareTo(odds) == 0;
        if (success)
        {
            double payout = bet * mult;
            double totalCash = user.Cash + payout;
            StatUpdate(user, true, payout);
            await user.SetCash(Context.User, totalCash);
            await Context.User.NotifyAsync(Context.Channel, $"Good shit my guy! You rolled a {roll} and got yourself **{payout:C2}**!" +
                $"\nBalance: {totalCash:C2}");
            if (odds == 99)
                await user.UnlockAchievement("Pretty Damn Lucky", "Win $99+.", Context.User, Context.Channel);
            else if (odds == 69.69)
                await user.UnlockAchievement("Luckiest Dude Alive", "Win $69.69.", Context.User, Context.Channel);
        }
        else
        {
            double totalCash = (user.Cash - bet) > 0 ? user.Cash - bet : 0;
            StatUpdate(user, false, bet);
            await user.SetCash(Context.User, totalCash);
            await Context.User.NotifyAsync(Context.Channel, $"Well damn, you rolled a {roll}, which wasn't enough. You lost **{bet:C2}**." +
                $"\nBalance: {totalCash:C2}");
        }

        return CommandResult.FromSuccess();
    }
}