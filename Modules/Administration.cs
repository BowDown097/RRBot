namespace RRBot.Modules;
[RequireUserPermission(GuildPermission.Administrator)]
[Summary("Commands for admin stuff. Whether you wanna screw with the economy or fuck someone over, I'm sure you'll have fun. However, you'll need to have a very high role to have all this fun. Sorry!")]
public class Administration : ModuleBase<SocketCommandContext>
{
    public InteractiveService Interactive { get; set; }
    
    [Command("cleartextchannel")]
    [Summary("Deletes and recreates a text channel, effectively wiping its messages.")]
    [Remarks("$cleartextchannel \\#furry-rp")]
    public async Task ClearTextChannel(ITextChannel channel)
    {
        await channel.DeleteAsync();
        await Context.Guild.CreateTextChannelAsync(channel.Name, properties => {
            properties.CategoryId = channel.CategoryId;
            properties.IsNsfw = channel.IsNsfw;
            properties.Name = channel.Name;
            properties.PermissionOverwrites = new Optional<IEnumerable<Overwrite>>(channel.PermissionOverwrites.AsEnumerable());
            properties.Position = channel.Position;
            properties.SlowModeInterval = channel.SlowModeInterval;
            properties.Topic = channel.Topic;
        });
    }

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

    [Command("giveitem")]
    [Summary("Give a user an item.")]
    [Remarks("$giveitem Cashmere V Card")]
    public async Task<RuntimeResult> GiveItem(IGuildUser user, [Remainder] string name)
    {
        name = name.Replace(" crate", "");
        if (user.IsBot)
            return CommandResult.FromError("Nope.");

        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        Item item = ItemSystem.GetItem(name);
        switch (item)
        {
            case Collectible:
                await ItemSystem.GiveCollectible(item.Name, Context.Channel, dbUser);
                break;
            case Consumable:
                if (dbUser.Consumables.ContainsKey(item.Name))
                    dbUser.Consumables[item.Name]++;
                else
                    dbUser.Consumables.Add(item.Name, 1);
                break;
            case Crate:
                dbUser.Crates.Add(item.Name);
                break;
            case Perk:
                if (dbUser.Perks.ContainsKey(item.Name))
                    return CommandResult.FromError($"**{user.Sanitize()}** already has a(n) {item}.");
                dbUser.Perks.Add(item.Name, DateTimeOffset.UtcNow.ToUnixTimeSeconds(86400));
                break;
            case Tool:
                if (dbUser.Tools.Contains(item.Name))
                    return CommandResult.FromError($"**{user.Sanitize()}** already has a(n) {item}.");
                dbUser.Tools.Add(item.Name);
                break;
            default:
                return CommandResult.FromError($"**{name}** is not an item!");
        }

        await Context.User.NotifyAsync(Context.Channel, $"Gave **{user.Sanitize()}** a(n) {item}.");
        return CommandResult.FromSuccess();
    }

    [Alias("delachievement", "rmachievement", "delach", "removeach", "rmach")]
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

    [Alias("delcrates", "rmcrates")]
    [Command("removecrates")]
    [Summary("Remove a user's crates.")]
    [Remarks("$removecrates cashmere")]
    public async Task RemoveCrates([Remainder] IGuildUser user)
    {
        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        dbUser.Crates = new List<string>();
        await Context.User.NotifyAsync(Context.Channel, $"Removed **{user.Sanitize()}**'s crates.");
    }

    [Alias("delstat", "rmstat")]
    [Command("removestat")]
    [Summary("Removes a user's stat.")]
    [Remarks("$removestat cashmere Bitches")]
    public async Task<RuntimeResult> RemoveStat(IGuildUser user, [Remainder] string stat)
    {
        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        if (!dbUser.Stats.Remove(stat))
            return CommandResult.FromError("They do not have that stat!");

        await Context.User.NotifyAsync(Context.Channel, $"Removed the **{stat}** stat from {user.Sanitize()}.");
        return CommandResult.FromSuccess();
    }

    [Command("resetcd")]
    [Summary("Reset a user's crime cooldowns.")]
    [Remarks("$resetcd \\*Jazzy Hands\\*")]
    public async Task ResetCooldowns([Remainder] IGuildUser user)
    {
        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        foreach (string cmd in Economy.CmdsWithCooldown)
            dbUser[$"{cmd}Cooldown"] = 0;
        await Context.User.NotifyAsync(Context.Channel, $"Reset **{user.Sanitize()}**'s cooldowns.");
    }

    [Alias("greatreset")]
    [Command("reseteconomy", RunMode = RunMode.Async)]
    [Summary("Reset the economy.")]
    [RequireServerOwner]
    public async Task<RuntimeResult> ResetEconomy()
    {
        await Context.User.NotifyAsync(Context.Channel, "Are you SURE you want to reset the economy?\n**Respond with YES if you're sure. There is no turning back!**");
        InteractiveResult<SocketMessage> iResult = await Interactive.NextMessageAsync(
            x => x.Channel.Id == Context.Channel.Id && x.Author.Id == Context.User.Id,
            timeout: TimeSpan.FromSeconds(20)
        );
        if (!iResult.IsSuccess || !iResult.Value.Content.Equals("yes", StringComparison.OrdinalIgnoreCase))
            return CommandResult.FromError("Reset canceled.");

        await Context.User.NotifyAsync(Context.Channel, "Doing as you say! This will probably take a while.");
        foreach (string key in MemoryCache.Default.Select(kvp => kvp.Key))
        {
            try
            {
                if (MemoryCache.Default.Get(key) is not DbUser)
                    continue;

                MemoryCache.Default.Remove(key);
            }
            catch (NullReferenceException) {}
        }

        QuerySnapshot users = await Program.Database.Collection($"servers/{Context.Guild.Id}/users").GetSnapshotAsync();
        foreach (DocumentSnapshot document in users.Documents)
            await document.Reference.DeleteAsync();

        await Context.User.NotifyAsync(Context.Channel, "Well, there you go. Everything was reset. GG.");
        return CommandResult.FromSuccess();
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
        await ReplyAsync($"Set **{user.Sanitize()}**'s cash to **{amount:C2}**.", allowedMentions: Constants.Mentions);
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
        if (cUp is not ("BTC" or "ETH" or "LTC" or "XRP"))
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
        if (level is < 0 or > Constants.MaxPrestige)
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

    [Command("setvotes")]
    [Summary("Set a user's votes in an election.")]
    [Remarks("$setvotes 3 BowDown097 1000000")]
    [RequireServerOwner]
    public async Task<RuntimeResult> SetVotes(int electionId, IGuildUser user, int votes)
    {
        QuerySnapshot elections = await Program.Database.Collection($"servers/{Context.Guild.Id}/elections").GetSnapshotAsync();
        if (!MemoryCache.Default.Any(k => k.Key.StartsWith("election") && k.Key.EndsWith(electionId.ToString())) && elections.All(r => r.Id != electionId.ToString()))
            return CommandResult.FromError("There is no election with that ID!");

        DbConfigChannels channels = await DbConfigChannels.GetById(Context.Guild.Id);
        if (Context.Guild.TextChannels.All(channel => channel.Id != channels.ElectionsAnnounceChannel))
            return CommandResult.FromError("This server's election announcement channel has yet to be set or no longer exists.");

        DbElection election = await DbElection.GetById(Context.Guild.Id, electionId);
        election.Candidates[user.Id.ToString()] = votes;
        await Polls.UpdateElection(election, channels, Context.Guild);

        return CommandResult.FromSuccess();
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