namespace RRBot.Modules;
public class FunnyContext(SocketCommandContext context)
{
    public SocketCommandContext Context { get; } = context;
}

[RequireOwner]
[Summary("Commands for bot owners only.")]
public class BotOwner : ModuleBase<SocketCommandContext>
{
    public CommandService Commands { get; set; }

    [Alias("botban")]
    [Command("blacklist")]
    [Summary("Ban a user from using the bot.")]
    [Remarks("$blacklist BowDown097")]
    public async Task<RuntimeResult> Blacklist([Remainder] IGuildUser user)
    {
        if (user.IsBot)
            return CommandResult.FromError("Nope.");

        DbGlobalConfig globalConfig = await MongoManager.FetchGlobalConfigAsync();
        globalConfig.BannedUsers.Add(user.Id);

        await Context.User.NotifyAsync(Context.Channel, $"Blacklisted {user.Sanitize()}.");
        await MongoManager.UpdateObjectAsync(globalConfig);
        return CommandResult.FromSuccess();
    }

    [Command("disablecmdglobal")]
    [Summary("Globally disable a command.")]
    [Remarks("$disablecmdglobal eval")]
    public async Task<RuntimeResult> DisableCommandGlobal(string cmd)
    {
        string cmdLower = cmd.ToLower();
        if (cmdLower is "disablecmdglobal" or "enablecmdglobal")
            return CommandResult.FromError("​I don't think that's a good idea.");

        SearchResult search = Commands.Search(cmd);
        if (!search.IsSuccess)
            return CommandResult.FromError($"**${cmdLower}** is not a command!");
        
        DbGlobalConfig globalConfig = await MongoManager.FetchGlobalConfigAsync();
        globalConfig.DisabledCommands.Add(cmdLower);

        await Context.User.NotifyAsync(Context.Channel, $"Disabled ${cmdLower}.");
        await MongoManager.UpdateObjectAsync(globalConfig);
        return CommandResult.FromSuccess();
    }

    [Command("enablecmdglobal")]
    [Summary("Globally enable a previously disabled command.")]
    [Remarks("$enablecmdglobal disablecmd")]
    public async Task<RuntimeResult> EnableCommandGlobal(string cmd)
    {
        string cmdLower = cmd.ToLower();
        DbGlobalConfig globalConfig = await MongoManager.FetchGlobalConfigAsync();
        if (!globalConfig.DisabledCommands.Remove(cmdLower))
            return CommandResult.FromError($"**{cmdLower}** is not disabled!");

        await Context.User.NotifyAsync(Context.Channel, $"Enabled ${cmdLower}.");
        await MongoManager.UpdateObjectAsync(globalConfig);
        return CommandResult.FromSuccess();
    }

    [Command("eval")]
    [Summary("Execute C# code.")]
    [Remarks("$eval Context.Channel.SendMessageAsync(\"Mods are fat\");")]
    [DoNotSanitize]
    public async Task<RuntimeResult> Eval([Remainder] string code)
    {
        try
        {
            code = code.Replace("```cs", "").Trim('`');
            string[] imports = ["System", "System.Collections.Generic", "System.Text"];
            object evaluation = await CSharpScript.EvaluateAsync(
                code,
                ScriptOptions.Default.WithImports(imports),
                new FunnyContext(Context)
            );

            string codeOutput = $"Your code, ```cs\n{code}``` evaluates to: ```cs\n\"{evaluation}\"```";
            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Code Evaluation")
                .WithDescription(codeOutput.Length <= 4096 ? codeOutput : evaluation.ToString());
            await ReplyAsync(embed: embed.Build());
            return CommandResult.FromSuccess();
        }
        catch (CompilationErrorException cee)
        {
            return CommandResult.FromError($"Compilation error: ``{cee.Message}``");
        }
        catch (Exception e) when (e is not NullReferenceException)
        {
            return CommandResult.FromError($"Other error: ``{e.Message}``");
        }
    }

    [Command("evalsilent")]
    [Summary("Evaluate C# code with no output.")]
    [Remarks("$evalsilent Context.Channel.SendMessageAsync(\"Mods are obese\");")]
    [DoNotSanitize]
    public async Task EvalSilent([Remainder] string code)
    {
        await Context.Message.DeleteAsync();
        code = code.Replace("```cs", "").Trim('`');
        string[] imports = ["System", "System.Collections.Generic", "System.Text"];
        await CSharpScript.EvaluateAsync(code, ScriptOptions.Default.WithImports(imports), new FunnyContext(Context));
    }

    [Command("resetuser")]
    [Summary("Completely reset a user.")]
    [Remarks("$resetuser SmushyTaco")]
    public async Task ResetUser(IGuildUser user)
    {
        await MongoManager.Users.DeleteOneAsync(u => u.GuildId == user.GuildId && u.UserId == user.Id);
        await Context.User.NotifyAsync(Context.Channel, $"Reset **{user.Sanitize()}**.");
    }

    [Alias("setuserproperty")]
    [Command("setuserproperty")]
    [Summary("Set a property for a specific user in the database.")]
    [Remarks("$setuserproperty Dragonpreet Cash NaN")]
    public async Task<RuntimeResult> SetUserProperty(IGuildUser user, string property, [Remainder] string value)
    {
        DbUser dbUser = await MongoManager.FetchUserAsync(user.Id, Context.Guild.Id);
        try
        {
            dbUser[property] = Convert.ChangeType(value, dbUser[property].GetType());
            await Context.User.NotifyAsync(Context.Channel, $"Set {property} to ``{value}`` for {user.Sanitize()}.");
            await MongoManager.UpdateObjectAsync(dbUser);
            return CommandResult.FromSuccess();
        }
        catch (Exception e)
        {
            return CommandResult.FromError($"Couldn't set property: {e.Message}");
        }
    }

    [Alias("unbotban")]
    [Command("unblacklist")]
    [Summary("Unban a user from using the bot.")]
    [Remarks("$unblacklist \"El Pirata Basado\"")]
    public async Task<RuntimeResult> Unblacklist([Remainder] IGuildUser user)
    {
        if (user.IsBot)
            return CommandResult.FromError("Nope.");
        
        DbGlobalConfig globalConfig = await MongoManager.FetchGlobalConfigAsync();
        globalConfig.BannedUsers.Remove(user.Id);

        await Context.User.NotifyAsync(Context.Channel, $"Unblacklisted {user.Sanitize()}.");
        await MongoManager.UpdateObjectAsync(globalConfig);
        return CommandResult.FromSuccess();
    }
}