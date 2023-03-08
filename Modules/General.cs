namespace RRBot.Modules;
[Summary("The name really explains it all. Fun fact, you used one of the commands under this module to view info about this module.")]
public class General : ModuleBase<SocketCommandContext>
{
    public CommandService Commands { get; set; }

    [Alias("ach")]
    [Command("achievements")]
    [Summary("View your own or someone else's achievements.")]
    [Remarks("$achievements toes69ing")]
    public async Task Achievements([Remainder] IGuildUser user = null)
    {
        DbUser dbUser = await MongoManager.FetchUserAsync(user?.Id ?? Context.User.Id, Context.Guild.Id);
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

        SearchResult search = Commands.Search(command);
        if (!search.IsSuccess)
            return CommandResult.FromError("You have specified a nonexistent command!");

        CommandInfo commandInfo = search.Commands[0].Command;
        StringBuilder preconditions = new();
        if (commandInfo.TryGetPrecondition<CheckPacifistAttribute>())
            preconditions.AppendLine("Requires not having the Pacifist perk equipped");
        if (commandInfo.TryGetPrecondition(out RequireCashAttribute requireCashAttr))
            preconditions.AppendLine($"Requires {(requireCashAttr.Cash > 0.01m ? requireCashAttr.Cash.ToString("C2") : "any amount of cash")}");
        if (commandInfo.TryGetPrecondition<RequireDjAttribute>())
            preconditions.AppendLine("Requires DJ");
        if (commandInfo.TryGetPrecondition<RequireOwnerAttribute>())
            preconditions.AppendLine("Requires Bot Owner");
        if (commandInfo.TryGetPrecondition<RequirePerkAttribute>())
            preconditions.AppendLine("Requires a perk");
        if (commandInfo.TryGetPrecondition(out RequireRankLevelAttribute rankLevelAttr))
            preconditions.AppendLine($"Requires rank level {rankLevelAttr.RankLevel}");
        if (commandInfo.TryGetPrecondition<RequireServerOwnerAttribute>())
            preconditions.AppendLine("Requires Server Owner");
        if (commandInfo.TryGetPrecondition<RequireStaffAttribute>())
            preconditions.AppendLine("Requires Staff");
        if (commandInfo.TryGetPrecondition(out RequireBeInChannelAttribute requireChannelAttr))
            preconditions.AppendLine($"Must be in #{requireChannelAttr.Name}");
        if (commandInfo.TryGetPrecondition(out RequireToolAttribute requireToolAttr))
            preconditions.AppendLine(string.IsNullOrEmpty(requireToolAttr.ToolType) ? "Requires a tool" : $"Requires {requireToolAttr.ToolType}");
        if (commandInfo.TryGetPrecondition(out RequireUserPermissionAttribute requirePermAttr))
            preconditions.AppendLine($"Requires {Enum.GetName(requirePermAttr.GuildPermission.GetValueOrDefault())} permission");

        EmbedBuilder commandEmbed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithDescription($"**{command.ToTitleCase()}**")
            .RrAddField("Description", commandInfo.Summary)
            .RrAddField("Usage", commandInfo.GetUsage())
            .RrAddField("Example", commandInfo.Remarks)
            .RrAddField("Aliases", string.Join(", ", commandInfo.Aliases.Where(a => a != commandInfo.Name)))
            .RrAddField("Preconditions", preconditions);
        await ReplyAsync(embed: commandEmbed.Build());
        return CommandResult.FromSuccess();
    }

    [Command("info")]
    [Summary("View info about the bot.")]
    public async Task Info()
    {
        int usersCount = Context.Client.Guilds.Aggregate(0, (total, next) => total + next.MemberCount);
        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor("Rush Reborn Bot", Context.Client.CurrentUser.GetAvatarUrl())
            .WithColor(Color.Red)
            .WithDescription("An epic bot with a cash system, music commands, moderation commands, and much, much more.")
            .AddField("Serving", $"{usersCount} users across {Context.Client.Guilds.Count} servers", true)
            .AddField("Uptime", (DateTime.Now - Constants.StartTime).FormatCompound(), true)
            .AddField("Latency", Context.Client.Latency, true)
            .AddField("Commands", Commands.Commands.Count(), true)
            .AddField("Modules", Commands.Modules.Count(), true)
            .AddField("Support Discord", "[Join](https://discord.gg/USpJnaaNap)", true)
            .WithFooter("Developer: BowDown097#8946 • Please contribute! You will be added to this list.");
        await ReplyAsync(embed: embed.Build());
    }

