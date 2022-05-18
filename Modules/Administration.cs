namespace RRBot.Modules;
[RequireUserPermission(GuildPermission.Administrator)]
[Summary("Commands for admin stuff. Whether you wanna screw with the economy or fuck someone over, I'm sure you'll have fun. However, you'll need to have a very high role to have all this fun. Sorry!")]
public class Administration : ModuleBase<SocketCommandContext>
{
    [Command("drawpot")]
    [Summary("Draw the pot before it ends.")]
    public async Task<RuntimeResult> DrawPot()
    {
        DbPot pot = await DbPot.GetById(Context.Guild.Id);
        if (pot.EndTime < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            return CommandResult.FromError("The pot is currently empty.");

        pot.EndTime = 69;
        await Context.User.NotifyAsync(Context.Channel, "Done! The pot should be drawn soon.");
        return CommandResult.FromSuccess();
    }

    [Command("givecollectible")]
    [Summary("Give a user a collectible.")]
    [Remarks("$givecollectible Cashmere V Card")]
    public async Task<RuntimeResult> GiveCollectible(IGuildUser user, [Remainder] string name)
    {
        if (user.IsBot)
            return CommandResult.FromError("Nope.");

        Item item = ItemSystem.GetItem(name);
        if (item is null)
            return CommandResult.FromError($"**{name}** is not an item!");
        if (item is not Collectible)
            return CommandResult.FromError($"**{item}** is not a collectible!");

        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        await ItemSystem.GiveCollectible(item.Name, Context.Channel, dbUser);
        return CommandResult.FromSuccess();
    }

    [Command("givetool")]
    [Summary("Give a user a tool.")]
    [Remarks("$givetool \"Lenny McLennington\" Diamond Pickaxe")]
    public async Task<RuntimeResult> GiveTool(IGuildUser user, [Remainder] string name)
    {
        if (user.IsBot)
            return CommandResult.FromError("Nope.");

        Item item = ItemSystem.GetItem(name);
        if (item is null)
            return CommandResult.FromError($"**{name}** is not an item!");
        if (item is not Tool)
            return CommandResult.FromError($"**{item}** is not a tool!");

        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        if (dbUser.Tools.Contains(item.Name))
            return CommandResult.FromError($"**{user.Sanitize()}** already has a(n) {item}.");

        dbUser.Tools.Add(item.Name);
        await Context.User.NotifyAsync(Context.Channel, $"Gave **{user.Sanitize()}** a(n) {item}.");
        return CommandResult.FromSuccess();
    }

    [Command("removeachievement")]
    [Summary("Remove a user's achievement.")]
    [Remarks("$removeachievement AceOfSevens I Just Feel Bad")]
    public async Task<RuntimeResult> RemoveAchievement(IGuildUser user, [Remainder] string name)
    {
        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        if (!dbUser.Achievements.Remove(name))
            return CommandResult.FromError($"**{user.Sanitize()}** doesn't have that achievement!");

        await Context.User.NotifyAsync(Context.Channel, $"Successfully removed the achievement from **{user.Sanitize()}**.");
        return CommandResult.FromSuccess();
    }

    [Command("removecrates")]
    [Summary("Remove a user's crates.")]
    [Remarks("$removecrates cashmere")]
    public async Task RemoveCrates(IGuildUser user)
    {
        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        dbUser.Crates = new();
        await Context.User.NotifyAsync(Context.Channel, $"Removed **{user.Sanitize()}**'s crates.");
    }

    [Command("resetcd")]
    [Summary("Reset a user's crime cooldowns.")]
    [Remarks("$resetcd \"\\*Jazzy Hands\\*\"")]
    public async Task ResetCooldowns(IGuildUser user)
    {
        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        foreach (string cmd in Economy.CMDS_WITH_COOLDOWN)
            dbUser[$"{cmd}Cooldown"] = 0;
        await Context.User.NotifyAsync(Context.Channel, $"Reset **{user.Sanitize()}**'s cooldowns.");
    }

    [Command("setcash")]
    [Summary("Set a user's cash.")]
    [Remarks("$setcash BowDown097 0.01")]
    public async Task<RuntimeResult> SetCash(IGuildUser user, double amount)
    {
        if (double.IsNaN(amount) || amount < 0)
            return CommandResult.FromError("You can't set someone's cash to a negative value or NaN!");
        if (user.IsBot)
            return CommandResult.FromError("Nope.");

        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        await dbUser.SetCash(user, amount);
        await ReplyAsync($"Set **{user.Sanitize()}**'s cash to **{amount:C2}**.", allowedMentions: Constants.MENTIONS);
        return CommandResult.FromSuccess();
    }

    [Command("setcrypto")]
    [Summary("Set a user's cryptocurrency amount. See $invest's help info for currently accepted currencies.")]
    [Remarks("$setcrypto Shrimp BTC 69000")]
    public async Task<RuntimeResult> SetCrypto(IGuildUser user, string crypto, double amount)
    {
        string cUp = crypto.ToUpper();
        if (user.IsBot)
            return CommandResult.FromError("Nope.");
        if (!(cUp is "BTC" or "ETH" or "LTC" or "XRP"))
            return CommandResult.FromError($"**{crypto}** is not a currently accepted currency!");

        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        dbUser[cUp] = Math.Round(amount, 4);
        await Context.User.NotifyAsync(Context.Channel, $"Set **{user.Sanitize()}**'s {cUp} to **{amount:0.####}**.");
        return CommandResult.FromSuccess();
    }

    [Command("setprestige")]
    [Summary("Set a user's prestige level.")]
    [Remarks("$setprestige Justin 10")]
    public async Task<RuntimeResult> SetPrestige(IGuildUser user, int level)
    {
        if (level < 0 || level > Constants.MAX_PRESTIGE)
            return CommandResult.FromError("Invalid prestige level!");

        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        dbUser.Prestige = level;
        await Context.User.NotifyAsync(Context.Channel, $"Set **{user.Sanitize()}**'s prestige level to **{level}**.");
        return CommandResult.FromSuccess();
    }

    [Command("setstat")]
    [Summary("Set a stat for a user.")]
    [Remarks("$setstat BowDown097 Mutes 100")]
    public async Task SetStat(IGuildUser user, string stat, string value)
    {
        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        dbUser.Stats[stat] = value;
        await Context.User.NotifyAsync(Context.Channel, $"Set **{user.Sanitize()}**'s **{stat}** to **{value}**.");
    }

    [Command("unlockachievement")]
    [Summary("Unlock an achievement for a user.")]
    [Remarks("$unlockachievement AceOfSevens I Just Feel Bad")]
    public async Task UnlockAchievement(IGuildUser user, [Remainder] string name)
    {
        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        await dbUser.UnlockAchievement(name, user, Context.Channel);
    }
}