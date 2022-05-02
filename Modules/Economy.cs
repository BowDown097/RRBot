namespace RRBot.Modules;
[Summary("This is the hub for checking and managing your economy stuff. Wanna know how much cash you have? Or what items you have? Or do you want to check out le shoppe? It's all here.")]
public class Economy : ModuleBase<SocketCommandContext>
{
    public static readonly string[] CMDS_WITH_COOLDOWN = { "Deal", "Loot", "Rape", "Rob", "Scavenge",
        "Slavery", "Whore", "Bully",  "Chop", "Dig", "Farm", "Fish", "Hunt", "Mine", "Support", "Hack",
        "Daily", "Prestige" };

    [Alias("bal", "cash")]
    [Command("balance")]
    [Summary("Check your own or someone else's balance.")]
    [Remarks("$bal \"Coalava 🌙#1002\"")]
    public async Task<RuntimeResult> Balance(IGuildUser user = null)
    {
        if (user?.IsBot == true)
            return CommandResult.FromError("Nope.");

        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user?.Id ?? Context.User.Id);
        if (dbUser.Cash < 0.01)
            return CommandResult.FromError(user == null ? "You're broke!" : $"**{user.Sanitize()}** is broke!");

        if (user == null)
            await Context.User.NotifyAsync(Context.Channel, $"You have **{dbUser.Cash:C2}**.");
        else
            await ReplyAsync($"**{user.Sanitize()}** has **{dbUser.Cash:C2}**.", allowedMentions: Constants.MENTIONS);

