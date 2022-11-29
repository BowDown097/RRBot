namespace RRBot.Modules;
[Summary("This is where all the BORING administration stuff goes. Here, you can change how the bot does things in the server in a variety of ways. Huge generalization, but that's the best I can do.")]
[RequireUserPermission(GuildPermission.Administrator)]
public class Config : ModuleBase<SocketCommandContext>
{
    public CommandService Commands { get; set; }

    #region Commands
    [Command("addrank")]
    [Summary("Register a rank, its level, and the money required to get it.")]
    [Remarks("$addrank 1 10000 809512753081483294")]
    public async Task AddRank(int level, decimal cost, [Remainder] IRole role)
    {
        DbConfigRanks ranks = await MongoManager.FetchConfigAsync<DbConfigRanks>(Context.Guild.Id);
        ranks.Costs.Add(level, cost);
        ranks.Ids.Add(level, role.Id);

        await Context.User.NotifyAsync(Context.Channel, $"Added {role} as a level {level} rank that costs {cost:C2}.");
        await MongoManager.UpdateObjectAsync(ranks);
    }

    [Command("addselfrole")]
    [Summary("Add a self role for the self role message.")]
    [Remarks("$addselfrole \\:Sperg\\: 809512856713166918")]
    public async Task<RuntimeResult> AddSelfRole(IEmote emote, [Remainder] SocketRole role)
    {
        SocketRole authorHighest = (Context.User as SocketGuildUser)?.Roles.MaxBy(r => r.Position);
        if (authorHighest != null && role.Position >= authorHighest.Position)
            return CommandResult.FromError("Cannot create this selfrole because it is higher than or is the same as your highest role.");
        
        DbConfigSelfRoles selfRoles = await MongoManager.FetchConfigAsync<DbConfigSelfRoles>(Context.Guild.Id);
        if (selfRoles.Channel == default)
            return CommandResult.FromError("The self roles message has not been set. Please set it using ``$setselfrolesmsg``.");

        SocketTextChannel channel = Context.Guild.GetTextChannel(selfRoles.Channel);
        IMessage message = await channel.GetMessageAsync(selfRoles.Message);
        await message.AddReactionAsync(emote);

        selfRoles.SelfRoles.Add(emote.ToString() ?? "", role.Id);

        await Context.User.NotifyAsync(Context.Channel, $"Added {role} as a self role bound to {emote}.");
        await MongoManager.UpdateObjectAsync(selfRoles);
        return CommandResult.FromSuccess();
    }

    [Command("clearconfig")]
    [Summary("Clear all configuration that has been set.")]
    public async Task ClearConfig()
    {
        await MongoManager.ChannelConfigs.DeleteOneAsync(c => c.GuildId == Context.Guild.Id);
        await MongoManager.MiscConfigs.DeleteOneAsync(c => c.GuildId == Context.Guild.Id);
        await MongoManager.RankConfigs.DeleteOneAsync(c => c.GuildId == Context.Guild.Id);
        await MongoManager.RoleConfigs.DeleteOneAsync(c => c.GuildId == Context.Guild.Id);
        await MongoManager.SelfRoleConfigs.DeleteOneAsync(c => c.GuildId == Context.Guild.Id);
        await Context.User.NotifyAsync(Context.Channel, "All configuration cleared!");
    }

    [Command("clearselfroles")]
    [Summary("Clear the self roles that are registered, if any.")]
    public async Task ClearSelfRoles()
    {
        DbConfigSelfRoles selfRoles = await MongoManager.FetchConfigAsync<DbConfigSelfRoles>(Context.Guild.Id);
        selfRoles.SelfRoles.Clear();
        await Context.User.NotifyAsync(Context.Channel, "Any registered self roles removed!");
        await MongoManager.UpdateObjectAsync(selfRoles);
    }

