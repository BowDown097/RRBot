namespace RRBot.Modules;
[Summary("Do you want to test your luck? Do you want to probably go broke? Here you go! By the way, you don't need to be 21 or over in this joint ;)")]
public class Gambling : ModuleBase<SocketCommandContext>
{
    private static readonly Emoji[] Emojis = { new("\uD83C\uDF4E"), new("\uD83C\uDF47"), new("7️⃣"),  new("\uD83C\uDF52"),
        new("\uD83C\uDF4A"), new("\uD83C\uDF48"), new("\uD83C\uDF4B") }; // apple, grape, seven, cherry, orange, melon, lemon

    #region Commands
    [Command("55x2")]
    [Summary("Roll 55 or higher on a 100 sided die, get 2x what you put in.")]
    [Remarks("$55x2 1000")]
    [RequireCash]
    public async Task<RuntimeResult> Roll55(decimal bet) => await GenericGamble(bet, 55, 1);

    [Command("6969")]
    [Summary("Roll 69.69 on a 100 sided die, get 6969x what you put in.")]
    [Remarks("$6969 all")]
    [RequireCash]
    public async Task<RuntimeResult> Roll6969(decimal bet) => await GenericGamble(bet, 69.69, 6968, true);

    [Command("75+")]
    [Summary("Roll 75 or higher on a 100 sided die, get 3.6x what you put in.")]
    [Remarks("$75+ all")]
    [RequireCash]
    public async Task<RuntimeResult> Roll75(decimal bet) => await GenericGamble(bet, 75, 2.6m);

    [Command("99+")]
    [Summary("Roll 99 or higher on a 100 sided die, get 90x what you put in.")]
    [Remarks("$99+ 120")]
    [RequireCash]
    public async Task<RuntimeResult> Roll99(decimal bet) => await GenericGamble(bet, 99, 89);

    [Alias("chuckaluck", "birdcage")]
    [Command("dice")]
    [Summary("Play a simple game of Chuck-a-luck, AKA Birdcage.\nIf you don't know how it works: The player bets on a number. Three dice are rolled. The number appearing once gives a 1:1 payout, twice a 2:1, and thrice a 10:1.")]
    [RequireCash]
    public async Task<RuntimeResult> Dice(decimal bet, int number)
    {
        if (bet < Constants.TransactionMin)
            return CommandResult.FromError($"You need to bet at least {Constants.TransactionMin:C2}.");
        if (number is < 1 or > 6)
            return CommandResult.FromError("Your number needs to be between 1 and 6.");
        
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        if (user.Cash < bet)
            return CommandResult.FromError("You can't bet more than what you have!");

        int[] rolls = { RandomUtil.Next(1, 7), RandomUtil.Next(1, 7), RandomUtil.Next(1, 7) };
        int matches = rolls.Count(r => r == number);
        string rollsText = string.Join(' ', new[]
        {
            Constants.DiceEmojis[rolls[0] - 1],
            Constants.DiceEmojis[rolls[1] - 1],
            Constants.DiceEmojis[rolls[2] - 1]
        });

        StringBuilder description = new(rollsText + "\n\n");
        description.AppendLine(matches switch
        {
            1 => "Good stuff! 1 match. You got paid out your bet.",
            2 => $"DOUBLES! Now we're cooking with gas. You got paid out DOUBLE your bet (**{bet * 2:C2}**).",
            3 => $"WOOOAAHHH! Good shit, man! That's a fine set of **TRIPLES** you just rolled. You got paid out **TEN TIMES** your bet (**{bet * 10:C2}**).",
            _ => $"Well damn! There were no matches with your number. Sucks to be you, because you lost **{bet:C2}**."
        });

        decimal payout = matches >= 1 ? bet * matches : -bet;
        decimal totalCash = user.Cash + payout;
        description.AppendLine($"Balance: {totalCash:C2}");

        if (matches == 3)
            await user.UnlockAchievement("OH BABY A TRIPLE", Context.User, Context.Channel);
        
        if (user.GamblingMultiplier > 1)
        {
            decimal multiplierCash = payout * user.GamblingMultiplier - payout;
            description.AppendLine($"*(+{multiplierCash:C2} from gambling multiplier)*");
            totalCash += multiplierCash;
        }
        
        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Let's see your roll...")
            .WithDescription(description.ToString());
        await ReplyAsync(embed: embed.Build());
        
        StatUpdate(user, matches != 0, matches != 0 ? payout : bet);
        await user.SetCash(Context.User, totalCash);
        await MongoManager.UpdateObjectAsync(user);
        return CommandResult.FromSuccess();
    }

