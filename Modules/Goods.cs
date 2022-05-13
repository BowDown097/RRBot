namespace RRBot.Modules;
[Summary("Items, crates, and everything about em.")]
public class Goods : ModuleBase<SocketCommandContext>
{
    public InteractiveService Interactive { get; set; }

    [Alias("purchase")]
    [Command("buy")]
    [Summary("Buy an item from the shop.")]
    [Remarks("$buy Fishing Rod")]
    public async Task<RuntimeResult> Buy([Remainder] string itemName)
    {
        Item item = ItemSystem.GetItem(itemName.ToLower().Replace(" crate", ""));
        if (item?.Name == "Daily")
            return CommandResult.FromError("You cannot buy the Daily crate!");

        if (item is Crate crate)
            return await ItemSystem.BuyCrate(crate, Context.User, Context.Guild, Context.Channel);
        if (item is Perk perk)
            return await ItemSystem.BuyPerk(perk, Context.User, Context.Guild, Context.Channel);
        else if (item is Tool tool)
            return await ItemSystem.BuyTool(tool, Context.User, Context.Guild, Context.Channel);
        else
            return CommandResult.FromError($"**{itemName}** is not an item!");
    }

    [Command("daily")]
    [Summary("Get a daily reward.")]
    [RequireCooldown("DailyCooldown", "Slow down there, turbo! It hasn't been a day yet. You've still got {0} left.")]
    [RequireRankLevel("3")]
    public async Task<RuntimeResult> Daily()
    {
        RuntimeResult result = await ItemSystem.BuyCrate(ItemSystem.GetItem("Daily") as Crate, Context.User, Context.Guild, Context.Channel, false);
        if (result.IsSuccess)
        {
            await Context.User.NotifyAsync(Context.Channel, "Here's a Daily crate, my good man! Best of luck.");
            DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
            await user.SetCooldown("DailyCooldown", Constants.DAILY_COOLDOWN, Context.Guild, Context.User);
        }
        return result;
    }

    [Alias("sell")]
    [Command("discard")]
    [Summary("Discard a tool or the Pacifist perk.")]
    [Remarks("$discard Pacifist")]
    public async Task<RuntimeResult> Discard([Remainder] string itemName)
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        if (user.UsingSlots)
            return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");

        Item item = ItemSystem.GetItem(itemName);
        if (item is Crate or Consumable)
        {
            return CommandResult.FromError("Crates and consumables cannot be discarded!");
        }
        else if (item is Perk)
        {
            if (item.Name != "Pacifist")
                return CommandResult.FromError("No perks other than Pacifist can be discarded!");
            if (!user.Perks.Remove("Pacifist"))
                return CommandResult.FromError("You do not have the Pacifist perk!");

            await user.SetCooldown("PacifistCooldown", 259200, Context.Guild, Context.User);
            await Context.User.NotifyAsync(Context.Channel, "You discarded your Pacifist perk. If you wish to buy it again, you will have to wait 3 days.");
        }
        else if (item is Tool)
        {
            if (!user.Tools.Remove(item.Name))
                return CommandResult.FromError($"You do not have a(n) {item}!");

            await user.SetCash(Context.User, user.Cash + (item.Price * 0.9));
            await Context.User.NotifyAsync(Context.Channel, $"You sold your {item} to some dude for **{item.Price * 0.9:C2}**.");
        }
        else
        {
            return CommandResult.FromError($"**{itemName}** is not an item!");
        }

