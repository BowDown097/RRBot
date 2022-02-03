namespace RRBot.Modules;
public class FunnyContext
{
    public SocketCommandContext Context;
    public FunnyContext(SocketCommandContext context) => Context = context;
}

[RequireOwner]
[Summary("Commands for bot owners only.")]
public class BotOwner : ModuleBase<SocketCommandContext>
{
    public CommandService Commands { get; set; }

    [Alias("bot1984")]
    [Command("denylist")]
    [Summary("Ban a user from using the bot.")]
    [Remarks("$denylist [user]")]
    public async Task<RuntimeResult> Blacklist(IGuildUser user)
    {
        if (user.IsBot)
            return CommandResult.FromError("Nope.");

        DbGlobalConfig globalConfig = await DbGlobalConfig.Get();
        globalConfig.BannedUsers.Add(user.Id);
        await Context.User.NotifyAsync(Context.Channel, $"Blacklisted {user.Sanitize()}.");
        return CommandResult.FromSuccess();
    }

    [Command("1984channel")]
    [Summary("Deletes and recreates a text channel, effectively wiping its messages.")]
    [Remarks("$1984channel [channel]")]
    public async Task ClearTextChannel(ITextChannel channel)
    {
        await channel.DeleteAsync();
        });
    }

    [Command("1984cmd")]
    [Summary("Disable a command.")]
    [Remarks("$1984cmd [cmd]")]
    public async Task<RuntimeResult> DisableCommand(string cmd)
    {
        string cmdLower = cmd.ToLower();
        if (cmdLower == "ban")
            return CommandResult.FromError("I don't think that's a good idea.");

        Discord.Commands.SearchResult search = Commands.Search(cmd);
        if (!search.IsSuccess)
            return CommandResult.FromError($"**${cmdLower}** is not a command!");

        DbGlobalConfig globalConfig = await DbGlobalConfig.Get();
        globalConfig.DisabledCommands.Add(cmdLower);
        await Context.User.NotifyAsync(Context.Channel, $"Disabled ${cmdLower}.");
        return CommandResult.FromSuccess();
    }

    [Alias("setuserproperty")]
    [Command("setuserproperty")]
    [Summary("Set a property for a specific user in the database.")]
    [Remarks("$setuserproperty [user] [property] [value]")]
    public async Task<RuntimeResult> SetUserProperty(IGuildUser user, string property, [Remainder] string value)
    {
        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        try
        {
            dbUser[property] = Convert.ChangeType(value, dbUser[property].GetType());
            await Context.User.NotifyAsync(Context.Channel, $"Set {property} to ``{value}`` for {user.Sanitize()}.");
            return CommandResult.FromSuccess();
        }
        catch (Exception e)
        {
            return CommandResult.FromError($"Couldn't set property: {e.Message}");
        }
    }

    [Command("updatedb")]
    [Summary("Pushes all cached data to the database.")]
    [Remarks("$updatedb")]
    public async Task UpdateDB()
    {
        long count = MemoryCache.Default.GetCount();
        foreach (string key in MemoryCache.Default.Select(kvp => kvp.Key))
        {
            try
            {
                if (MemoryCache.Default.Get(key) is not DbObject item)
                    continue;

                await item.Reference.SetAsync(item);
                MemoryCache.Default.Remove(key);
            }
            catch (NullReferenceException) {}
        }

        await Context.User.NotifyAsync(Context.Channel, $"Pushed all **{count}** items in the cache to the database.");
    }
}