    [Command("double")]
    [Summary("Double your cash...?")]
    [RequireCash]
    public async Task<RuntimeResult> Double()
    {
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        if (RandomUtil.Next(100) < Constants.DoubleOdds)
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
        await MongoManager.UpdateObjectAsync(user);
        return CommandResult.FromSuccess();
    }

    [Command("pot")]
    [Summary("View the pot or add money into the pot.")]
    [Remarks("$pot 2000")]
    public async Task<RuntimeResult> Pot(decimal amount = -1)
    {
        DbPot pot = await MongoManager.FetchPotAsync(Context.Guild.Id);
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);

        if (decimal.IsNegative(amount))
        {
            if (pot.EndTime < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                return CommandResult.FromError("The pot is currently empty.");

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Pot")
                .RrAddField("Total Value", pot.Value.ToString("C2"))
                .RrAddField("Draws At", $"<t:{pot.EndTime}>");

            StringBuilder memberInfo = new();
            foreach (KeyValuePair<ulong, decimal> mem in pot.Members)
            {
                SocketGuildUser guildUser = Context.Guild.GetUser(mem.Key);
                memberInfo.AppendLine($"**{guildUser.Sanitize()}**: {mem.Value:C2} ({pot.GetMemberOdds(mem.Key)}%)");
            }

            embed.RrAddField("Members", memberInfo);
            await ReplyAsync(embed: embed.Build());
        }
        else
        {
            if (amount < Constants.TransactionMin)
                return CommandResult.FromError($"You need to pitch in at least {Constants.TransactionMin:C2}.");
            if (user.Cash < amount)
                return CommandResult.FromError($"You don't have {amount:C2}!");

            if (pot.EndTime < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                pot.EndTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(86400);
                pot.Members.Clear();
                pot.Value = 0;
            }

            pot.Members[Context.User.Id] = pot.Members.TryGetValue(Context.User.Id, out decimal value) ? value + amount : amount;
            pot.Value += amount;

            await Context.User.NotifyAsync(Context.Channel, $"Added **{amount:C2}** into the pot.");
            await user.SetCash(Context.User, user.Cash - amount);

            await MongoManager.UpdateObjectAsync(pot);
            await MongoManager.UpdateObjectAsync(user);
        }

        return CommandResult.FromSuccess();
    }

    [Command("slots", RunMode = RunMode.Async)]
    [Summary("Take the slot machine for a spin!")]
    [Remarks("$slots 4391039")]
    [RequireCash]
    public async Task<RuntimeResult> Slots(decimal bet)
    {
        if (bet < Constants.TransactionMin)
            return CommandResult.FromError($"You need to bet at least {Constants.TransactionMin:C2}.");
        
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        if (user.Cash < bet)
            return CommandResult.FromError("You can't bet more than what you have!");

        user.UsingSlots = true;
        await MongoManager.UpdateObjectAsync(user);

        decimal payoutMult = 1;
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
            $"{Emojis[results[0] - 1]}  {Emojis[results[1] - 1]}  {Emojis[results[2] - 1]}\n" +
            $"{Emojis[results[0]]}  {Emojis[results[1]]}  {Emojis[results[2]]}\n" +
            $"{Emojis[results[0] + 1]}  {Emojis[results[1] + 1]}  {Emojis[results[2] + 1]}\n" +
            "------------");
            await slotMsg.ModifyAsync(msg => msg.Embed = embed.Build());

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        int sevens = results.Count(num => num == 2);
        if (sevens == 3)
            payoutMult = Constants.SlotsMultThreesevens;
        else if (ThreeInARow(results))
            payoutMult = Constants.SlotsMultThreeinarow;
        else if (TwoInARow(results))
            payoutMult = Constants.SlotsMultTwoinarow;

        user.UsingSlots = false;
        await MongoManager.UpdateObjectAsync(user);

