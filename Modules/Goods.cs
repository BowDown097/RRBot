namespace RRBot.Modules;
[Summary("Items, crates, and everything about 'em.")]
public partial class Goods : ModuleBase<SocketCommandContext>
{
    public InteractiveService Interactive { get; set; } = null!;

    [Alias("purchase")]
    [Command("buy")]
    [Summary("Buy an item from the shop.")]
    [Remarks("$buy Fishing Rod")]
    public async Task<RuntimeResult> Buy([Remainder] string itemName)
    {
        Item? item = ItemSystem.GetItem(itemName.ToLower().Replace(" crate", ""));
        if (item is null)
            return CommandResult.FromError("That is not an item!");
        if (item.Name == "Daily")
            return CommandResult.FromError("You cannot buy the Daily crate!");
        if (item.Name.StartsWith("Netherite"))
            return CommandResult.FromError("​Netherite items can only be obtained from Diamond crates!");

        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        RuntimeResult result = item switch
        {
            Ammo => CommandResult.FromError("Ammo can only be obtained from crates!"),
            Crate crate => await ItemSystem.BuyCrate(crate, Context.User, user, Context.Channel),
            Perk perk => await ItemSystem.BuyPerk(perk, Context.User, user, Context.Channel),
            Tool tool => await ItemSystem.BuyTool(tool, Context.User, user, Context.Channel),
            Weapon => CommandResult.FromError("Weapons can only be obtained from crates!"),
            _ => CommandResult.FromError("That is not an item!"),
        };

        await MongoManager.UpdateObjectAsync(user);
        return result;
    }

    [Command("daily")]
    [Summary("Get a daily reward.")]
    [RequireCooldown("DailyCooldown", "Slow down there, turbo! It hasn't been a day yet. You've still got {0} left.")]
    [RequireRankLevel(3)]
    public async Task<RuntimeResult> Daily()
    {
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        RuntimeResult result = await ItemSystem.BuyCrate((Crate)ItemSystem.GetItem("Daily")!, Context.User,
            user, Context.Channel, false);
        if (!result.IsSuccess) 
            return result;

        await Context.User.NotifyAsync(Context.Channel, "Here's a Daily crate, my good man! Best of luck.");
        await user.SetCooldown("DailyCooldown", Constants.DailyCooldown, Context.User);
        await MongoManager.UpdateObjectAsync(user);

        return result;
    }

    [Alias("sell")]
    [Command("discard")]
    [Summary("Toss an item you don't want anymore for some cash.")]
    [Remarks("$discard Pacifist")]
    public async Task<RuntimeResult> Discard([Remainder] string itemName)
    {
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        Item? item = ItemSystem.GetItem(itemName);
        switch (item)
        {
            case Ammo or Consumable or Crate:
                return CommandResult.FromError("Ammo, consumables and crates cannot be discarded!");
            case Collectible collectible:
                if (!user.Collectibles.TryGetValue(item.Name, out int count) || count == 0)
                    return CommandResult.FromError($"You do not have a(n) {item}!");
                if (!collectible.Discardable)
                    return CommandResult.FromError($"You cannot discard your {item}.");

                decimal price = item.Price != -1 ? item.Price : RandomUtil.NextDecimal(100, 1500);
                await user.SetCash(Context.User, user.Cash + price);
                user.Collectibles[item.Name]--;
                await Context.User.NotifyAsync(Context.Channel, $"You gave your {item} to some dude for **{price:C2}**.");
                break;
            case Perk:
                if (item.Name != "Pacifist")
                    return CommandResult.FromError("No perks other than Pacifist can be discarded!");
                if (!user.Perks.Remove("Pacifist"))
                    return CommandResult.FromError("You do not have the Pacifist perk!");

                await user.SetCooldown("PacifistCooldown", 259200, Context.User);
                await Context.User.NotifyAsync(Context.Channel, "You discarded your Pacifist perk. If you wish to buy it again, you will have to wait 3 days.");
                break;
            case Tool:
                if (!user.Tools.Remove(item.Name))
                    return CommandResult.FromError($"You do not have a(n) {item}!");

                await user.SetCash(Context.User, user.Cash + item.Price * 0.9m);
                await Context.User.NotifyAsync(Context.Channel, $"You sold your {item} to some dude for **{item.Price * 0.9m:C2}**.");
                break;
            case Weapon:
                if (!user.Weapons.Remove(item.Name))
                    return CommandResult.FromError($"You do not have a(n) {item}!");

                await user.SetCash(Context.User, user.Cash + 5000);
                await Context.User.NotifyAsync(Context.Channel, $"You sold your {item} to some dude for **$5,000.00**.");
                break;
            default:
                return CommandResult.FromError("That is not an item!");
        }

        await MongoManager.UpdateObjectAsync(user);
        return CommandResult.FromSuccess();
    }

