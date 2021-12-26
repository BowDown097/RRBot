﻿namespace RRBot.Modules;
[Summary("This is the hub for checking and managing your economy stuff. Wanna know how much cash you have? Or what items you have? Or do you want to check out le shoppe? It's all here.")]
public class Economy : ModuleBase<SocketCommandContext>
{
    public static readonly string[] CMDS_WITH_COOLDOWN = { "Deal", "Loot", "Rape", "Rob", "Slavery", "Whore", "Bully",
        "Chop", "Dig", "Farm", "Fish", "Hunt", "Mine", "Support", "Hack", "Daily" };

    private static void RestoreUserData(DbUser user, double btc, double doge, double eth, double ltc, double xrp,
        bool dmNotifs, bool noReplyPings, Dictionary<string, string> stats, long dealCd, long lootCd,
        long rapeCd, long robCd, long slaveryCd, long whoreCd, long bullyCd, long chopCd, long digCd,
        long farmCd, long fishCd, long huntCd, long mineCd, long supportCd, long hackCd, long dailyCd)
    {
        user.BTC = btc;
        user.DOGE = doge;
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

    [Alias("bal", "cash")]
    [Command("balance")]
    [Summary("Check your own or someone else's balance.")]
    [Remarks("$balance <user>")]
    public async Task<RuntimeResult> Balance(IGuildUser user = null)
    {
        if (user?.IsBot == true)
            return CommandResult.FromError("Nope.");

        ulong userId = user == null ? Context.User.Id : user.Id;
        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, userId);
        if (dbUser.Cash < 0.01)
            return CommandResult.FromError(user == null ? "You're broke!" : $"**{user.Sanitize()}** is broke!");

        if (user == null)
            await Context.User.NotifyAsync(Context.Channel, $"You have **{dbUser.Cash:C2}**.");
        else
            await ReplyAsync($"**{user.Sanitize()}** has **{dbUser.Cash:C2}**.");

        return CommandResult.FromSuccess();
    }

    [Alias("purchase")]
    [Command("buy")]
    [Summary("Buy an item or perk from the shop.")]
    [Remarks("$buy [item]")]
    public async Task<RuntimeResult> Buy([Remainder] string item)
    {
        if (ItemSystem.items.Any(i => i == item))
            return await ItemSystem.BuyItem(item, Context.User, Context.Guild, Context.Channel);
        else if (ItemSystem.perks.Any(perk => perk.name == item))
            return await ItemSystem.BuyPerk(item, Context.User, Context.Guild, Context.Channel);
        else
            return CommandResult.FromError($"**{item}** is not a valid item or perk!\n*Tip: This command is case sensitive.*");
    }

    [Alias("cd")]
    [Command("cooldowns")]
    [Summary("Check your own or someone else's crime cooldowns.")]
    [Remarks("$cooldowns <user>")]
    public async Task Cooldowns(IGuildUser user = null)
    {
        ulong userId = user == null ? Context.User.Id : user.Id;
        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, userId);
        StringBuilder description = new();
        double mult = dbUser.Perks.ContainsKey("Speed Demon") ? 0.85 : 1;