    [Command("module")]
    [Summary("View info about a module.")]
    [Remarks("$module administration")]
    public async Task<RuntimeResult> Module(string module)
    {
        ModuleInfo moduleInfo = Commands.Modules.FirstOrDefault(m => m.Name.Equals(module, StringComparison.OrdinalIgnoreCase));
        if (moduleInfo == default)
            return CommandResult.FromError("You have specified a nonexistent module!");

        EmbedBuilder moduleEmbed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithDescription($"**{module.ToTitleCase()}**")
            .RrAddField("Available commands", string.Join(", ", moduleInfo.Commands.Select(x => x.Name)))
            .RrAddField("Description", moduleInfo.Summary);
        await ReplyAsync(embed: moduleEmbed.Build());
        return CommandResult.FromSuccess();
    }

    [Command("modules")]
    [Summary("View info about the bot's modules.")]
    public async Task Modules()
    {
        EmbedBuilder modulesEmbed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Modules")
            .WithDescription(string.Join(", ", Commands.Modules.Select(x => x.Name).ToList()));
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
            .RrAddField("Banner", !string.IsNullOrWhiteSpace(banner) ? $"[Here]({banner})" : "N/A", true)
            .RrAddField("Discovery Splash", !string.IsNullOrWhiteSpace(discovery) ? $"[Here]({discovery})" : "N/A", true)
            .RrAddField("Icon", !string.IsNullOrWhiteSpace(icon) ? $"[Here]({icon})" : "N/A", true)
            .RrAddField("Invite Splash", !string.IsNullOrWhiteSpace(invSplash) ? $"[Here]({invSplash})" : "N/A", true)
            .AddSeparatorField()
            .RrAddField("Categories", Context.Guild.CategoryChannels.Count, true)
            .RrAddField("Text Channels", Context.Guild.TextChannels.Count, true)
            .RrAddField("Voice Channels", Context.Guild.VoiceChannels.Count, true)
            .AddSeparatorField()
            .RrAddField("Boosts", Context.Guild.PremiumSubscriptionCount, true)
            .RrAddField("Emotes", Context.Guild.Emotes.Count, true)
            .RrAddField("Members", Context.Guild.MemberCount, true)
            .RrAddField("Roles", Context.Guild.Roles.Count, true)
            .RrAddField("Stickers", Context.Guild.Stickers.Count, true)
            .RrAddField("Upload Limit", $"{Context.Guild.MaxUploadLimit/1000000} MB", true)
            .AddSeparatorField()
            .RrAddField("Created At", Context.Guild.CreatedAt)
            .RrAddField("Description", Context.Guild.Description)
            .RrAddField("ID", Context.Guild.Id)
            .RrAddField("Owner", Context.Guild.Owner)
            .RrAddField("Vanity URL", Context.Guild.VanityURLCode);

        await ReplyAsync(embed: embed.Build());
    }

    [Alias("statistics")]
    [Command("stats")]
    [Summary("View various statistics about your own, or another user's, bot usage.")]
    [Remarks("$stats Ross")]
    public async Task<RuntimeResult> Stats([Remainder] IGuildUser user = null)
    {
        if (user?.IsBot == true)
            return CommandResult.FromError("Nope.");
        
        DbUser dbUser = await MongoManager.FetchUserAsync(user?.Id ?? Context.User.Id, Context.Guild.Id);
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

    [Alias("whois", "memberinfo")]
    [Command("userinfo")]
    [Summary("View info about yourself or another user.")]
    [Remarks("$userinfo Moth")]
    public async Task UserInfo([Remainder] SocketGuildUser user = null)
    {
        user ??= Context.User as SocketGuildUser;
        IEnumerable<string> perms = user.GuildPermissions.ToList().Select(p => Enum.GetName(p).SplitPascalCase());

        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor(user)
            .WithColor(Color.Red)
            .WithDescription("**User Info**")
            .WithThumbnailUrl(user.GetAvatarUrl())
            .RrAddField("ID", user.Id, true)
            .RrAddField("Nickname", user.Nickname, true)
            .AddSeparatorField()
            .RrAddField("Joined At", user.JoinedAt ?? DateTimeOffset.MinValue, true)
            .RrAddField("Created At", user.CreatedAt, true)
            .AddSeparatorField()
            .RrAddField("Permissions", string.Join(", ", perms.Where(p => p != null)))
            .RrAddField("Roles", string.Join(" ", user.Roles.Select(r => r.Mention)));

        await ReplyAsync(embed: embed.Build());
    }
}