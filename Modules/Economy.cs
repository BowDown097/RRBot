namespace RRBot.Modules;
[Summary("This is the hub for checking and managing your economy stuff. Wanna know how much cash you have? Or what items you have? Or do you want to check out le shoppe? It's all here.")]
public partial class Economy : ModuleBase<SocketCommandContext>
{
    public static readonly string[] CmdsWithCooldown =
    [
        "Deal", "Loot", "Rape", "Rob", "Scavenge", "Slavery", "Whore", "Bully",
        "Chop", "Dig", "Farm", "Fish", "Hunt", "Mine", "Hack", "Daily", "Prestige", "Shoot"
    ];

    [Alias("bal", "cash")]
    [Command("balance")]
    [Summary("Check your own or someone else's balance.")]
    [Remarks("$bal \"Coalava 🌙#1002\"")]
    public async Task<RuntimeResult> Balance([Remainder] IGuildUser user = null)
    {
        if (user?.IsBot == true)
            return CommandResult.FromError("Nope.");
        
        DbUser dbUser = await MongoManager.FetchUserAsync(user?.Id ?? Context.User.Id, Context.Guild.Id);
        if (dbUser.Cash < 0.01m)
            return CommandResult.FromError(user == null ? "You're broke!" : $"**{user.Sanitize()}** is broke!");

        await Context.User.NotifyAsync(Context.Channel, user == null ? $"You have **{dbUser.Cash:C2}**." : $"**{user.Sanitize()}** has **{dbUser.Cash:C2}**.");
        return CommandResult.FromSuccess();
    }

