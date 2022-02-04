namespace RRBot.Modules;
[RequireUserPermission(GuildPermission.Administrator)]
[Summary("Commands for admin stuff. Whether you wanna screw with the economy or fuck someone over, I'm sure you'll have fun. However, you'll need to have a very high role to have all this fun. Sorry!")]
public class Administration : ModuleBase<SocketCommandContext>
{
    [Command("givetool")]
    [Summary("Give a user a tool.")]
    [Remarks("$givetool [user] [tool]")]
    public async Task<RuntimeResult> GiveTool(IGuildUser user, [Remainder] string tool)
    {
        if (user.IsBot)
            return CommandResult.FromError("Nope.");
        if (!ItemSystem.tools.Any(t => t.Name == tool))
            return CommandResult.FromError($"**{tool}** is not a valid tool!");

        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        if (dbUser.Tools.Contains(tool))
            return CommandResult.FromError($"**{user.Sanitize()}** already has a(n) {tool}.");

        dbUser.Tools.Add(tool);
        await Context.User.NotifyAsync(Context.Channel, $"Gave **{user.Sanitize()}** a(n) {tool}.");
        return CommandResult.FromSuccess();
    }

    [Command("resetcd")]
    [Summary("Reset a user's crime cooldowns.")]
    [Remarks("$resetcd [user]")]
    public async Task ResetCooldowns(IGuildUser user)
    {
        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        foreach (string cmd in Economy.CMDS_WITH_COOLDOWN)
            dbUser[$"{cmd}Cooldown"] = 0L;
        await Context.User.NotifyAsync(Context.Channel, $"Reset **{user.Sanitize()}**'s cooldowns.");
    }

    [Command("setcash")]
    [Summary("Set a user's cash.")]
    [Remarks("$setcash [user] [amount]")]
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
    [Remarks("$setcrypto [user] [crypto] [amount]")]
    public async Task<RuntimeResult> SetCrypto(IGuildUser user, string crypto, double amount)
    {
        string cUp = crypto.ToUpper();
        if (user.IsBot)
            return CommandResult.FromError("Nope.");
        if (!(cUp is "BTC" or "DOGE" or "ETH" or "LTC" or "XRP"))
            return CommandResult.FromError($"**{crypto}** is not a currently accepted currency!");

        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        dbUser[cUp] = Math.Round(amount, 4);
        await Context.User.NotifyAsync(Context.Channel, $"Added **{amount}** to **{user.Sanitize()}**'s {cUp} balance.");
        return CommandResult.FromSuccess();
    }

    [Command("unlockachievement")]
    [Summary("Unlock an achievement for a user.")]
    [Remarks("$unlockachievement [user] [name] [desc] <reward>")]
    public async Task UnlockAchievement(IGuildUser user, string name, string desc, double reward = 0)
    {
        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        await dbUser.UnlockAchievement(name, desc, user, Context.Channel, reward);
    }
}