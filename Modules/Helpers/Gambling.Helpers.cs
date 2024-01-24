namespace RRBot.Modules;
public partial class Gambling
{
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
}