    [Command("currentconfig")]
    [Summary("List the current configuration that has been set for the bot.")]
    public async Task CurrentConfig()
    {
        StringBuilder description = new();

        description.AppendLine("***Channels***");
        DbConfigChannels channels = await MongoManager.FetchConfigAsync<DbConfigChannels>(Context.Guild.Id);
        IEnumerable<string> whitelisted = channels.WhitelistedChannels.Select(MentionUtils.MentionChannel);
        description.AppendLine(Pair("Command Whitelisted Channels", string.Join(", ", whitelisted)));
        description.AppendLine($"Election Announcements Channel: {MentionUtils.MentionChannel(channels.ElectionsAnnounceChannel)}");
        description.AppendLine($"Election Voting Channel: {MentionUtils.MentionChannel(channels.ElectionsVotingChannel)}");
        description.AppendLine($"Logs Channel: {MentionUtils.MentionChannel(channels.LogsChannel)}");
        description.AppendLine($"Polls Channel: {MentionUtils.MentionChannel(channels.PollsChannel)}");
        description.AppendLine($"Pot Channel: {MentionUtils.MentionChannel(channels.PotChannel)}");

        description.AppendLine("***Miscellaneous***");
        DbConfigMisc misc = await MongoManager.FetchConfigAsync<DbConfigMisc>(Context.Guild.Id);
        IEnumerable<string> noFilter = misc.NoFilterChannels.Select(MentionUtils.MentionChannel);
        description.AppendLine(Pair("Disabled Commands", string.Join(", ", misc.DisabledCommands)));
        description.AppendLine(Pair("Disabled Modules", string.Join(", ", misc.DisabledModules)));
        description.AppendLine(Pair("Filtered Words", string.Join(", ", misc.FilteredWords)));
        description.AppendLine($"Invite Filter Enabled: {misc.InviteFilterEnabled}");
        description.AppendLine(Pair("No Filter Channels", string.Join(", ", noFilter)));
        description.AppendLine($"NSFW Enabled: {misc.NsfwEnabled}");
        description.AppendLine($"Scam Filter Enabled: {misc.ScamFilterEnabled}");

        description.AppendLine("***Ranks***");
        DbConfigRanks ranks = await MongoManager.FetchConfigAsync<DbConfigRanks>(Context.Guild.Id);
        foreach (KeyValuePair<int, decimal> kvp in ranks.Costs.OrderBy(kvp => kvp.Key))
        {
            SocketRole role = Context.Guild.GetRole(ranks.Ids[kvp.Key]);
            description.AppendLine($"Level {kvp.Key} - {role?.ToString() ?? "(deleted role)"}: {kvp.Value:C2}");
        }

        description.AppendLine("***Roles***");
        DbConfigRoles roles = await MongoManager.FetchConfigAsync<DbConfigRoles>(Context.Guild.Id);
        SocketRole djRole = Context.Guild.GetRole(roles.DjRole);
        SocketRole staffLvl1Role = Context.Guild.GetRole(roles.StaffLvl1Role);
        SocketRole staffLvl2Role = Context.Guild.GetRole(roles.StaffLvl2Role);
        description.AppendLine($"DJ Role: {djRole?.ToString() ?? "(deleted role)"}");
        description.AppendLine($"Staff Level 1 Role: {staffLvl1Role?.ToString() ?? "(deleted role)"}");
        description.AppendLine($"Staff Level 2 Role: {staffLvl2Role?.ToString() ?? "(deleted role)"}");

        description.AppendLine("***Self Roles***");
        DbConfigSelfRoles selfRoles = await MongoManager.FetchConfigAsync<DbConfigSelfRoles>(Context.Guild.Id);
        IMessage message = await Context.Guild.GetTextChannel(selfRoles.Channel).GetMessageAsync(selfRoles.Message);
        string messageContent = message != null ? $"[Jump]({message.GetJumpUrl()})" : "(deleted)";
        description.AppendLine($"Message: {messageContent}");
        foreach (KeyValuePair<string, ulong> kvp in selfRoles.SelfRoles)
        {
            SocketRole role = Context.Guild.GetRole(kvp.Value);
            description.AppendLine($"{kvp.Key}: {role?.ToString() ?? "(deleted role)"}");
        }

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Current Configuration")
            .WithDescription(description.ToString());
        await ReplyAsync(embed: embed.Build());
    }

