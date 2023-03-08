namespace RRBot.Modules;
[Summary("All prestige-related stuffs.")]
public class Prestige : ModuleBase<SocketCommandContext>
{
    public InteractiveService Interactive { get; set; }

    [Command("prestige", RunMode = RunMode.Async)]
    [Summary("Prestige!\n\nUpon prestige, you will **GET**:\n- +20% cash multiplier\n- +50% rank cost multiplier\n- A shiny, cool new badge on $prestigeinfo\n\nand you will **LOSE**:\n- All money, including in crypto investments\n- All cooldowns\n- All items")]
    [RequireCooldown("PrestigeCooldown", "​I can't let you go on and prestige so quickly! Wait {0}.")]
    public async Task<RuntimeResult> DoPrestige()
    {
        DbConfigRanks ranks = await MongoManager.FetchConfigAsync<DbConfigRanks>(Context.Guild.Id);
        if (ranks.Ids.Count == 0)
            return CommandResult.FromError("No ranks are configured.");

        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        decimal prestigeCost = ranks.Costs.OrderBy(kvp => kvp.Value)
            .Select(a => a.Value).Last() * (1 + 0.5m * user.Prestige);
        if (user.Cash < prestigeCost)
            return CommandResult.FromError($"You don't have enough to prestige! You need {prestigeCost:C2}.");
        if (user.Prestige == Constants.MaxPrestige)
            return CommandResult.FromError(
                $"You have already reached the maximum prestige level of {Constants.MaxPrestige}.");

        await Context.User.NotifyAsync(Context.Channel,
            "Are you SURE you want to prestige? If you don't know already,\nyou will **GET**:\n- +20% cash multiplier\n- +50% rank cost multiplier\n- A shiny, cool new badge on $prestigeinfo\n\nand you will **LOSE**:\n- All money, including in crypto investments\n- All cooldowns\n- All items\n**Respond with \"YES\" if you are sure that you want to prestige. You have 20 seconds.**");
        InteractiveResult<SocketMessage> iResult = await Interactive.NextMessageAsync(
            x => x.Channel.Id == Context.Channel.Id && x.Author.Id == Context.User.Id,
            timeout: TimeSpan.FromSeconds(20)
        );
        if (!iResult.IsSuccess || !iResult.Value.Content.Equals("yes", StringComparison.OrdinalIgnoreCase))
            return CommandResult.FromError("Prestige canceled.");

        user.Btc = user.Eth = user.Ltc = user.Xrp = 0;
        user.Collectibles.Clear();
        user.Consumables.Clear();
        user.Crates.Clear();
        user.Perks.Clear();
        user.Tools.Clear();
        user.Weapons.Clear();
        user.UsedConsumables = new Dictionary<string, int>
        {
            { "Black Hat", 0 },
            { "Cocaine", 0 },
            { "Romanian Flag", 0 },
            { "Viagra", 0 }
        };
        user.Prestige++;

        // grab all cds/times via reflection to reduce maintenance of the command
        foreach (PropertyInfo property in typeof(DbUser).GetProperties()
                     .Where(p => p.Name.EndsWith("Cooldown") || p.Name.EndsWith("Time")))
        {
            property.SetValue(user, 0);
        }

        await user.SetCashWithoutAdjustment(Context.User, 0);
        await user.SetCooldown("PrestigeCooldown", Constants.PrestigeCooldown, Context.Guild, Context.User);
        await Context.User.NotifyAsync(Context.Channel,
            $"Congratulations, homie! You're now Prestige {user.Prestige}. Check $prestigeinfo for your new prestige perks. Hope you said your goodbyes to all of your stuff, cause it's gone!");
        await user.UnlockAchievement("Prestiged!", Context.User, Context.Channel);
        if (user.Prestige == Constants.MaxPrestige)
            await user.UnlockAchievement("Maxed!", Context.User, Context.Channel);

        await MongoManager.UpdateObjectAsync(user);
        return CommandResult.FromSuccess();
    }

    [Command("prestigeinfo")]
    [Summary("Check out the perks you're getting from prestige and other info.")]
    public async Task<RuntimeResult> PrestigePerks()
    {
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        if (user.Prestige < 1)
            return CommandResult.FromError("You haven't prestiged yet!");

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithDescription("**Prestige Perks**")
            .WithThumbnailUrl(Constants.PrestigeImages[user.Prestige])
            .RrAddField("Prestige Level", user.Prestige)
            .RrAddField("Cash Multiplier", (1 + 0.2 * user.Prestige).ToString("0.#") + "x")
            .RrAddField("Rank Cost Multiplier", (1 + 0.5 * user.Prestige).ToString("0.#") + "x");

        await ReplyAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }
}
