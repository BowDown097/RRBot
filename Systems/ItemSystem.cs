namespace RRBot.Systems;
public static class ItemSystem
{
    public static Item? GetItem(string name)
    {
        Item[] allItems = Constants.Crates.Cast<Item>()
            .Concat(Constants.Ammo)
            .Concat(Constants.Collectibles)
            .Concat(Constants.Consumables)
            .Concat(Constants.Perks)
            .Concat(Constants.Tools)
            .Concat(Constants.Weapons)
            .ToArray();
        return Array.Find(allItems, i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public static async Task<RuntimeResult> BuyCrate(Crate crate, IUser user, DbUser dbUser, ISocketMessageChannel channel, bool notify = true)
    {
        if (dbUser.Crates.Count(s => s == crate.Name) == 10)
            return CommandResult.FromError($"You already have the maximum amount of {crate} crates (10).");
        if (crate.Price > dbUser.Cash)
            return CommandResult.FromError($"You do not have enough to buy a {crate} crate!");

        dbUser.Crates.Add(crate.Name);
        await dbUser.SetCash(user, dbUser.Cash - crate.Price);
        if (notify)
            await user.NotifyAsync(channel, $"You got yourself a {crate} crate for **{crate.Price:C2}**!");
        return CommandResult.FromSuccess();
    }

    public static async Task<RuntimeResult> BuyPerk(Perk perk, SocketUser user, DbUser dbUser, ISocketMessageChannel channel)
    {
        if (dbUser.Perks.ContainsKey("Pacifist"))
            return CommandResult.FromError("You have the Pacifist perk and cannot buy another.");
        if (!dbUser.Perks.ContainsKey("Multiperk") && dbUser.Perks.Count == 1 && perk.Name is not ("Pacifist" or "Multiperk"))
            return CommandResult.FromError("You already have a perk.");
        if (dbUser.Perks.ContainsKey("Multiperk") && dbUser.Perks.Count == 3 && perk.Name is not ("Pacicist" or "Multiperk"))
            return CommandResult.FromError("You already have 2 perks.");

        if (dbUser.Perks.ContainsKey(perk.Name))
            return CommandResult.FromError($"You already have {perk}!");

        if (perk.Name == "Pacifist")
        {
            if (dbUser.PacifistCooldown != 0)
            {
                long cooldownSecs = dbUser.PacifistCooldown - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (cooldownSecs > 0)
                    return CommandResult.FromError("You bought the Pacifist perk later than 3 days ago. You still have to wait {TimeSpan.FromSeconds(cooldownSecs).FormatCompound()}.");
                dbUser.PacifistCooldown = 0;
            }

            foreach (string key in dbUser.Perks.Keys)
            {
                if (GetItem(key) is Perk keyPerk) 
                    dbUser.Cash += keyPerk.Price;
                dbUser.Perks.Remove(key);
            }
        }

        if (perk.Price > dbUser.Cash) 
            return CommandResult.FromError($"You do not have enough to buy {perk}!");

        dbUser.Perks.Add(perk.Name, DateTimeOffset.UtcNow.ToUnixTimeSeconds(perk.Duration));
        await dbUser.SetCash(user, dbUser.Cash - perk.Price);

        StringBuilder notification = new($"You got yourself the {perk} perk for **{perk.Price:C2}**!");
        if (perk.Name == "Pacifist")
            notification.Append(" Additionally, as you bought the Pacifist perk, any perks you previously had have been refunded.");

        await user.NotifyAsync(channel, notification.ToString());
        return CommandResult.FromSuccess();
    }

    public static async Task<RuntimeResult> BuyTool(Tool tool, SocketUser user, DbUser dbUser, ISocketMessageChannel channel)
    {
        if (dbUser.Tools.Contains(tool.Name))
            return CommandResult.FromError($"You already have a {tool}!");
        if (tool.Price > dbUser.Cash)
            return CommandResult.FromError($"You do not have enough to buy a {tool}!");

        dbUser.Tools.Add(tool.Name);
        await dbUser.SetCash(user, dbUser.Cash - tool.Price);
        await user.NotifyAsync(channel, $"You got yourself a fresh {tool} for **{tool.Price:C2}**!");
        return CommandResult.FromSuccess();
    }

    public static async Task GiveCollectible(string name, IMessageChannel channel, DbUser user)
    {
        if (GetItem(name) is not Collectible collectible)
            return;

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithThumbnailUrl(collectible.Image)
            .WithTitle("Collectible found!")
            .WithDescription($"**{collectible}:** {collectible.Description}\n\nWorth {(collectible.Price != -1 ? collectible.Price.ToString("C2") : "some amount of money")} - $discard this item to cash in!");

        if (!user.Collectibles.TryAdd(collectible.Name, 1))
            user.Collectibles[collectible.Name]++;

        try
        {
            await channel.SendMessageAsync(embed: embed.Build());
        }
        catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.InsufficientPermissions) {}
    }

    public static string GetBestTool(IEnumerable<string> tools, string type)
    {
        IEnumerable<string> toolsOfType = tools.Where(tool => tool.EndsWith(type));
        return toolsOfType.OrderByDescending(tool => GetItem(tool)!.Price).First();
    }
}