    [Command("disablecmd")]
    [Summary("Disable a command for your server.")]
    [Remarks("$disablecmd rob")]
    public async Task<RuntimeResult> DisableCommand(string cmd)
    {
        string cmdLower = cmd.ToLower();
        if (cmdLower is "disablecmd" or "enablecmd")
            return CommandResult.FromError("I don't think that's a good idea.");

        Discord.Commands.SearchResult search = Commands.Search(cmd);
        if (!search.IsSuccess)
            return CommandResult.FromError($"**${cmdLower}** is not a command!");

        DbConfigMisc misc = await MongoManager.FetchConfigAsync<DbConfigMisc>(Context.Guild.Id);
        misc.DisabledCommands.Add(cmdLower);

        await Context.User.NotifyAsync(Context.Channel, $"Disabled ${cmdLower}.");
        await MongoManager.UpdateObjectAsync(misc);
        return CommandResult.FromSuccess();
    }

    [Command("disablemodule")]
    [Summary("Disable a module for your server.")]
    [Remarks("$disablemodule fun")]
    public async Task<RuntimeResult> DisableModule(string module)
    {
        string moduleLower = module.ToLower();
        if (moduleLower == "config")
            return CommandResult.FromError("I don't think that's a good idea.");

        ModuleInfo moduleInfo = Commands.Modules.FirstOrDefault(m => m.Name.Equals(module, StringComparison.OrdinalIgnoreCase));
        if (moduleInfo == default)
            return CommandResult.FromError($"\"{module}\" is not a module.");
        
        DbConfigMisc misc = await MongoManager.FetchConfigAsync<DbConfigMisc>(Context.Guild.Id);
        misc.DisabledModules.Add(moduleLower);

        await Context.User.NotifyAsync(Context.Channel, $"Disabled the {module.ToTitleCase()} module.");
        await MongoManager.UpdateObjectAsync(misc);
        return CommandResult.FromSuccess();
    }

    [Command("disablefiltersinchannel")]
    [Summary("Disable filters for a specific channel.")]
    [Remarks("$disablefiltersinchannel \\#extremely-funny")]
    public async Task DisableFiltersInChannel(ITextChannel channel)
    {
        DbConfigMisc misc = await MongoManager.FetchConfigAsync<DbConfigMisc>(Context.Guild.Id);
        misc.NoFilterChannels.Add(channel.Id);
        await Context.User.NotifyAsync(Context.Channel, $"Disabled filters in {channel.Mention}.");
        await MongoManager.UpdateObjectAsync(misc);
    }

    [Command("enablecmd")]
    [Summary("Enable a previously disabled command for your server.")]
    [Remarks("$enablecmd rob")]
    public async Task<RuntimeResult> EnableCommand(string cmd)
    {
        string cmdLower = cmd.ToLower();
        DbConfigMisc misc = await MongoManager.FetchConfigAsync<DbConfigMisc>(Context.Guild.Id);
        if (!misc.DisabledCommands.Remove(cmdLower))
            return CommandResult.FromError($"**{cmdLower}** is not disabled!");

        await Context.User.NotifyAsync(Context.Channel, $"Enabled ${cmdLower}.");
        await MongoManager.UpdateObjectAsync(misc);
        return CommandResult.FromSuccess();
    }

    [Command("enablemodule")]
    [Summary("Enable a previously disabled module for your server.")]
    [Remarks("$enablemodule fun")]
    public async Task<RuntimeResult> EnableModule(string module)
    {
        string moduleLower = module.ToLower();
        DbConfigMisc misc = await MongoManager.FetchConfigAsync<DbConfigMisc>(Context.Guild.Id);
        if (!misc.DisabledModules.Remove(moduleLower))
            return CommandResult.FromError($"\"{module}\" is not disabled!");

        await Context.User.NotifyAsync(Context.Channel, $"Enabled the {module.ToTitleCase()} module.");
        await MongoManager.UpdateObjectAsync(misc);
        return CommandResult.FromSuccess();
    }

    [Command("filterword")]
    [Summary("Add a word to filter using the filter system. Word must only contain the characters abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-.")]
    [Remarks("$filterword niggardly")]
    public async Task<RuntimeResult> FilterWord(string word)
    {
        StringBuilder regexString = new();
        foreach (char c in word.ToLower())
        {
            if (!FilterSystem.Homoglyphs.ContainsKey(c))
                return CommandResult.FromError($"Invalid character found in input: '{c}'.");
            regexString.Append($"[{c}{string.Concat(FilterSystem.Homoglyphs[c])}]");
        }
        
        DbConfigMisc misc = await MongoManager.FetchConfigAsync<DbConfigMisc>(Context.Guild.Id);
        misc.FilterRegexes.Add(regexString.ToString());
        misc.FilteredWords.Add(word.ToLower());

        await Context.User.NotifyAsync(Context.Channel, $"Added \"{word}\" as a filtered word.");
        await MongoManager.UpdateObjectAsync(misc);
        return CommandResult.FromSuccess();
    }