        if (payoutMult > 1)
        {
            decimal payout = bet * payoutMult - bet;
            decimal totalCash = user.Cash + payout;
            StatUpdate(user, true, payout);
            string message = $"Nicely done! You won **{payout:C2}**.\nBalance: {totalCash:C2}";

            if (payoutMult == Constants.SlotsMultThreesevens)
            {
                message = $"​SWEET BABY JESUS, YOU GOT A MOTHERFUCKING JACKPOT! You won **{payout:C2}**!\nBalance: {totalCash:C2}";
                await user.UnlockAchievement("Jackpot!", Context.User, Context.Channel);
            }

            if (user.GamblingMultiplier > 1)
            {
                decimal multiplierCash = payout * user.GamblingMultiplier - payout;
                message += $"\n*(+{multiplierCash:C2} from gambling multiplier)*";
                totalCash += multiplierCash;
            }

            await Context.User.NotifyAsync(Context.Channel, message);
            await user.SetCash(Context.User, totalCash);
        }
        else
        {
            decimal totalCash = user.Cash - bet > 0 ? user.Cash - bet : 0;
            StatUpdate(user, false, bet);
            await user.SetCash(Context.User, totalCash);
            if (bet >= 1000000)
                await user.UnlockAchievement("I Just Feel Bad", Context.User, Context.Channel);
            await Context.User.NotifyAsync(Context.Channel, $"You won nothing! Well, you can't win 'em all. You lost **{bet:C2}**." +
                                                            $"\nBalance: {totalCash:C2}");
        }

        await MongoManager.UpdateObjectAsync(user);
        return CommandResult.FromSuccess();
    }
    #endregion

    #region Helpers
    private static bool TwoInARow(int[] results) => results[0] == results[1] || results[1] == results[2];

    private static bool ThreeInARow(int[] results)
    {
        return (results[0] == results[1] && results[1] == results[2]) ||
            (results[0] - 1 == results[1] && results[1] == results[2] + 1) ||
            (results[0] + 1 == results[1] && results[1] == results[2] - 1);
    }

    private static void StatUpdate(DbUser user, bool success, decimal gain)
    {
        CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
        culture.NumberFormat.CurrencyNegativePattern = 2;
        if (success)
        {
            user.AddToStats(new Dictionary<string, string>
            {
                { "Gambles Won", "1" },
                { "Money Gained from Gambling", gain.ToString("C2", culture) },
                { "Net Gain/Loss from Gambling", gain.ToString("C2", culture) }
            });
        }
        else
        {
            user.AddToStats(new Dictionary<string, string>
            {
                { "Gambles Lost", "1" },
                { "Money Lost to Gambling", gain.ToString("C2", culture) },
                { "Net Gain/Loss from Gambling", (-gain).ToString("C2", culture) }
            });
        }
    }

    private async Task<RuntimeResult> GenericGamble(decimal bet, double odds, decimal mult, bool exactRoll = false)
    {
        if (bet < Constants.TransactionMin)
            return CommandResult.FromError($"You need to bet at least {Constants.TransactionMin:C2}.");
            
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        if (user.Cash < bet)
            return CommandResult.FromError("You can't bet more than what you have!");

        double roll = Math.Round(RandomUtil.NextDouble(1, 101), 2);
        if (user.Perks.ContainsKey("Speed Demon"))
            odds *= 1.05;
        bool success = !exactRoll ? roll >= odds : roll.CompareTo(odds) == 0;

        if (success)
        {
            decimal payout = bet * mult;
            decimal totalCash = user.Cash + payout;
            StatUpdate(user, true, payout);
            string message = $"Good shit my guy! You rolled a {roll} and got yourself **{payout:C2}**!\nBalance: {totalCash:C2}";

            if (roll == 99)
                await user.UnlockAchievement("Pretty Damn Lucky", Context.User, Context.Channel);
            else if (odds == 69.69)
                await user.UnlockAchievement("Luckiest Dude Alive", Context.User, Context.Channel);

            if (user.GamblingMultiplier > 1)
            {
                decimal multiplierCash = payout * user.GamblingMultiplier - payout;
                message += $"\n*(+{multiplierCash:C2} from gambling multiplier)*";
                totalCash += multiplierCash;
            }

            await user.SetCash(Context.User, totalCash);
            await Context.User.NotifyAsync(Context.Channel, message);
        }
        else
        {
            decimal totalCash = user.Cash - bet > 0 ? user.Cash - bet : 0;
            StatUpdate(user, false, bet);
            await user.SetCash(Context.User, totalCash);
            if (bet >= 1000000)
                await user.UnlockAchievement("I Just Feel Bad", Context.User, Context.Channel);
            await Context.User.NotifyAsync(Context.Channel, $"Well damn, you rolled a {roll}, which wasn't enough. You lost **{bet:C2}**.\nBalance: {totalCash:C2}");
        }

        await MongoManager.UpdateObjectAsync(user);
        return CommandResult.FromSuccess();
    }
    #endregion
}