    [Command("item")]
    [Summary("View information on an item.")]
    [Remarks("$item Cocaine")]
    public async Task<RuntimeResult> ItemInfo([Remainder] string itemName)
    {
        Item? item = ItemSystem.GetItem(itemName.ToLower().Replace(" crate", ""));
        if (item is null)
            return CommandResult.FromError("That is not an item!");

        EmbedBuilder embed = item switch
        {
            Ammo ammo => new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle(ammo.Name)
                .AddField("Accepted By", string.Join(", ", Constants.Weapons.Where(w => w.Ammo == ammo.Name).OrderBy(w => w.Name))),
            Collectible collectible => new EmbedBuilder()
                .WithColor(Color.Red)
                .WithThumbnailUrl(collectible.Image)
                .WithTitle(collectible.Name)
                .AddField("Description", collectible.Description, true)
                .AddField("Worth", collectible.Price != -1 ? collectible.Price.ToString("C2") : "Some amount of money", true),
            Consumable consumable => new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle(consumable.Name)
                .WithDescription($"ℹ️ {consumable.Information}\n⏱️ {TimeSpan.FromSeconds(consumable.Duration).FormatCompound()}\n➕ {consumable.PosEffect}\n➖ {consumable.NegEffect}\n{(consumable.Max > 0 ? $"⚠️ {consumable.Max} max" : "")}"),
            Crate crate => new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle($"{crate} Crate")
                .AddField("Price", crate.Price.ToString("C2"), true)
                .AddField("Cash", crate.Cash.ToString("C2"), crate.Cash != 0, true)
                .AddField("Consumables", crate.ConsumableCount, crate.ConsumableCount != 0, true)
                .AddField("Tools", crate.ToolCount, crate.ToolCount != 0, true),
            Perk perk => new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle(perk.Name)
                .WithDescription(perk.Description)
                .AddField("Type", "Perk", true)
                .AddField("Price", perk.Price.ToString("C2"), true)
                .AddField("Duration", TimeSpan.FromSeconds(perk.Duration).FormatCompound(), true),
            Tool tool => new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle(tool.Name)
                .AddField("Type", "Tool", true)
                .AddField("Price", tool.Price.ToString("C2"), true)
                .AddField("Cash Range", tool.Name.EndsWith("Pickaxe")
                    ? $"{128 * tool.Mult:C2} - {256 * tool.Mult:C2}"
                    : $"{tool.GenericMin:C2} - {tool.GenericMax:C2}",
                    true)
                .AddField("Additional Info", "Only obtainable from Diamond crates", tool.Name.StartsWith("Netherite"), true),
            Weapon weapon => new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle(weapon.Name)
                .WithDescription(weapon.Information)
                .AddField("Type", weapon.Type, true)
                .AddField("Accuracy", weapon.Accuracy + "%", true)
                .AddField("Ammo", weapon.Ammo, true)
                .AddField("Damage Range", $"{weapon.DamageMin} - {weapon.DamageMax}", true)
                .AddField("Drop Chance", weapon.DropChance + "%", true)
                .AddField("Available In Crates", string.Join(", ", weapon.InsideCrates), true),
            _ => new EmbedBuilder()
        };

        await ReplyAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }

    [Alias("inv", "inventory")]
    [Command("items", RunMode = RunMode.Async)]
    [Summary("View your own or someone else's items.")]
    [Remarks("$items Zurmii#2208")]
    public async Task<RuntimeResult> Items([Remainder] IGuildUser? user = null)
    {
        DbUser dbUser = await MongoManager.FetchUserAsync(user?.Id ?? Context.User.Id, Context.Guild.Id);
        string[] sortedPerks = [..dbUser.Perks
            .Where(k => k.Value > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            .Select(p => p.Key)];

        string collectibles = string.Join('\n', dbUser.Collectibles.Where(k => k.Value > 0).Select(c => $"{c.Key} ({c.Value}x)"));
        string consumables = string.Join('\n', dbUser.Consumables.Where(k => k.Value > 0).Select(c => $"{c.Key} ({c.Value}x)"));
        string crates = string.Join('\n', dbUser.Crates.Distinct().Select(c => $"{c} ({dbUser.Crates.Count(cr => cr == c)}x)"));
        string perks = string.Join('\n', sortedPerks);
        string tools = string.Join('\n', dbUser.Tools);
        string weapons = string.Join('\n', dbUser.Weapons);

        if (dbUser.Ammo.Any(k => k.Value > 0))
            weapons += '\n' + string.Join('\n', dbUser.Ammo.Where(k => k.Value > 0).Select(a => $"{a.Key} ({a.Value}x)"));

        List<PageBuilder> pages = [];
        if (dbUser.Tools.Count > 0)
            pages.Add(new PageBuilder().WithColor(Color.Red).WithTitle("Tools").WithDescription(tools));
        if (dbUser.Weapons.Count > 0)
            pages.Add(new PageBuilder().WithColor(Color.Red).WithTitle("Weapons").WithDescription(weapons));
        if (sortedPerks.Length > 0)
            pages.Add(new PageBuilder().WithColor(Color.Red).WithTitle("Perks").WithDescription(perks));
        if (!string.IsNullOrWhiteSpace(collectibles))
            pages.Add(new PageBuilder().WithColor(Color.Red).WithTitle("Collectibles").WithDescription(collectibles));
        if (!string.IsNullOrWhiteSpace(consumables))
            pages.Add(new PageBuilder().WithColor(Color.Red).WithTitle("Consumables").WithDescription(consumables));
        if (!string.IsNullOrWhiteSpace(crates))
            pages.Add(new PageBuilder().WithColor(Color.Red).WithTitle("Crates").WithDescription(crates));

        if (pages.Count == 0)
            return CommandResult.FromError(user is null ? "You've got nothing!" : $"**{user.Sanitize()}**'s got nothing!");

        StaticPaginator paginator = new StaticPaginatorBuilder()
            .AddUser(Context.User)
            .WithPages(pages)
            .Build();

        await Interactive.SendPaginatorAsync(paginator, Context.Channel, resetTimeoutOnInput: true);
        return CommandResult.FromSuccess();
    }

    [Alias("oc")]
    [Command("open")]
    [Summary("Open a crate.")]
    [Remarks("$open diamond")]
    public async Task<RuntimeResult> Open([Remainder] string crateName)
    {
        crateName = crateName.Replace(" crate", "");
        if (ItemSystem.GetItem(crateName) is not Crate crate)
            return CommandResult.FromError("That is not a crate!");
        
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        if (!user.Crates.Remove(crate.Name))
            return CommandResult.FromError($"You don't have a {crate} crate!");
        user.Cash += crate.Cash;

        List<Item> items = crate.Open(user);
        string[] ammos = [..items.Where(i => i is Ammo).Select(a => a.Name)];
        string[] consumables = [..items.Where(i => i is Consumable).Select(c => c.Name)];
        IEnumerable<string> tools = items.Where(i => i is Tool).Select(t => t.Name);
        IEnumerable<string> weapons = items.Where(i => i is Weapon).Select(t => t.Name);

        StringBuilder description = new();
        if (crate.Cash > 0)
            description.AppendLine($"**Cash** ({crate.Cash:C2})");

        foreach (string ammo in ammos.Distinct())
        {
            int count = ammos.Count(a => a == ammo);
            if (!user.Ammo.TryAdd(ammo, count))
                user.Ammo[ammo] += count;
            description.AppendLine($"**{ammo}** ({count}x)");
        }
        
        foreach (string consumable in consumables.Distinct())
        {
            int count = consumables.Count(c => c == consumable);
            if (!user.Consumables.TryAdd(consumable, count))
                user.Consumables[consumable] += count;

            description.AppendLine($"**{consumable}** ({count}x)");
        }

        foreach (string tool in tools)
        {
            user.Tools.Add(tool);
            description.AppendLine($"**{tool}**");
        }

        foreach (string weapon in weapons)
        {
            user.Weapons.Add(weapon);
            description.AppendLine($"**{weapon}**");
        }

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle($"{crate} Crate")
            .WithDescription($"You got:\n{description}");
        await ReplyAsync(embed: embed.Build());

        await MongoManager.UpdateObjectAsync(user);
        return CommandResult.FromSuccess();
    }

    [Command("shop", RunMode = RunMode.Async)]
    [Summary("Check out what's available for purchase in the shop.")]
    public async Task Shop()
    {
        string crates = string.Join('\n', Constants.Crates.Where(c => c.Name != "Daily").Select(c => $"**{c}**: {c.Price:C2}"));
        string perks = string.Join('\n', Constants.Perks.Select(p => $"**{p}**: {p.Description}\nDuration: {TimeSpan.FromSeconds(p.Duration).FormatCompound()}\nPrice: {p.Price:C2}"));
        string tools = string.Join('\n', Constants.Tools.Where(t => !t.Name.StartsWith("Netherite")).Select(t => $"**{t}**: {t.Price:C2}"));
        string weapons = string.Join('\n', Constants.Weapons.Select(w => $"**{w}**: {w.Information}"));

        IPageBuilder[] pages =
        [
            new PageBuilder().WithColor(Color.Red).WithTitle("Tools").WithDescription(tools).WithFooter("Buy tools with $buy!"),
            new PageBuilder().WithColor(Color.Red).WithTitle("Weapons").WithDescription(weapons).WithFooter("Get weapons from crates!"),
            new PageBuilder().WithColor(Color.Red).WithTitle("Perks").WithDescription(perks).WithFooter("Buy perks with $buy!"),
            new PageBuilder().WithColor(Color.Red).WithTitle("Crates").WithDescription(crates).WithFooter("Buy crates with $buy!")
        ];

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
        Item? item = ItemSystem.GetItem(name);
        if (item is null)
            return CommandResult.FromError("That is not an item!");
        if (item is not Consumable con)
            return CommandResult.FromError($"**{item}** is not a consumable!");
        
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        if (!user.Consumables.TryGetValue(con.Name, out int amount) || amount == 0)
            return CommandResult.FromError($"You don't have any {con}(s)!");
        if (user.UsedConsumables.TryGetValue(con.Name, out int used) && used == con.Max)
            return CommandResult.FromError($"You cannot use more than {con.Max} {con}!");

        user.UsedConsumables.TryAdd(con.Name, 0);
        user.Consumables[con.Name]--;
        user.UsedConsumables[con.Name]++;

        switch (con.Name)
        {
            case "Black Hat":
                await GenericUse(con, user, Context,
                    "Oh yeah. Hacker mode activated. 10% greater $hack chance.",
                    "Dammit! The feds caught onto you! You were fined **{0:C2}**.",
                    "BlackHatTime", Constants.BlackHatDuration, 1.5m, 3);
                break;
            case "Cocaine":
                if (RandomUtil.Next(6 - user.UsedConsumables.GetValueOrDefault("Cocaine")) == 1)
                {
                    int recoveryHours = 1 * (1 + user.UsedConsumables.GetValueOrDefault("Cocaine"));
                    user.CocaineTime = 0;
                    user.Consumables["Cocaine"] = 0;
                    user.CocaineRecoveryTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(3600 * recoveryHours);
                    user.UsedConsumables["Cocaine"] = 0;
                    await Context.User.NotifyAsync(Context.Channel, $"​OH SHIT, HOMIE! You overdosed! This is why you don't do drugs! You lost all your remaining cocaine and have to go into recovery for {recoveryHours} hours, meaning no economy commands for you!");
                    break;
                }

                await Context.User.NotifyAsync(Context.Channel, "​PHEW WEE! That nose candy is already making you feel hyped as FUCK! Your cooldowns have been reduced by a solid 10%.");
                foreach (string cmd in Economy.CmdsWithCooldown)
                {
                    long cooldownSecs = (long)user[$"{cmd}Cooldown"] - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    if (cooldownSecs > 0)
                        user[$"{cmd}Cooldown"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds((long)(cooldownSecs * 0.90));
                }

                user.CocaineTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.CocaineDuration);
                break;
            case "Romanian Flag":
                await GenericUse(con, user, Context,
                    "Hell yeah! Wear that flag with pride! You've now got a 10% higher chance to rob people.",
                    "Those damn gyppos caught onto you! **{0:C2}** was yoinked from you and you lost all of your flags.",
                    "RomanianFlagTime", Constants.RomanianFlagDuration);
                break;
            case "Viagra":
                await GenericUse(con, user, Context,
                    "Zoo wee mama! Your blood is rushing so much you can feel it. You're now 10% more likely for a rape to land.",
                    "Dammit bro! The pill backfired and now you've got ED! You had to pay **{0:C2}** to get that shit fixed.",
                    "ViagraTime", Constants.ViagraDuration);
                break;
        }

        await MongoManager.UpdateObjectAsync(user);
        return CommandResult.FromSuccess();
    }
}