    [Alias("delselfrole", "rmselfrole")]
    [Command("removeselfrole")]
    [Summary("Remove a registered self role.")]
    [Remarks("$removeselfrole :Sperg:")]
    public async Task<RuntimeResult> RemoveSelfRole(IEmote emote)
    {
        DbConfigSelfRoles selfRoles = await MongoManager.FetchConfigAsync<DbConfigSelfRoles>(Context.Guild.Id);
        if (selfRoles.Channel == 0UL)
            return CommandResult.FromError("The self roles message has not been set. Please set it using ``$setselfrolesmsg``.");
        if (!selfRoles.SelfRoles.Remove(emote.ToString() ?? ""))
            return CommandResult.FromError("There is no selfrole bound to that emote.");

        SocketTextChannel channel = Context.Guild.GetTextChannel(selfRoles.Channel);
        IMessage message = await channel.GetMessageAsync(selfRoles.Message);
        await message.RemoveAllReactionsForEmoteAsync(emote);

        await Context.User.NotifyAsync(Context.Channel, $"Successfully removed the selfrole bound to {emote}.");
        await MongoManager.UpdateObjectAsync(selfRoles);
        return CommandResult.FromSuccess();
    }

    [Command("setdjrole")]
    [Summary("Register the ID for the DJ role in your server so that some of the music commands work properly.")]
    [Remarks("$setdjrole 850827023982395413")]
    public async Task SetDjRole([Remainder] IRole role)
    {
        DbConfigRoles roles = await MongoManager.FetchConfigAsync<DbConfigRoles>(Context.Guild.Id);
        roles.DjRole = role.Id;
        await Context.User.NotifyAsync(Context.Channel, $"Set {role} as the DJ role.");
        await MongoManager.UpdateObjectAsync(roles);
    }

    [Command("setelectionannouncementschannel")]
    [Summary("Register the ID for the election announcements channel in your server so that elections work properly.")]
    [Remarks("$setelectionannouncementschannel \\#elections")]
    public async Task SetElectionAnnouncementsChannel(ITextChannel channel)
    {
        DbConfigChannels channels = await MongoManager.FetchConfigAsync<DbConfigChannels>(Context.Guild.Id);
        channels.ElectionsAnnounceChannel = channel.Id;
        await Context.User.NotifyAsync(Context.Channel, $"Set election announcements channel to {channel.Mention}.");
        await MongoManager.UpdateObjectAsync(channels);
    }

    [Command("setelectionvotingchannel")]
    [Summary("Register the ID for the election voting channel in your server so that elections work properly.")]
    [Remarks("$setelectionvotingchannel \\#vote")]
    public async Task SetElectionVotingChannel(ITextChannel channel)
    {
        DbConfigChannels channels = await MongoManager.FetchConfigAsync<DbConfigChannels>(Context.Guild.Id);
        channels.ElectionsVotingChannel = channel.Id;
        await Context.User.NotifyAsync(Context.Channel, $"Set election voting channel to {channel.Mention}.");
        await MongoManager.UpdateObjectAsync(channels);
    }

    [Command("setlogschannel")]
    [Summary("Register the ID for the logs channel in your server so that logging works properly.")]
    [Remarks("$setlogschannel \\#logs")]
    public async Task SetLogsChannel(ITextChannel channel)
    {
        DbConfigChannels channels = await MongoManager.FetchConfigAsync<DbConfigChannels>(Context.Guild.Id);
        channels.LogsChannel = channel.Id;
        await Context.User.NotifyAsync(Context.Channel, $"Set logs channel to {channel.Mention}.");
        await MongoManager.UpdateObjectAsync(channels);
    }

