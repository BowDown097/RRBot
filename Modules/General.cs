namespace RRBot.Modules;
[Summary("The name really explains it all. Fun fact, you used one of the commands under this module to view info about this module.")]
public class General : ModuleBase<SocketCommandContext>
{
    public CommandService Commands { get; set; }

    [Alias("ach")]
    [Command("achievements")]
    [Summary("View your own or someone else's achievements.")]
    [Remarks("$achievements toes69ing")]
    public async Task Achievements(IGuildUser user = null)
    {
        ulong userId = user != null ? user.Id : Context.User.Id;
        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, userId);
        StringBuilder description = new();
        foreach (KeyValuePair<string, string> achievement in dbUser.Achievements)
            description.AppendLine($"**{achievement.Key}**: {achievement.Value}");

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Achievements")
            .WithDescription(description.Length > 0 ? description.ToString() : "None");
        await ReplyAsync(embed: embed.Build());
    }

    [Command("help")]
    [Summary("View info about a command.")]
    [Remarks("$help help")]
    public async Task<RuntimeResult> Help(string command = "")
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            await ReplyAsync("Use $modules to see all of this bot's modules and use $module to view the commands in a module.");
            return CommandResult.FromSuccess();
        }

        Discord.Commands.SearchResult search = Commands.Search(command);
        if (!search.IsSuccess)
            return CommandResult.FromError("You have specified a nonexistent command!");

        CommandInfo commandInfo = search.Commands[0].Command;
        if (commandInfo.TryGetPrecondition<RequireNsfwEnabledAttribute>())
        {
            DbConfigOptionals modules = await DbConfigOptionals.GetById(Context.Guild.Id);
            if (!modules.NSFWEnabled)
                return CommandResult.FromError("NSFW commands are disabled!");
        }
        if (commandInfo.TryGetPrecondition<RequireRushRebornAttribute>()
            && !(Context.Guild.Id is RequireRushRebornAttribute.RR_MAIN or RequireRushRebornAttribute.RR_TEST))
        {
            return CommandResult.FromError("You have specified a nonexistent command!");
        }

        StringBuilder preconditions = new();
        if (commandInfo.TryGetPrecondition<CheckPacifistAttribute>())
            preconditions.AppendLine("Requires not having the Pacifist perk equipped");
        if (commandInfo.TryGetPrecondition<RequireCashAttribute>())
            preconditions.AppendLine("Requires any amount of cash");
        if (commandInfo.TryGetPrecondition<RequireDJAttribute>())
            preconditions.AppendLine("Requires DJ");
        if (commandInfo.TryGetPrecondition<RequireNsfwAttribute>())
            preconditions.AppendLine("Must be in NSFW channel");
        if (commandInfo.TryGetPrecondition<RequireOwnerAttribute>())
            preconditions.AppendLine("Requires Bot Owner");
        if (commandInfo.TryGetPrecondition<RequireRushRebornAttribute>())
            preconditions.AppendLine("Exclusive to Rush Reborn");
        if (commandInfo.TryGetPrecondition<RequirePerkAttribute>())
            preconditions.AppendLine("Requires a perk");
        if (commandInfo.TryGetPrecondition(out RequireRankLevelAttribute rRL))
            preconditions.AppendLine($"Requires rank level {rRL.RankLevel}");
        if (commandInfo.TryGetPrecondition<RequireStaffAttribute>())
            preconditions.AppendLine("Requires Staff");
        if (commandInfo.TryGetPrecondition(out RequireBeInChannelAttribute rBIC))
            preconditions.AppendLine($"Must be in #{rBIC.Name}");
        if (commandInfo.TryGetPrecondition(out RequireToolAttribute ri))
            preconditions.AppendLine(string.IsNullOrEmpty(ri.ToolType) ? "Requires a tool" : $"Requires {ri.ToolType}");
        if (commandInfo.TryGetPrecondition(out RequireUserPermissionAttribute rUP))
            preconditions.AppendLine($"Requires {Enum.GetName(rUP.GuildPermission.GetValueOrDefault())} permission");

        EmbedBuilder commandEmbed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithDescription("**" + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(command.ToLower()) + "**")
            .RRAddField("Description", commandInfo.Summary)
            .RRAddField("Usage", commandInfo.GetUsage())
            .RRAddField("Example", commandInfo.Remarks)
            .RRAddField("Aliases", string.Join(", ", commandInfo.Aliases.Where(a => a != commandInfo.Name)))
            .RRAddField("Preconditions", preconditions);
        await ReplyAsync(embed: commandEmbed.Build());
        return CommandResult.FromSuccess();
    }

    [Command("module")]
    [Summary("View info about a module.")]
    [Remarks("$module administration")]
    public async Task<RuntimeResult> Module(string module)
    {
        ModuleInfo moduleInfo = Commands.Modules.FirstOrDefault(m => m.Name.Equals(module, StringComparison.OrdinalIgnoreCase));
        if (moduleInfo == default)
            return CommandResult.FromError("You have specified a nonexistent module!");

        if (moduleInfo.Name == "NSFW")
        {
            DbConfigOptionals modules = await DbConfigOptionals.GetById(Context.Guild.Id);
            if (!modules.NSFWEnabled)
                return CommandResult.FromError("NSFW commands are disabled!");
        }
        if (moduleInfo.Name == "Support")
        {
            return CommandResult.FromError("You have specified a nonexistent module!");
        }

        EmbedBuilder moduleEmbed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithDescription("**" + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(module.ToLower()) + "**")
            .RRAddField("Available commands", string.Join(", ", moduleInfo.Commands.Select(x => x.Name)))
            .RRAddField("Description", moduleInfo.Summary);
        await ReplyAsync(embed: moduleEmbed.Build());
        return CommandResult.FromSuccess();
    }

    [Command("modules")]
    [Summary("View info about the bot's modules.")]
    public async Task Modules()
    {
        List<string> modulesList = Commands.Modules.Select(x => x.Name).ToList();
        if (!(Context.Guild.Id is RequireRushRebornAttribute.RR_MAIN or RequireRushRebornAttribute.RR_TEST))
        {
            modulesList.Remove("Support");
        }

        EmbedBuilder modulesEmbed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Modules")
            .WithDescription(string.Join(", ", modulesList));
        await ReplyAsync(embed: modulesEmbed.Build());
    }

    [Alias("guildinfo")]
    [Command("serverinfo")]
    [Summary("View info about this server.")]
    public async Task ServerInfo()
    {
        string banner = Context.Guild.BannerUrl;
        string discovery = Context.Guild.DiscoverySplashUrl;
        string icon = Context.Guild.IconUrl;
        string invSplash = Context.Guild.SplashUrl;
        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor(Context.Guild.Name, Context.Guild.IconUrl)
            .WithColor(Color.Red)
            .WithDescription("**Server Info**")
            .WithThumbnailUrl(Context.Guild.IconUrl)
            .RRAddField("Banner", !string.IsNullOrWhiteSpace(banner) ? $"[Here]({banner})" : "N/A", true)
            .RRAddField("Discovery Splash", !string.IsNullOrWhiteSpace(discovery) ? $"[Here]({discovery})" : "N/A", true)
            .RRAddField("Icon", !string.IsNullOrWhiteSpace(icon) ? $"[Here]({icon})" : "N/A", true)
            .RRAddField("Invite Splash", !string.IsNullOrWhiteSpace(invSplash) ? $"[Here]({invSplash})" : "N/A", true)
            .AddSeparatorField()
            .RRAddField("Categories", Context.Guild.CategoryChannels.Count, true)
            .RRAddField("Text Channels", Context.Guild.TextChannels.Count, true)
            .RRAddField("Voice Channels", Context.Guild.VoiceChannels.Count, true)
            .AddSeparatorField()
            .RRAddField("Boosts", Context.Guild.PremiumSubscriptionCount, true)
            .RRAddField("Emotes", Context.Guild.Emotes.Count, true)
            .RRAddField("Members", Context.Guild.MemberCount, true)
            .RRAddField("Roles", Context.Guild.Roles.Count, true)
            .RRAddField("Stickers", Context.Guild.Stickers.Count, true)
            .RRAddField("Upload Limit", $"{Context.Guild.MaxUploadLimit/1000000} MB", true)
            .AddSeparatorField()
            .RRAddField("Created At", Context.Guild.CreatedAt)
            .RRAddField("Description", Context.Guild.Description)
            .RRAddField("ID", Context.Guild.Id)
            .RRAddField("Owner", Context.Guild.Owner)
            .RRAddField("Vanity URL", Context.Guild.VanityURLCode);

        await ReplyAsync(embed: embed.Build());
    }

    [Alias("statistics")]
    [Command("stats")]
    [Summary("View various statistics about your own, or another user's, bot usage.")]
    [Remarks("$stats Ross")]
    public async Task<RuntimeResult> Stats(IGuildUser user = null)
    {
        if (user?.IsBot == true)
            return CommandResult.FromError("Nope.");

        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user?.Id ?? Context.User.Id);
        if (dbUser.Stats.Count == 0)
            return CommandResult.FromError(user == null ? "You have no available stats!" : $"**{user.Sanitize()}** has no available stats!");

        StringBuilder description = new();
        foreach (string key in dbUser.Stats.Keys.ToList().OrderBy(s => s))
            description.AppendLine($"**{key}**: {dbUser.Stats[key]}");

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Stats")
            .WithDescription(description.ToString());
        await ReplyAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }

    [Alias("whois", "profile", "memberinfo")]
    [Command("userinfo")]
    [Summary("View info about a user.")]
    [Remarks("$userinfo Moth")]
    public async Task UserInfo(SocketGuildUser user)
    {
        IEnumerable<string> perms = user.GuildPermissions.ToList().Select(p => Enum.GetName(p).SplitPascalCase());

        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor(user)
            .WithColor(Color.Red)
            .WithDescription("**User Info**")
            .WithThumbnailUrl(user.GetAvatarUrl())
            .RRAddField("ID", user.Id, true)
            .RRAddField("Nickname", user.Nickname, true)
            .AddSeparatorField()
            .RRAddField("Joined At", user.JoinedAt.Value, true)
            .RRAddField("Created At", user.CreatedAt, true)
            .AddSeparatorField()
            .RRAddField("Permissions", string.Join(", ", perms))
            .RRAddField("Roles", string.Join(" ", user.Roles.Select(r => r.Mention)));

        await ReplyAsync(embed: embed.Build());
    }
}