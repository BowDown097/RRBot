namespace RRBot.Modules;
[Summary("All prestige-related stuffs.")]
public class Prestige : ModuleBase<SocketCommandContext>
{
    [Command("prestige")]
    [Summary("Prestige!\n\nUpon prestige, you will **GET**:\n- +20% cash multiplier\n- +50% rank cost multiplier\n- A shiny, cool new badge on $prestigeinfo\n\nand you will **LOSE**:\n- All money, including in crypto investments\n- All cooldowns\n- All items")]
    [RequireCooldown("PrestigeCooldown", "​I can't let you go on and prestige so quickly! Wait {0}.")]
    public async Task<RuntimeResult> DoPrestige()
    {
        DbConfigRanks ranks = await DbConfigRanks.GetById(Context.Guild.Id);
        if (ranks.Ids.Count == 0)
            return CommandResult.FromError("No ranks are configured.");

        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        double prestigeCost = ranks.Costs.OrderBy(kvp => kvp.Value).Select(a => a.Value).Last() * (1 + (0.5 * user.Prestige));
        if (user.Cash < prestigeCost)
            return CommandResult.FromError($"You don't have enough to prestige! You need {prestigeCost:C2}.");
        if (user.Prestige == Constants.MAX_PRESTIGE)
            return CommandResult.FromError($"You have already reached the maximum prestige level of {Constants.MAX_PRESTIGE}.");

        user.BTC = user.Cash = user.ETH = user.LTC = user.XRP = 0;
        user.BullyCooldown = user.ChopCooldown = user.DailyCooldown = user.DealCooldown
            = user.DigCooldown = user.FarmCooldown = user.FishCooldown = user.HackCooldown
            = user.HuntCooldown = user.LootCooldown = user.MineCooldown = user.PacifistCooldown
            = user.RapeCooldown = user.RobCooldown = user.ScavengeCooldown = user.SlaveryCooldown
            = user.SupportCooldown = user.TimeTillCash = user.WhoreCooldown = 0;
        user.Consumables = new();
        user.Crates = user.Tools = new();
        user.Perks = new();
        user.Prestige++;
        user.PrestigeCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.PRESTIGE_COOLDOWN);

        await Context.User.NotifyAsync(Context.Channel, $"Congratulations, homie! You're now Prestige {user.Prestige}. Check $prestigeinfo for your new prestige perks. Hope you said your goodbyes to all of your stuff, cause it's gone!");
        await user.UnlockAchievement("Prestiged!", "Get your first prestige.", Context.User, Context.Channel, 1000);
        if (user.Prestige == Constants.MAX_PRESTIGE)
            await user.UnlockAchievement("Maxed!", "Reach the max prestige.", Context.User, Context.Channel, 1420);
        return CommandResult.FromSuccess();
    }

    [Command("prestigeinfo")]
    [Summary("Check out the perks you're getting from prestige and other info.")]
    public async Task<RuntimeResult> PrestigePerks()
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        if (user.Prestige < 1)
            return CommandResult.FromError("You haven't prestiged yet!");

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithDescription("**Prestige Perks**")
            .WithThumbnailUrl(Constants.PRESTIGE_IMAGES[user.Prestige])
            .RRAddField("Prestige Level", user.Prestige)
            .RRAddField("Cash Multiplier", 1 + (0.20 * user.Prestige) + "x")
            .RRAddField("Rank Cost Multiplier", 1 + (0.5 * user.Prestige) + "x");

        await ReplyAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }
}