    [Command("setpollschannel")]
    [Summary("Register the ID for the polls channel in your server so that polls work properly.")]
    [Remarks("$setpollschannel \\#polls")]
    public async Task SetPollsChannel(ITextChannel channel)
    {
        DbConfigChannels channels = await MongoManager.FetchConfigAsync<DbConfigChannels>(Context.Guild.Id);
        channels.PollsChannel = channel.Id;
        await Context.User.NotifyAsync(Context.Channel, $"Set polls channel to {channel.Mention}.");
        await MongoManager.UpdateObjectAsync(channels);
    }

    [Command("setpotchannel")]
    [Summary("Register the ID for the pot channel in your server so that pot winnings are announced.")]
    [Remarks("$setpotchannel \\#bot-commands")]
    public async Task SetPotChannel(ITextChannel channel)
    {
        DbConfigChannels channels = await MongoManager.FetchConfigAsync<DbConfigChannels>(Context.Guild.Id);
        channels.PotChannel = channel.Id;
        await Context.User.NotifyAsync(Context.Channel, $"Set pot channel to {channel.Mention}.");
        await MongoManager.UpdateObjectAsync(channels);
    }

    [Command("setselfrolesmsg")]
    [Summary("Register the ID for the message that users can react to to receive roles.")]
    [Remarks("$setselfrolesmsg \\#self-roles 837416517133271063")]
    public async Task<RuntimeResult> SetSelfRolesMsg(ITextChannel channel, ulong msgId)
    {
        IMessage msg = await channel.GetMessageAsync(msgId);
        DbConfigSelfRoles selfRoles = await MongoManager.FetchConfigAsync<DbConfigSelfRoles>(Context.Guild.Id);
        selfRoles.Channel = channel.Id;
        selfRoles.Message = msg.Id;

        await Context.User.NotifyAsync(Context.Channel, $"Set self roles message to the one at {msg.GetJumpUrl()}.");
        await MongoManager.UpdateObjectAsync(selfRoles);
        return CommandResult.FromSuccess();
    }

    [Command("setstafflvl1role")]
    [Summary("Register the ID for the first level Staff role in your server so that staff-related operations work properly.")]
    [Remarks("$setstafflvl1role House")]
    public async Task SetStaffLvl1Role([Remainder] IRole role)
    {
        DbConfigRoles roles = await MongoManager.FetchConfigAsync<DbConfigRoles>(Context.Guild.Id);
        roles.StaffLvl1Role = role.Id;
        await Context.User.NotifyAsync(Context.Channel, $"Set first level Staff role to {role}.");
        await MongoManager.UpdateObjectAsync(roles);
    }

    [Command("setstafflvl2role")]
    [Summary("Register the ID for the second level Staff role in your server so that staff-related operations work properly.")]
    [Remarks("$setstafflvl2role Senate")]
    public async Task SetStaffLvl2Role([Remainder] IRole role)
    {
        DbConfigRoles roles = await MongoManager.FetchConfigAsync<DbConfigRoles>(Context.Guild.Id);
        roles.StaffLvl2Role = role.Id;
        await Context.User.NotifyAsync(Context.Channel, $"Set second level Staff role to {role}.");
        await MongoManager.UpdateObjectAsync(roles);
    }

    [Command("setvotingage")]
    [Summary("Set the minimum voting age for elections.")]
    [Remarks("$setvotingage 14")]
    public async Task SetVotingAge(int days)
    {
        DbConfigChannels channels = await MongoManager.FetchConfigAsync<DbConfigChannels>(Context.Guild.Id);
        channels.MinimumVotingAgeDays = days;
        await Context.User.NotifyAsync(Context.Channel, $"Set minimum voting age to **{days} days**.");
        await MongoManager.UpdateObjectAsync(channels);
    }

    [Command("toggledrops")]
    [Summary("Toggles random drops, such as Bank Cheques.")]
    public async Task ToggleDrops()
    {
        DbConfigMisc misc = await MongoManager.FetchConfigAsync<DbConfigMisc>(Context.Guild.Id);
        misc.DropsDisabled = !misc.DropsDisabled;
        await Context.User.NotifyAsync(Context.Channel, $"Toggled random drops {(misc.DropsDisabled ? "OFF" : "ON")}.");
        await MongoManager.UpdateObjectAsync(misc);
    }