    [Alias("cd")]
    [Command("cooldowns")]
    [Summary("Check your own or someone else's crime cooldowns.")]
    [Remarks("$cd Lilpumpfan1")]
    public async Task Cooldowns([Remainder] IGuildUser user = null)
    {
        DbUser dbUser = await MongoManager.FetchUserAsync(user?.Id ?? Context.User.Id, Context.Guild.Id);
        StringBuilder description = new();

        foreach (string cmd in CmdsWithCooldown)
        {
            long cooldownSecs = (long)dbUser[$"{cmd}Cooldown"] - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
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
        string cUp = currency.Equals("cash", StringComparison.OrdinalIgnoreCase) ? "Cash" : currency.ToTitleCase();
        if (cUp is not ("Cash" or "Btc" or "Eth" or "Ltc" or "Xrp"))
            return CommandResult.FromError($"**{currency}** is not a currently accepted currency!");

        decimal cryptoValue = cUp != "Cash" ? await Investments.QueryCryptoValue(cUp.ToUpper()) : 0;
        SortDefinition<DbUser> sort = Builders<DbUser>.Sort.Descending(cUp);
        FindOptions<DbUser> opts = new()
        {
            Collation = new Collation("en", numericOrdering: true),
            Sort = sort
        };
        IAsyncCursor<DbUser> cursor = 
            await MongoManager.Users.FindAsync(u => u.GuildId == Context.Guild.Id, opts);
        List<DbUser> users = await cursor.ToListAsync();

        StringBuilder lb = new();
        int processedUsers = 0, failedUsers = 0;
        foreach (DbUser user in users)
        {
            if (processedUsers == 10)
                break;

            SocketGuildUser guildUser = Context.Guild.GetUser(user.UserId);
            if (guildUser == null || user.Perks.ContainsKey("Pacifist"))
            {
                failedUsers++;
                continue;
            }

            decimal val = (decimal)user[cUp];
            if (val < Constants.InvestmentMinAmount)
                break;

            lb.AppendLine(cUp == "Cash"
                ? $"{processedUsers + 1}: **{guildUser.Sanitize()}**: {val:C2}"
                : $"{processedUsers + 1}: **{guildUser.Sanitize()}**: {val:0.####} ({cryptoValue * val:C2})");

            processedUsers++;
        }

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle(cUp == "Cash" ? "Leaderboard" : $"{cUp.ToUpper()} Leaderboard")
            .WithDescription(lb.Length > 0 ? lb.ToString() : "Nothing to see here!");
        ComponentBuilder component = new ComponentBuilder()
            .WithButton("Back", "dddd", disabled: true)
            .WithButton("Next", $"lbnext-{Context.User.Id}-{cUp}-11-20-{failedUsers}-False",
                disabled: processedUsers != 10 || users.Count < 11);
        await ReplyAsync(embed: embed.Build(), components: component.Build());

        return CommandResult.FromSuccess();
    }

    [Command("profile")]
    [Summary("View a bunch of economy-related info on yourself or another user.")]
    [Remarks("$profile zuki")]
    public async Task<RuntimeResult> Profile([Remainder] IGuildUser user = null)
    {
        if (user?.IsBot == true)
            return CommandResult.FromError("Nope.");

        user ??= Context.User as IGuildUser;
        DbUser dbUser = await MongoManager.FetchUserAsync(user.Id, Context.Guild.Id);
        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithAuthor(user)
            .WithTitle("User Profile")
            .RrAddField("Essentials", BuildPropsList(dbUser, "Cash", "Gang", "Health"))
            .RrAddField("Crypto", BuildPropsList(dbUser, "BTC", "ETH", "LTC", "XRP"))
            .RrAddField("Items", BuildPropsList(dbUser, "Tools", "Perks", "Consumables", "Crates"))
            .RrAddField("Active Consumables", string.Join('\n', dbUser.UsedConsumables.Where(c => c.Value > 0).Select(c => $"**{c.Key}**: {c.Value}x")));

        StringBuilder counts = new($"**Achievements**: {dbUser.Achievements.Count}");
        int cooldowns = CmdsWithCooldown.Select(cmd => (long)dbUser[$"{cmd}Cooldown"] - DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            .Count(cooldownSecs => cooldownSecs > 0);
        counts.AppendLine($"\n**Commands On Cooldown**: {cooldowns}");

        embed.RrAddField("Counts", counts.ToString());
        embed.RrAddField("Misc", BuildPropsList(dbUser, "GamblingMultiplier", "Prestige"));

        await ReplyAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }

    [Alias("roles")]
    [Command("ranks")]
    [Summary("View all the ranks and their costs.")]
    public async Task Ranks()
    {
        DbConfigRanks ranks = await MongoManager.FetchConfigAsync<DbConfigRanks>(Context.Guild.Id);
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        StringBuilder description = new();
        foreach (KeyValuePair<int, decimal> kvp in ranks.Costs.OrderBy(kvp => kvp.Key))
        {
            SocketRole role = Context.Guild.GetRole(ranks.Ids[kvp.Key]);
            decimal cost = kvp.Value * (1 + 0.5m * user.Prestige);
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
    public async Task<RuntimeResult> Sauce(IGuildUser user, decimal amount)
    {
        if (amount < Constants.TransactionMin)
            return CommandResult.FromError($"You need to sauce at least {Constants.TransactionMin:C2}.");
        if (Context.User == user)
            return CommandResult.FromError("You can't sauce yourself money. Don't even know how you would.");
        if (user.IsBot)
            return CommandResult.FromError("Nope.");
        
        DbUser author = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        DbUser target = await MongoManager.FetchUserAsync(user.Id, Context.Guild.Id);
        if (target.UsingSlots)
            return CommandResult.FromError($"**{user.Sanitize()}** is currently gambling. They cannot do any transactions at the moment.");
        if (author.Cash < amount)
            return CommandResult.FromError("You do not have that much money!");

        await author.SetCash(Context.User, author.Cash - amount);
        await target.SetCash(user, target.Cash + amount);

        await Context.User.NotifyAsync(Context.Channel, $"You sauced **{user.Sanitize()}** {amount:C2}.");
        await MongoManager.UpdateObjectAsync(author);
        await MongoManager.UpdateObjectAsync(target);
        return CommandResult.FromSuccess();
    }
}