        foreach (string cmd in CMDS_WITH_COOLDOWN)
        {
            long cooldown = (long)dbUser[$"{cmd}Cooldown"];
            long fullCd = (long)((cooldown - DateTimeOffset.UtcNow.ToUnixTimeSeconds()) * mult);
            if (fullCd > 0L)
                description.AppendLine($"**{cmd}**: {TimeSpan.FromSeconds(fullCd).FormatCompound()}");
        }

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle(user == null ? "Cooldowns" : $"{user.Sanitize()}'s Cooldowns")
            .WithDescription(description.Length > 0 ? description.ToString() : "None");
        await ReplyAsync(embed: embed.Build());
    }

    [Command("daily")]
    [Summary("Get a daily reward.")]
    [Remarks("$daily")]
    [RequireCooldown("DailyCooldown", "Slow down there, turbo! It hasn't been a day yet. You've still got {0} left.")]
    [RequireRankLevel("3")]
    public async Task<RuntimeResult> Daily()
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        if (user.UsingSlots)
            return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");

        double moneyEarned = RandomUtil.NextDouble(Constants.DAILY_MIN, Constants.DAILY_MAX);
        double totalCash = user.Cash + moneyEarned;
        string message = RandomUtil.Next(5) switch
        {
            0 => $"Your job at Pierce & Pierce is paying exceptionally well, and business is looking fantastic. **{moneyEarned:C2}** for a pretty mild day of work. That's what I'm talkin' bout.\nBalance: {totalCash:C2}",
            1 => $"Quite a long day of disabling evil right-wingers' Discord accounts, but hey, you got yourself **{moneyEarned:C2}**. Least it's paying better than the furry shoots you were doing for quite a while.\nBalance: {totalCash:C2}",
            2 => $"The OnlyFans money is pouring in! **{moneyEarned:C2}** from some lonely suckers just today! Thank God for giving you such a big ass.\nBalance: {totalCash:C2}",
            3 => $"Another day of slouching on the couch and leeching off taxpayer money has gotten you **{moneyEarned:C2}**.\nBalance: {totalCash:C2}",
            4 => $"Hot dayum! **{moneyEarned:C2}** from simp donations on your hot tub stream tonight! Your (also simp) boyfriend is gonna be ecstatic.\nBalance: {totalCash:C2}",
            _ => ""
        };

        await user.SetCash(Context.User, totalCash);
        user.DailyCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.DAILY_COOLDOWN);
        await Context.User.NotifyAsync(Context.Channel, message);
        return CommandResult.FromSuccess();
    }

    [Alias("sell")]
    [Command("discard")]
    [Summary("Discard an item or the Pacifist perk.")]
    [Remarks("$discard [item]")]
    public async Task<RuntimeResult> Discard([Remainder] string item)
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        if (user.UsingSlots)
            return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");

        if (item == "Pacifist")
        {
            if (!user.Perks.ContainsKey("Pacifist"))
                return CommandResult.FromError("You do not have the Pacifist perk!");

            user.Perks.Remove("Pacifist");
            user.PacifistCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(259200);
            await Context.User.NotifyAsync(Context.Channel, "You discarded your Pacifist perk. If you wish to buy it again, you will have to wait 3 days.");
        }
        else if (user.Items.Remove(item))
        {
            double price = ItemSystem.ComputeItemPrice(item) / 1.5;
            await user.SetCash(Context.User, user.Cash + price);
            await Context.User.NotifyAsync(Context.Channel, $"You sold your {item} to some dude for **{price:C2}**.");
        }
        else
        {
            return CommandResult.FromError($"You do not have a(n) {item}!" +
                "\n*Tip: This command is case sensitive and does not accept perks other than Pacifist.*");
        }

        return CommandResult.FromSuccess();
    }

    [Command("items")]
    [Summary("Check your own or someone else's items.")]
    [Remarks("$items <user>")]
    [RequireItem]
    public async Task GetItems(IGuildUser user = null)
    {
        ulong userId = user == null ? Context.User.Id : user.Id;
        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, userId);
        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle(user == null ? "Items" : $"{user.Sanitize()}'s Items")
            .WithDescription(string.Join(", ", dbUser.Items));
        await ReplyAsync(embed: embed.Build());
    }

    [Alias("lb")]
    [Command("leaderboard")]
    [Summary("Check the leaderboard for cash or for a specific currency.")]
    [Remarks("$leaderboard <currency>")]
    public async Task<RuntimeResult> Leaderboard(string currency = "cash")
    {
        string cUp = currency.Equals("cash", StringComparison.OrdinalIgnoreCase) ? "Cash" : currency.ToUpper();
        if (!cUp.In("Cash", "BTC", "DOGE", "ETH", "LTC", "XRP"))
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

    [Command("perks")]
    [Summary("View info about your currently active perks.")]
    [Remarks("$perks")]
    [RequirePerk]
    public async Task Perks()
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        StringBuilder perksBuilder = new();
        foreach (KeyValuePair<string, long> kvp in user.Perks.OrderBy(p => p.Key))
        {
            if (kvp.Value <= DateTimeOffset.UtcNow.ToUnixTimeSeconds() && kvp.Key != "Pacifist")
                return;

            Perk perk = Array.Find(ItemSystem.perks, p => p.name.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));
            perksBuilder.AppendLine($"**{perk.name}**: {perk.description}" +
                $"\nTime Left: {(perk.name != "Pacifist" ? TimeSpan.FromSeconds(kvp.Value - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).FormatCompound() : "Indefinite")}");
        }

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Perks")
            .WithDescription(perksBuilder.Length > 0 ? perksBuilder.ToString() : "None");
        await ReplyAsync(embed: embed.Build());
    }

    [Alias("roles")]
    [Command("ranks")]
    [Summary("View all the ranks and their costs.")]
    [Remarks("$ranks")]
    public async Task Ranks()
    {
        DbConfigRanks ranks = await DbConfigRanks.GetById(Context.Guild.Id);
        StringBuilder description = new();
        foreach (KeyValuePair<string, double> kvp in ranks.Costs.OrderBy(kvp => int.Parse(kvp.Key)))
        {
            SocketRole role = Context.Guild.GetRole(ranks.Ids[kvp.Key]);
            if (role == null) continue;
            description.AppendLine($"**{role.Name}**: {kvp.Value:C2}");
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
    [Remarks("$sauce [user] [amount]")]
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

    [Command("shop")]
    [Summary("Check out what's available for purchase in the shop.")]
    [Remarks("$shop <items|perks>")]
    public async Task Shop(string category = "")
    {
        StringBuilder items = new();
        StringBuilder perks = new();

        foreach (string item in ItemSystem.items)
        {
            double price = ItemSystem.ComputeItemPrice(item);
            items.AppendLine($"**{item}**: {price:C2}");
        }

        foreach (Perk perk in ItemSystem.perks)
            perks.AppendLine($"**{perk.name}**: {perk.description}\nDuration: {TimeSpan.FromSeconds(perk.duration).FormatCompound()}\nPrice: {perk.price:C2}");

        EmbedBuilder itemsEmbed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("⛏️Items⛏️️")
            .WithDescription(items.ToString());
        EmbedBuilder perksEmbed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("️️🧪Perks🧪")
            .WithDescription(perks.ToString());

        if (string.IsNullOrWhiteSpace(category))
        {
            await ReplyAsync("Welcome to the shop! Here's what I've got: ", embed: itemsEmbed.Build());
            await ReplyAsync(embed: perksEmbed.Build());
        }
        else if (category.Equals("items", StringComparison.OrdinalIgnoreCase))
        {
            await ReplyAsync("Welcome to the shop! Here's what I've got: ", embed: itemsEmbed.Build());
        }
        else if (category.Equals("perks", StringComparison.OrdinalIgnoreCase))
        {
            await ReplyAsync("Welcome to the shop! Here's what I've got: ", embed: perksEmbed.Build());
        }
        else
        {
            await ReplyAsync($"{Context.User.Mention}, **{category}** is not a valid category!");
        }
    }

    [Alias("kms", "selfend")]
    [Command("suicide")]
    [Summary("Kill yourself.")]
    [Remarks("$suicide")]
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
                RestoreUserData(user, temp.BTC, temp.DOGE, temp.ETH, temp.LTC, temp.XRP, temp.DMNotifs,
                    temp.NoReplyPings, temp.Stats, temp.DealCooldown, temp.LootCooldown, temp.RapeCooldown,
                    temp.RobCooldown, temp.SlaveryCooldown, temp.WhoreCooldown, temp.BullyCooldown,
                    temp.ChopCooldown, temp.DigCooldown, temp.FarmCooldown, temp.FishCooldown,
                    temp.HuntCooldown, temp.MineCooldown, temp.SupportCooldown, temp.HackCooldown,
                    temp.DailyCooldown);
                break;
            case 3:
                await Context.User.NotifyAsync(Context.Channel, "It was quite a struggle, but the noose put you out of your misery. You lost everything.");
                await user.Reference.DeleteAsync();
                await user.SetCash(Context.User, 0);
                RestoreUserData(user, temp.BTC, temp.DOGE, temp.ETH, temp.LTC, temp.XRP, temp.DMNotifs,
                    temp.NoReplyPings, temp.Stats, temp.DealCooldown, temp.LootCooldown, temp.RapeCooldown,
                    temp.RobCooldown, temp.SlaveryCooldown, temp.WhoreCooldown, temp.BullyCooldown,
                    temp.ChopCooldown, temp.DigCooldown, temp.FarmCooldown, temp.FishCooldown,
                    temp.HuntCooldown, temp.MineCooldown, temp.SupportCooldown, temp.HackCooldown,
                    temp.DailyCooldown);
                break;
        }

        return CommandResult.FromSuccess();
    }
}