    [Command("toggleinvitefilter")]
    [Summary("Toggle the invite filter.")]
    public async Task ToggleInviteFilter()
    {
        DbConfigMisc misc = await MongoManager.FetchConfigAsync<DbConfigMisc>(Context.Guild.Id);
        misc.InviteFilterEnabled = !misc.InviteFilterEnabled;
        await Context.User.NotifyAsync(Context.Channel, $"Toggled invite filter {(misc.InviteFilterEnabled ? "ON" : "OFF")}.");
        await MongoManager.UpdateObjectAsync(misc);
    }

    [Command("togglensfw")]
    [Summary("Toggle the NSFW module.")]
    public async Task ToggleNsfw()
    {
        DbConfigMisc misc = await MongoManager.FetchConfigAsync<DbConfigMisc>(Context.Guild.Id);
        misc.NsfwEnabled = !misc.NsfwEnabled;
        await Context.User.NotifyAsync(Context.Channel, $"Toggled NSFW enabled {(misc.NsfwEnabled ? "ON" : "OFF")}.");
        await MongoManager.UpdateObjectAsync(misc);
    }

    [Command("togglescamfilter")]
    [Summary("Toggle the scam filter.")]
    public async Task ToggleScamFilter()
    {
        DbConfigMisc misc = await MongoManager.FetchConfigAsync<DbConfigMisc>(Context.Guild.Id);
        misc.ScamFilterEnabled = !misc.ScamFilterEnabled;
        await Context.User.NotifyAsync(Context.Channel, $"Toggled scam filter {(misc.ScamFilterEnabled ? "ON" : "OFF")}.");
        await MongoManager.UpdateObjectAsync(misc);
    }
    
    [Command("unfilterword")]
    [Summary("Remove a word from the filter system.")]
    [Remarks("$unfilterword niggardly")]
    public async Task<RuntimeResult> UnfilterWord(string word)
    {
        DbConfigMisc misc = await MongoManager.FetchConfigAsync<DbConfigMisc>(Context.Guild.Id);
        Regex regex = misc.FilterRegexes.Select(rs => new Regex(rs)).FirstOrDefault(r => r.IsMatch(word.ToLower()));
        if (regex is null)
            return CommandResult.FromError("That word appears to not be in the filter system.");

        misc.FilterRegexes.Remove(regex.ToString());
        misc.FilteredWords.Remove(word.ToLower());

        await Context.User.NotifyAsync(Context.Channel, $"Removed \"{word}\" from the filter system.");
        await MongoManager.UpdateObjectAsync(misc);
        return CommandResult.FromSuccess();
    }

    [Alias("blacklistchannel")]
    [Command("unwhitelistchannel")]
    [Summary("Removes a channel from the whitelist.")]
    [Remarks("$unwhitelistchannel \\#general")]
    public async Task<RuntimeResult> UnwhitelistChannel(ITextChannel channel)
    {
        DbConfigChannels channels = await MongoManager.FetchConfigAsync<DbConfigChannels>(Context.Guild.Id);
        if (!channels.WhitelistedChannels.Remove(channel.Id))
            return CommandResult.FromError($"{channel.Mention} is not in the whitelist.");

        await Context.User.NotifyAsync(Context.Channel, $"Removed {channel.Mention} from the whitelist.");
        await MongoManager.UpdateObjectAsync(channels);
        return CommandResult.FromSuccess();
    }

    [Command("whitelistchannel")]
    [Summary("Adds a channel to a list of whitelisted channels for bot commands. All moderation and music commands will still work in every channel.")]
    [Remarks("$whitelistchannel 837306775987683368")]
    public async Task WhitelistChannel(ITextChannel channel)
    {
        DbConfigChannels channels = await MongoManager.FetchConfigAsync<DbConfigChannels>(Context.Guild.Id);
        channels.WhitelistedChannels.Add(channel.Id);
        await Context.User.NotifyAsync(Context.Channel, $"Whitelisted {channel.Mention}.");
        await MongoManager.UpdateObjectAsync(channels);
    }
    #endregion Commands

    #region Helpers
    private static string Pair(string descriptor, object obj)
        => obj is string s ? $"{descriptor}: {(!string.IsNullOrWhiteSpace(s) ? s : "N/A")}" : $"{descriptor}: {obj ?? "N/A"}";
    #endregion
}