        return CommandResult.FromSuccess();
    }

    [Command("item")]
    [Summary("View information on an item.")]
    [Remarks("$item Cocaine")]
    public async Task<RuntimeResult> ItemInfo([Remainder] string itemName)
    {
        Item item = ItemSystem.GetItem(itemName.ToLower().Replace(" crate", ""));
        if (item == null)
            return CommandResult.FromError($"**{itemName}** is not an item!");

        EmbedBuilder embed = item switch
        {
            Consumable consumable => new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle(consumable.Name)
                .WithDescription($"ℹ️ {consumable.Information}\n➕ {consumable.PosEffect}\n➖ {consumable.NegEffect}"),
            Crate crate => new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle($"{crate.Name} Crate")
                .AddField("Price", crate.Price.ToString("C2"))
                .AddField("Cash", crate.Cash.ToString("C2"), condition: crate.Cash != 0)
                .AddField("Consumables", crate.ConsumableCount, condition: crate.ConsumableCount != 0)
                .AddField("Tools", crate.ToolCount, condition: crate.ToolCount != 0),
            Perk perk => new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle(perk.Name)
                .WithDescription(perk.Description)
                .AddField("Type", "Perk")
                .AddField("Price", perk.Price.ToString("C2"))
                .AddField("Duration", TimeSpan.FromSeconds(perk.Duration).FormatCompound()),
            Tool tool => new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle(tool.Name)
                .AddField("Type", "Tool")
                .AddField("Price", tool.Price.ToString("C2"))
                .AddField("Cash Range", tool.Name.EndsWith("Pickaxe")
                    ? $"{128 * tool.Mult:C2} - {256 * tool.Mult:C2}"
                    : $"{tool.GenericMin:C2} - {tool.GenericMax:C2}"),
            _ => new EmbedBuilder()
        };

        await ReplyAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }

    [Command("items", RunMode = RunMode.Async)]
    [Summary("View your own or someone else's tools, active perks, and consumables.")]
    [Remarks("$items Zurmii#2208")]
    public async Task<RuntimeResult> Items(IGuildUser user = null)
    {
        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user?.Id ?? Context.User.Id);

        IEnumerable<string> sortedPerks = dbUser.Perks.Where(k => k.Value > DateTimeOffset.UtcNow.ToUnixTimeSeconds()).Select(p => p.Key);
        List<PageBuilder> pages = new();
        if (dbUser.Tools.Count > 0)
            pages.Add(new PageBuilder().WithColor(Color.Red).WithTitle("Tools").WithDescription(string.Join('\n', dbUser.Tools)));
        if (sortedPerks.Any())
            pages.Add(new PageBuilder().WithColor(Color.Red).WithTitle("Perks").WithDescription(string.Join('\n', sortedPerks)));

        StringBuilder consumablesBuilder = new();
        StringBuilder cratesBuilder = new();
        foreach (KeyValuePair<string, int> consumable in dbUser.Consumables)
            consumablesBuilder.AppendLine($"{consumable.Key} ({consumable.Value}x)");
        foreach (string crate in dbUser.Crates.Distinct())
            cratesBuilder.AppendLine($"{crate} ({dbUser.Crates.Count(c => c == crate)}x)");
        if (dbUser.Consumables.Count > 0)
            pages.Add(new PageBuilder().WithColor(Color.Red).WithTitle("Consumables").WithDescription(consumablesBuilder.ToString()));
        if (dbUser.Crates.Count > 0)
            pages.Add(new PageBuilder().WithColor(Color.Red).WithTitle("Crates").WithDescription(cratesBuilder.ToString()));

        if (pages.Count == 0)
            return CommandResult.FromError(user == null ? "You've got nothing!" : $"**{user.Sanitize()}**'s got nothing!");

        StaticPaginator paginator = new StaticPaginatorBuilder()
            .AddUser(Context.User)
            .WithPages(pages)
            .Build();

        await Interactive.SendPaginatorAsync(paginator, Context.Channel, resetTimeoutOnInput: true);
        return CommandResult.FromSuccess();
    }

    [Command("opencrate")]
    [Summary("Open a crate.")]
    [Remarks("$opencrate diamond")]
    public async Task<RuntimeResult> OpenCrate(string crateName)
    {
        try
        {
            if (ItemSystem.GetItem(crateName) is not Crate crate)
                return CommandResult.FromError($"**{crateName}** is not a crate!");

            DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
            if (!user.Crates.Remove(crate.Name))
                return CommandResult.FromError($"You don't have a {crate.Name} crate!");
            user.Cash += crate.Cash;

            List<Item> items = crate.Open(user);
            IEnumerable<string> consumables = items.Where(i => i is Consumable).Select(c => c.Name);
            IEnumerable<string> tools = items.Where(i => i is Tool).Select(t => t.Name);

            StringBuilder description = new();
            if (crate.Cash > 0)
                description.AppendLine($"**Cash** ({crate.Cash:C2})");
            foreach (string consumable in consumables.Distinct())
            {
                int count = consumables.Count(c => c == consumable);
                if (user.Consumables.ContainsKey(consumable))
                    user.Consumables[consumable] += count;
                else
                    user.Consumables.Add(consumable, count);

                description.AppendLine($"**{consumable}** ({count}x)");
            }
            foreach (string tool in tools)
            {
                user.Tools.Add(tool);
                description.AppendLine($"**{tool}**");
            }

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle($"{crate.Name} Crate")
                .WithDescription($"You got:\n{description}");
            await ReplyAsync(embed: embed.Build());
        }
        catch (Exception e)
        {
            Console.WriteLine(e.StackTrace);
        }
        return CommandResult.FromSuccess();
    }

    [Command("shop", RunMode = RunMode.Async)]
    [Summary("Check out what's available for purchase in the shop.")]
    public async Task Shop()
    {
        StringBuilder tools = new();
        StringBuilder perks = new();
        StringBuilder crates = new();

        foreach (Tool tool in ItemSystem.tools)
            tools.AppendLine($"**{tool}**: {tool.Price:C2}");
        foreach (Perk perk in ItemSystem.perks)
            perks.AppendLine($"**{perk.Name}**: {perk.Description}\nDuration: {TimeSpan.FromSeconds(perk.Duration).FormatCompound()}\nPrice: {perk.Price:C2}");
        foreach (Crate crate in ItemSystem.crates.Where(c => c.Name != "Daily"))
            crates.AppendLine($"**{crate}**: {crate.Price:C2}");

        PageBuilder[] pages = new[]
        {
            new PageBuilder().WithColor(Color.Red).WithTitle("Tools").WithDescription(tools.ToString()),
            new PageBuilder().WithColor(Color.Red).WithTitle("Perks").WithDescription(perks.ToString()),
            new PageBuilder().WithColor(Color.Red).WithTitle("Crates").WithDescription(crates.ToString())
        };

        StaticPaginator paginator = new StaticPaginatorBuilder()
            .AddUser(Context.User)
            .WithPages(pages)
            .Build();

        await Interactive.SendPaginatorAsync(paginator, Context.Channel, resetTimeoutOnInput: true);
    }

    [Command("use")]
    [Summary("Use a consumable.")]
    public async Task<RuntimeResult> Use([Remainder] string name)
    {
        Item item = ItemSystem.GetItem(name);
        if (item is null)
            return CommandResult.FromError($"**{name}** is not an item!");
        if (item is not Consumable con)
            return CommandResult.FromError($"**{name}** is not a consumable!");

        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        if (!user.Consumables.TryGetValue(con.Name, out int amount) || amount == 0)
            return CommandResult.FromError($"You don't have any {con.Name}!");

        switch (con.Name)
        {
            case "Cocaine":
                user.CocaineInSystem++;
                user.Consumables["Cocaine"]--;

                if (RandomUtil.Next(6 - user.CocaineInSystem) == 1)
                {
                    int recoveryHours = 1 * (1 + user.CocaineInSystem);
                    user.CocaineInSystem = 0;
                    user.CocaineTime = 0;
                    user.Consumables["Cocaine"] = 0;
                    user.RecoveryTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(3600 * recoveryHours);
                    await Context.User.NotifyAsync(Context.Channel, $"​OH SHIT, HOMIE! You overdosed! This is why you don't do drugs! You lost all your remaining cocaine and have to go into recovery for {recoveryHours} hours, meaning no economy commands for you!");
                    break;
                }

                await Context.User.NotifyAsync(Context.Channel, "​PHEW WEE! That nose candy is already making you feel hyped as FUCK! Your cooldowns have been reduced by a solid 10%.");
                foreach (string cmd in Economy.CMDS_WITH_COOLDOWN)
                {
                    long cooldownSecs = (long)user[$"{cmd}Cooldown"] - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    if (cooldownSecs > 0)
                        user[$"{cmd}Cooldown"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds((long)(cooldownSecs * 0.90));
                }

                user.CocaineTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.COCAINE_DURATION);
                break;
            case "Romanian Flag":
                await ReplyAsync("Unimplemented");
                break;
        }

        return CommandResult.FromSuccess();
    }
}