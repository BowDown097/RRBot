namespace RRBot.Modules;
public partial class Crime
{
    private async Task<RuntimeResult> GenericCrime(string[] successOutcomes, string[] failOutcomes, string cdKey,
        long duration, bool hasMehOutcome = false)
    {
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        double winOdds = user.Perks.ContainsKey("Speed Demon") ? Constants.GenericCrimeWinOdds * 0.95 : Constants.GenericCrimeWinOdds;
        if (RandomUtil.NextDouble(100) < winOdds)
        {
            int outcomeNum = RandomUtil.Next(successOutcomes.Length);
            string outcome = successOutcomes[outcomeNum];
            decimal moneyEarned = RandomUtil.NextDecimal(Constants.GenericCrimeWinMin, Constants.GenericCrimeWinMax);
            if (hasMehOutcome && outcomeNum == successOutcomes.Length - 1)
                moneyEarned /= 5;
            decimal totalCash = user.Cash + moneyEarned;

            StatUpdate(user, true, moneyEarned);
            await user.SetCash(Context.User, totalCash, Context.Channel, string.Format($"{outcome}\nBalance: {totalCash:C2}", moneyEarned.ToString("C2")));
        }
        else
        {
            string outcome = RandomUtil.GetRandomElement(failOutcomes);
            decimal lostCash = RandomUtil.NextDecimal(Constants.GenericCrimeLossMin, Constants.GenericCrimeLossMax);
            lostCash = user.Cash - lostCash < 0 ? lostCash - Math.Abs(user.Cash - lostCash) : lostCash;
            decimal totalCash = user.Cash - lostCash > 0 ? user.Cash - lostCash : 0;

            StatUpdate(user, false, lostCash);
            await user.SetCash(Context.User, totalCash);
            await Context.User.NotifyAsync(Context.Channel, string.Format($"{outcome}\nBalance: {totalCash:C2}", lostCash.ToString("C2")));
        }

        if (RandomUtil.NextDouble(1, 101) < Constants.GenericCrimeToolOdds)
        {
            string[] availableTools = Constants.Tools.Where(t => !user.Tools.Contains(t.Name)).Select(t => t.Name).ToArray();
            if (availableTools.Length > 0)
            {
                string tool = RandomUtil.GetRandomElement(availableTools);
                user.Tools.Add(tool);
                await ReplyAsync($"Well I'll be damned! You also got yourself a(n) {tool}! Check out ``$module tasks`` to see how you can use it.");
            }
        }

        await user.SetCooldown(cdKey, duration, Context.User);
        await MongoManager.UpdateObjectAsync(user);
        return CommandResult.FromSuccess();
    }

    private async Task HandleScavenge(IUserMessage msg, InteractiveResult result, DbUser user, bool successCondition, string successResponse, string timeoutResponse, string failureResponse)
    {
        if (!result.IsSuccess || result.IsTimeout)
        {
            EmbedBuilder timeoutEmbed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle(msg.Embeds.First().Title)
                .WithDescription(timeoutResponse);
            await msg.ModifyAsync(x => x.Embed = timeoutEmbed.Build());
        }
        else if (successCondition)
        {
            decimal rewardCash = RandomUtil.NextDecimal(Constants.ScavengeMinCash, Constants.ScavengeMaxCash);
            decimal prestigeCash = rewardCash * 0.20m * user.Prestige;
            decimal totalCash = user.Cash + rewardCash + prestigeCash;
            EmbedBuilder successEmbed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle(msg.Embeds.First().Title)
                .WithDescription(successResponse + $" Here's {rewardCash:C2}.\nBalance: {totalCash:C2}\n{(prestigeCash != 0 ? $"*(+{prestigeCash:C2} from Prestige)*" : "")}");
            await msg.ModifyAsync(x => x.Embed = successEmbed.Build());
            await user.SetCash(Context.User, totalCash);
        }
        else
        {
            EmbedBuilder failureEmbed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle(msg.Embeds.First().Title)
                .WithDescription(failureResponse);
            await msg.ModifyAsync(x => x.Embed = failureEmbed.Build());
        }
    }

    private static string ScrambleWord(Match match)
    {
        double[] keys = new double[match.Value.Length];
        char[] letters = new char[match.Value.Length];
        for (int ctr = 0; ctr < match.Value.Length; ctr++)
        {
            keys[ctr] = RandomUtil.NextDouble(2);
            letters[ctr] = match.Value[ctr];
        }
        Array.Sort(keys, letters, 0, match.Value.Length);
        return new string(letters);
    }

    private static void StatUpdate(DbUser user, bool success, decimal gain)
    {
        CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
        culture.NumberFormat.CurrencyNegativePattern = 2;
        if (success)
        {
            user.AddToStats(new Dictionary<string, string>
            {
                { "Crimes Succeeded", "1" },
                { "Money Gained from Crimes", gain.ToString("C2", culture) },
                { "Net Gain/Loss from Crimes", gain.ToString("C2", culture) }
            });
        }
        else
        {
            user.AddToStats(new Dictionary<string, string>
            {
                { "Crimes Failed", "1" },
                { "Money Lost to Crimes", gain.ToString("C2", culture) },
                { "Net Gain/Loss from Crimes", (-gain).ToString("C2", culture) }
            });
        }
    }
}