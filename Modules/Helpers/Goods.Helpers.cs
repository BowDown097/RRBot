namespace RRBot.Modules;
public partial class Goods
{
    private static async Task GenericUse(
        Consumable con, DbUser user, SocketCommandContext context,
        string successMsg, string loseMsg, string cdKey, long cdDuration,
        decimal divMin = 2, decimal divMax = 5)
    {
        if (RandomUtil.Next(5) == 1)
        {
            user.Consumables[con.Name] = 0;
            user.UsedConsumables[con.Name] = 0;
            decimal lostCash = user.Cash / RandomUtil.NextDecimal(divMin, divMax);
            await user.SetCash(context.User, user.Cash - lostCash);
            await context.User.NotifyAsync(context.Channel, string.Format(loseMsg, lostCash));
            return;
        }

        await context.User.NotifyAsync(context.Channel, successMsg);
        user[cdKey] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(cdDuration);
    }
}