        return CommandResult.FromSuccess();
    }

    [Alias("cd")]
    [Command("cooldowns")]
    [Summary("Check your own or someone else's crime cooldowns.")]
    [Remarks("$cd Lilpumpfan1")]
    public async Task Cooldowns(IGuildUser user = null)
    {
        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user?.Id ?? Context.User.Id);
        StringBuilder description = new();

        foreach (string cmd in CMDS_WITH_COOLDOWN)
        {
            long cooldown = (long)dbUser[$"{cmd}Cooldown"];
            long cooldownSecs = cooldown - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (cooldownSecs > 0)
                description.AppendLine($"**{cmd}**: {TimeSpan.FromSeconds(cooldownSecs).FormatCompound()}");
        }

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Cooldowns")
            .WithDescription(description.Length > 0 ? description.ToString() : "None");
        await ReplyAsync(embed: embed.Build());
    }

    [Alias("lb")]
    [Command("leaderboard")]
    [Summary("Check the leaderboard for cash or for a specific currency.")]
    [Remarks("$lb btc")]
    public async Task<RuntimeResult> Leaderboard(string currency = "cash")
    {
        string cUp = currency.Equals("cash", StringComparison.OrdinalIgnoreCase) ? "Cash" : currency.ToUpper();
        if (!(cUp is "Cash" or "BTC" or "ETH" or "LTC" or "XRP"))
            return CommandResult.FromError($"**{currency}** is not a currently accepted currency!");

        double cryptoValue = cUp != "Cash" ? await Investments.QueryCryptoValue(cUp) : 0;
        QuerySnapshot users = await Program.database.Collection($"servers/{Context.Guild.Id}/users")
            .OrderByDescending(cUp).GetSnapshotAsync();
        StringBuilder lb = new("*Note: The leaderboard updates every 10 minutes, so stuff may not be up to date.*\n");
        int processedUsers = 0, failedUsers = 0;
        foreach (DocumentSnapshot doc in users.Documents)
        {
            if (processedUsers == 10)
                break;

            SocketGuildUser guildUser = Context.Guild.GetUser(Convert.ToUInt64(doc.Id));
            if (guildUser == null)
            {
                failedUsers++;
                continue;
            }

            DbUser user = await DbUser.GetById(Context.Guild.Id, guildUser.Id, false);
            if (user.Perks.ContainsKey("Pacifist"))
            {
                failedUsers++;
                continue;
            }

            double val = (double)user[cUp];
            if (val < Constants.INVESTMENT_MIN_AMOUNT)
                break;

            if (cUp == "Cash")
                lb.AppendLine($"{processedUsers + 1}: **{guildUser.Sanitize()}**: {val:C2}");
            else
                lb.AppendLine($"{processedUsers + 1}: **{guildUser.Sanitize()}**: {val:0.####} ({cryptoValue * val:C2})");

            processedUsers++;
        }

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle($"{cUp} Leaderboard")
            .WithDescription(lb.Length > 0 ? lb.ToString() : "Nothing to see here!");
        ComponentBuilder component = new ComponentBuilder()
            .WithButton("Back", "dddd", disabled: true)
            .WithButton("Next", $"lbnext-{Context.User.Id}-{cUp}-11-20-{failedUsers}-False", disabled: processedUsers != 10 || users.Documents.Count < 11);
        await ReplyAsync(embed: embed.Build(), components: component.Build());
        return CommandResult.FromSuccess();
    }

    [Command("profile")]
    [Summary("View a bunch of economy-related info on yourself or another user.")]
    [Remarks("$profile zuki")]
    public async Task<RuntimeResult> Profile(IGuildUser user = null)
    {
        if (user?.IsBot == true)
            return CommandResult.FromError("Nope.");

        user ??= Context.User as IGuildUser;
        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithAuthor(user)
            .WithTitle("User Profile")
            .RRAddField("Currencies", BuildPropsList(dbUser, "Cash", "BTC", "ETH", "LTC", "XRP"))
            .RRAddField("Items", BuildPropsList(dbUser, "Tools", "Perks", "Consumables", "Crates"));

        StringBuilder counts = new($"**Achievements**: {dbUser.Achievements.Count}");
        int cooldowns = 0;
        foreach (string cmd in CMDS_WITH_COOLDOWN)
        {
            long cooldown = (long)dbUser[$"{cmd}Cooldown"];
            long cooldownSecs = cooldown - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (cooldownSecs > 0)
                cooldowns++;
        }
        counts.AppendLine($"\n**Commands On Cooldown**: {cooldowns}");

        embed.RRAddField("Counts", counts.ToString());
        embed.RRAddField("Misc", BuildPropsList(dbUser, "GamblingMultiplier", "Prestige"));

        await ReplyAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }

    [Alias("roles")]
    [Command("ranks")]
    [Summary("View all the ranks and their costs.")]
    public async Task Ranks()
    {
        DbConfigRanks ranks = await DbConfigRanks.GetById(Context.Guild.Id);
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        StringBuilder description = new();
        foreach (KeyValuePair<string, double> kvp in ranks.Costs.OrderBy(kvp => int.Parse(kvp.Key)))
        {
            SocketRole role = Context.Guild.GetRole(ranks.Ids[kvp.Key]);
            double cost = kvp.Value * (1 + (0.5 * user.Prestige));
            if (role == null) continue;
            description.AppendLine($"**{role.Name}**: {cost:C2}");
        }

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Available Ranks")
            .WithDescription(description.Length > 0 ? description.ToString() : "None");
        await ReplyAsync(embed: embed.Build());
    }

    [Alias("give", "transfer")]
    [Command("sauce")]
    [Summary("Sauce someone some cash.")]
    [Remarks("$sauce Mateo 1000")]
    public async Task<RuntimeResult> Sauce(IGuildUser user, double amount)
    {
        if (amount < Constants.TRANSACTION_MIN || double.IsNaN(amount))
            return CommandResult.FromError($"You need to sauce at least {Constants.TRANSACTION_MIN:C2}.");
        if (Context.User == user)
            return CommandResult.FromError("You can't sauce yourself money. Don't even know how you would.");
        if (user.IsBot)
            return CommandResult.FromError("Nope.");

        DbUser author = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        DbUser target = await DbUser.GetById(Context.Guild.Id, user.Id);
        if (author.UsingSlots)
            return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");
        if (author.Cash < amount)
            return CommandResult.FromError("You do not have that much money!");

        await author.SetCash(Context.User, author.Cash - amount);
        await target.SetCash(user, target.Cash + amount);

        await Context.User.NotifyAsync(Context.Channel, $"You sauced **{user.Sanitize()}** {amount:C2}.");
        return CommandResult.FromSuccess();
    }

    [Alias("kms", "selfend")]
    [Command("suicide")]
    [Summary("Kill yourself.")]
    public async Task<RuntimeResult> Suicide()
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        DbUser temp = user;
        if (user.UsingSlots)
            return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");

        switch (RandomUtil.Next(4))
        {
            case 0:
                await Context.User.NotifyAsync(Context.Channel, "You attempted to hang yourself, but the rope snapped. You did not die.");
                break;
            case 1:
                await Context.User.NotifyAsync(Context.Channel, "You shot yourself, but somehow the bullet didn't kill you. Lucky or unlucky?");
                break;
            case 2:
                await Context.User.NotifyAsync(Context.Channel, "​DAMN that shotgun made a fucking mess out of you! You're DEAD DEAD, and lost everything.");
                await user.Reference.DeleteAsync();
                await user.SetCash(Context.User, 0);
                RestoreUserData(user, temp.BTC, temp.ETH, temp.LTC, temp.XRP, temp.DMNotifs,
                    temp.NoReplyPings, temp.Stats, temp.DealCooldown, temp.LootCooldown, temp.RapeCooldown,
                    temp.RobCooldown, temp.ScavengeCooldown, temp.SlaveryCooldown, temp.WhoreCooldown, temp.BullyCooldown,
                    temp.ChopCooldown, temp.DigCooldown, temp.FarmCooldown, temp.FishCooldown,
                    temp.HuntCooldown, temp.MineCooldown, temp.SupportCooldown, temp.HackCooldown,
                    temp.DailyCooldown);
                break;
            case 3:
                await Context.User.NotifyAsync(Context.Channel, "It was quite a struggle, but the noose put you out of your misery. You lost everything.");
                await user.Reference.DeleteAsync();
                await user.SetCash(Context.User, 0);
                RestoreUserData(user, temp.BTC, temp.ETH, temp.LTC, temp.XRP, temp.DMNotifs,
                    temp.NoReplyPings, temp.Stats, temp.DealCooldown, temp.LootCooldown, temp.RapeCooldown,
                    temp.RobCooldown, temp.ScavengeCooldown, temp.SlaveryCooldown, temp.WhoreCooldown, temp.BullyCooldown,
                    temp.ChopCooldown, temp.DigCooldown, temp.FarmCooldown, temp.FishCooldown,
                    temp.HuntCooldown, temp.MineCooldown, temp.SupportCooldown, temp.HackCooldown,
                    temp.DailyCooldown);
                break;
        }

        return CommandResult.FromSuccess();
    }

    private static string BuildPropsList(DbUser dbUser, params string[] properties)
    {
        StringBuilder builder = new();
        foreach (string prop in properties)
        {
            object obj = dbUser[prop];
            if (obj is null) continue;

            string propS = prop.SplitPascalCase();
            if (obj is double d && d > 0.01)
                builder.AppendLine($"**{propS}**: {(prop == "Cash" ? d.ToString("C2") : d.ToString("0.####"))}");
            else if (obj is System.Collections.ICollection col && col.Count > 0)
                builder.AppendLine($"**{propS}**: {col.Count}");
            else if (obj is int i && i > 0)
                builder.AppendLine($"**{propS}**: {obj}");
        }

        return builder.ToString();
    }

    private static void RestoreUserData(DbUser user, double btc, double eth, double ltc, double xrp,
        bool dmNotifs, bool noReplyPings, Dictionary<string, string> stats, long dealCd, long lootCd,
        long rapeCd, long robCd, long scavengeCd, long slaveryCd, long whoreCd, long bullyCd, long chopCd, long digCd,
        long farmCd, long fishCd, long huntCd, long mineCd, long supportCd, long hackCd, long dailyCd)
    {
        user.BTC = btc;
        user.ETH = eth;
        user.LTC = ltc;
        user.XRP = xrp;
        user.DMNotifs = dmNotifs;
        user.NoReplyPings = noReplyPings;
        user.Stats = stats;
        user.DealCooldown = dealCd;
        user.LootCooldown = lootCd;
        user.RapeCooldown = rapeCd;
        user.RobCooldown = robCd;
        user.ScavengeCooldown = scavengeCd;
        user.SlaveryCooldown = slaveryCd;
        user.WhoreCooldown = whoreCd;
        user.BullyCooldown = bullyCd;
        user.ChopCooldown = chopCd;
        user.DigCooldown = digCd;
        user.FarmCooldown = farmCd;
        user.FishCooldown = fishCd;
        user.HuntCooldown = huntCd;
        user.MineCooldown = mineCd;
        user.SupportCooldown = supportCd;
        user.HackCooldown = hackCd;
        user.DailyCooldown = dailyCd;
    }
}