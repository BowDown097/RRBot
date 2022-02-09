namespace RRBot.Modules;
[Summary("Items, crates, and everything about em.")]
public class Goods : ModuleBase<SocketCommandContext>
{
    public InteractiveService Interactive { get; set; }

    [Alias("purchase")]
    [Command("buy")]
    [Summary("Buy an item from the shop.")]
    [Remarks("$buy [item]")]
    public async Task<RuntimeResult> Buy([Remainder] string itemName)
    {
        Item item = ItemSystem.GetItem(itemName);
        if (item is Perk perk)
            return await ItemSystem.BuyPerk(perk, Context.User, Context.Guild, Context.Channel);
        else if (item is Tool tool)
            return await ItemSystem.BuyTool(tool, Context.User, Context.Guild, Context.Channel);
        else
            return CommandResult.FromError($"**{itemName}** is not an item!");
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
    [Summary("Discard a tool or the Pacifist perk.")]
    [Remarks("$discard [item]")]
    public async Task<RuntimeResult> Discard([Remainder] string itemName)
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        if (user.UsingSlots)
            return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");

        Item item = ItemSystem.GetItem(itemName);
        if (item is Consumable)
        {
            return CommandResult.FromError("Consumables cannot be discarded!");
        }
        else if (item is Perk)
        {
            if (item.Name != "Pacifist")
                return CommandResult.FromError("No perks other than Pacifist can be discarded!");
            if (!user.Perks.Remove("Pacifist"))
                return CommandResult.FromError("You do not have the Pacifist perk!");

            user.PacifistCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(259200);
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
    [Remarks("$item [item]")]
    public async Task<RuntimeResult> ItemInfo([Remainder] string itemName)
    {
        Item item = ItemSystem.GetItem(itemName);
        if (item is Consumable consumable)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle(consumable.Name)
                .WithDescription($"ℹ️ {consumable.Information}\n➕ {consumable.PosEffect}\n➖ {consumable.NegEffect}");
            await ReplyAsync(embed: embed.Build());
        }
        else if (item is Perk perk)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle(perk.Name)
                .WithDescription(perk.Description)
                .AddField("Type", "Perk")
                .AddField("Price", perk.Price.ToString("C2"))
                .AddField("Duration", TimeSpan.FromSeconds(perk.Duration).FormatCompound());
            await ReplyAsync(embed: embed.Build());
        }
        else if (item is Tool tool)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle(tool.Name)
                .AddField("Type", "Tool")
                .AddField("Price", tool.Price.ToString("C2"))
                .AddField("Cash Range", tool.Name.EndsWith("Pickaxe")
                    ? $"{128 * tool.Mult:C2} - {256 * tool.Mult:C2}"
                    : $"{tool.GenericMin:C2} - {tool.GenericMax:C2}");
            await ReplyAsync(embed: embed.Build());
        }
        else
        {
            return CommandResult.FromError($"**{itemName}** is not an item!");
        }

        return CommandResult.FromSuccess();
    }

    [Command("items", RunMode = RunMode.Async)]
    [Summary("View your own or someone else's tools, active perks, and consumables.")]
    [Remarks("$items <user>")]
    public async Task<RuntimeResult> Items(IGuildUser user = null)
    {
        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user?.Id ?? Context.User.Id);

        IEnumerable<string> sortedPerks = dbUser.Perks.Where(k => k.Value > DateTimeOffset.UtcNow.ToUnixTimeSeconds()).Select(p => p.Key);
        List<PageBuilder> pages = new();
        if (dbUser.Tools.Count > 0)
            pages.Add(new PageBuilder().WithColor(Color.Red).WithTitle("Tools").WithDescription(string.Join(", ", dbUser.Tools)));
        if (sortedPerks.Any())
            pages.Add(new PageBuilder().WithColor(Color.Red).WithTitle("Perks").WithDescription(string.Join(", ", sortedPerks)));
        if (dbUser.Consumables.Count > 0)
            pages.Add(new PageBuilder().WithColor(Color.Red).WithTitle("Consumables").WithDescription(string.Join(", ", dbUser.Consumables)));

        if (pages.Count == 0)
            return CommandResult.FromError(user == null ? "You've got nothing!" : $"**{user.Sanitize()}**'s got nothing!");

        StaticPaginator paginator = new StaticPaginatorBuilder()
            .AddUser(Context.User)
            .WithPages(pages)
            .Build();

        await Interactive.SendPaginatorAsync(paginator, Context.Channel, resetTimeoutOnInput: true);
        return CommandResult.FromSuccess();
    }

    [Command("shop", RunMode = RunMode.Async)]
    [Summary("Check out what's available for purchase in the shop.")]
    [Remarks("$shop")]
    public async Task Shop()
    {
        StringBuilder tools = new();
        StringBuilder perks = new();

        foreach (Tool tool in ItemSystem.tools)
            tools.AppendLine($"**{tool}**: {tool.Price:C2}");
        foreach (Perk perk in ItemSystem.perks)
            perks.AppendLine($"**{perk.Name}**: {perk.Description}\nDuration: {TimeSpan.FromSeconds(perk.Duration).FormatCompound()}\nPrice: {perk.Price:C2}");

        PageBuilder[] pages = new[]
        {
            new PageBuilder().WithColor(Color.Red).WithTitle("Tools").WithDescription(tools.ToString()),
            new PageBuilder().WithColor(Color.Red).WithTitle("Perks").WithDescription(perks.ToString())
        };

        StaticPaginator paginator = new StaticPaginatorBuilder()
            .AddUser(Context.User)
            .WithPages(pages)
            .Build();

        await Interactive.SendPaginatorAsync(paginator, Context.Channel, resetTimeoutOnInput: true);
    }
}