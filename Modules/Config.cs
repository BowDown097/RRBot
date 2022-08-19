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
    public async Task AddRank(int level, double cost, [Remainder] IRole role)
    {
        DbConfigRanks ranks = await DbConfigRanks.GetById(Context.Guild.Id);
        ranks.Costs.Add(level.ToString(), cost);
        ranks.Ids.Add(level.ToString(), role.Id);

        await Context.User.NotifyAsync(Context.Channel, $"Added {role} as a level {level} rank that costs {cost:C2}.");
    }

    [Command("addselfrole")]
    [Summary("Add a self role for the self role message.")]
    [Remarks("$addselfrole \\:Sperg\\: 809512856713166918")]
    public async Task<RuntimeResult> AddSelfRole(IEmote emote, [Remainder] SocketRole role)
    {
        SocketRole authorHighest = (Context.User as SocketGuildUser)?.Roles.OrderBy(r => r.Position).Last();
        if (role.Position >= authorHighest.Position)
            return CommandResult.FromError("Cannot create this selfrole because it is higher than or is the same as your highest role.");

        DbConfigSelfRoles selfRoles = await DbConfigSelfRoles.GetById(Context.Guild.Id);
        if (selfRoles.Channel == 0UL)
            return CommandResult.FromError("The self roles message has not been set. Please set it using ``$setselfrolesmsg``.");

        SocketTextChannel channel = Context.Guild.GetTextChannel(selfRoles.Channel);
        IMessage message = await channel.GetMessageAsync(selfRoles.Message);
        await message.AddReactionAsync(emote);

        selfRoles.SelfRoles.Add(emote.ToString(), role.Id);

        await Context.User.NotifyAsync(Context.Channel, $"Added {role} as a self role bound to {emote}.");
        return CommandResult.FromSuccess();
    }

    [Command("clearconfig")]
    [Summary("Clear all configuration that has been set.")]
    public async Task ClearConfig()
    {
        CollectionReference collection = Program.database.Collection($"servers/{Context.Guild.Id}/config");
        foreach (DocumentReference doc in collection.ListDocumentsAsync().ToEnumerable())
            await doc.DeleteAsync();
        await Context.User.NotifyAsync(Context.Channel, "All configuration cleared!");
    }

    [Command("clearselfroles")]
    [Summary("Clear the self roles that are registered, if any.")]
    public async Task ClearSelfRoles()
    {
        DbConfigSelfRoles selfRoles = await DbConfigSelfRoles.GetById(Context.Guild.Id);
        await selfRoles.Reference.DeleteAsync();
        await Context.User.NotifyAsync(Context.Channel, "Any registered self roles removed!");
    }

    [Command("currentconfig")]
    [Summary("List the current configuration that has been set for the bot.")]
    public async Task CurrentConfig()
    {
        QuerySnapshot config = await Program.database.Collection($"servers/{Context.Guild.Id}/config").GetSnapshotAsync();
        StringBuilder description = new();
        foreach (DocumentSnapshot entry in config.Documents)
        {
            description.AppendLine($"***{entry.Id.ToTitleCase()}***");
            switch (entry.Id)
            {
                case "channels":
                    DbConfigChannels channels = await DbConfigChannels.GetById(Context.Guild.Id);
                    IEnumerable<string> whitelisted = channels.WhitelistedChannels.Select(c => MentionUtils.MentionChannel(c));
                    description.AppendLine(Pair("Command Whitelisted Channels", string.Join(", ", whitelisted)));
                    description.AppendLine($"Election Announcements Channel: {MentionUtils.MentionChannel(channels.ElectionsAnnounceChannel)}");
                    description.AppendLine($"Election Voting Channel: {MentionUtils.MentionChannel(channels.ElectionsVotingChannel)}");
                    description.AppendLine($"Logs Channel: {MentionUtils.MentionChannel(channels.LogsChannel)}");
                    description.AppendLine($"Polls Channel: {MentionUtils.MentionChannel(channels.PollsChannel)}");
                    description.AppendLine($"Pot Channel: {MentionUtils.MentionChannel(channels.PotChannel)}");
                    break;
                case "optionals":
                    DbConfigOptionals optionals = await DbConfigOptionals.GetById(Context.Guild.Id);
                    IEnumerable<string> noFilter = optionals.NoFilterChannels.Select(c => MentionUtils.MentionChannel(c));
                    description.AppendLine(Pair("Disabled Commands", string.Join(", ", optionals.DisabledCommands)));
                    description.AppendLine(Pair("Disabled Modules", string.Join(", ", optionals.DisabledModules)));
                    description.AppendLine(Pair("Filtered Words", string.Join(", ", optionals.FilteredWords)));
                    description.AppendLine($"Invite Filter Enabled: {optionals.InviteFilterEnabled}");
                    description.AppendLine(Pair("No Filter Channels", string.Join(", ", noFilter)));
                    description.AppendLine($"NSFW Enabled: {optionals.NSFWEnabled}");
                    description.AppendLine($"Scam Filter Enabled: {optionals.ScamFilterEnabled}");
                    break;
                case "ranks":
                    DbConfigRanks ranks = await DbConfigRanks.GetById(Context.Guild.Id);
                    foreach (KeyValuePair<string, double> kvp in ranks.Costs.OrderBy(kvp => int.Parse(kvp.Key)))
                    {
                        SocketRole role = Context.Guild.GetRole(ranks.Ids[kvp.Key]);
                        description.AppendLine($"Level {kvp.Key}: {role?.ToString() ?? "(deleted role)"}: {kvp.Value:C2}");
                    }
                    break;
                case "roles":
                    DbConfigRoles roles = await DbConfigRoles.GetById(Context.Guild.Id);
                    SocketRole djRole = Context.Guild.GetRole(roles.DJRole);
                    SocketRole staffLvl1Role = Context.Guild.GetRole(roles.StaffLvl1Role);
                    SocketRole staffLvl2Role = Context.Guild.GetRole(roles.StaffLvl2Role);
                    description.AppendLine($"DJ Role: {djRole?.ToString() ?? "(deleted role)"}");
                    description.AppendLine($"Staff Level 1 Role: {staffLvl1Role?.ToString() ?? "(deleted role)"}");
                    description.AppendLine($"Staff Level 2 Role: {staffLvl2Role?.ToString() ?? "(deleted role)"}");
                    break;
                case "selfroles":
                    DbConfigSelfRoles selfRoles = await DbConfigSelfRoles.GetById(Context.Guild.Id);
                    IMessage message = await Context.Guild.GetTextChannel(selfRoles.Channel)?.GetMessageAsync(selfRoles.Message);
                    string messageContent = message != null ? $"[Jump]({message.GetJumpUrl()})" : "(deleted)";
                    description.AppendLine($"Message: {messageContent}");
                    foreach (KeyValuePair<string, ulong> kvp in selfRoles.SelfRoles)
                    {
                        SocketRole role = Context.Guild.GetRole(kvp.Value);
                        description.AppendLine($"{kvp.Key}: {role?.ToString() ?? "(deleted role)"}");
                    }
                    break;
            }
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

        DbConfigOptionals optionals = await DbConfigOptionals.GetById(Context.Guild.Id);
        optionals.DisabledCommands.Add(cmdLower);
        await Context.User.NotifyAsync(Context.Channel, $"Disabled ${cmdLower}.");
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

        DbConfigOptionals optionals = await DbConfigOptionals.GetById(Context.Guild.Id);
        optionals.DisabledModules.Add(moduleLower);
        await Context.User.NotifyAsync(Context.Channel, $"Disabled the {module.ToTitleCase()} module.");
        return CommandResult.FromSuccess();
    }

    [Command("disablefiltersinchannel")]
    [Summary("Disable filters for a specific channel.")]
    [Remarks("$disablefiltersinchannel \\#extremely-funny")]
    public async Task DisableFiltersInChannel(IChannel channel)
    {
        DbConfigOptionals optionals = await DbConfigOptionals.GetById(Context.Guild.Id);
        optionals.NoFilterChannels.Add(channel.Id);
        await Context.User.NotifyAsync(Context.Channel, $"Disabled filters in {channel.Mention()}.");
    }

    [Command("enablecmd")]
    [Summary("Enable a previously disabled command for your server.")]
    [Remarks("$disablecmd rob")]
    public async Task<RuntimeResult> EnableCommand(string cmd)
    {
        string cmdLower = cmd.ToLower();
        DbConfigOptionals optionals = await DbConfigOptionals.GetById(Context.Guild.Id);
        if (!optionals.DisabledCommands.Remove(cmdLower))
            return CommandResult.FromError($"**{cmdLower}** is not disabled!");

        await Context.User.NotifyAsync(Context.Channel, $"Enabled ${cmdLower}.");
        return CommandResult.FromSuccess();
    }

    [Command("enablemodule")]
    [Summary("Enable a previously disabled module for your server.")]
    [Remarks("$enablemodule fun")]
    public async Task<RuntimeResult> EnableModule(string module)
    {
        string moduleLower = module.ToLower();
        DbConfigOptionals optionals = await DbConfigOptionals.GetById(Context.Guild.Id);
        if (!optionals.DisabledModules.Remove(moduleLower))
            return CommandResult.FromError($"\"{module}\" is not disabled!");

        await Context.User.NotifyAsync(Context.Channel, $"Enabled the {module.ToTitleCase()} module.");
        return CommandResult.FromSuccess();
    }

    [Command("filterword")]
    [Summary("Add a word to filter using the filter system. Word must only contain the characters abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-.")]
    [Remarks("$filterword niggardly")]
    public async Task<RuntimeResult> FilterWord(string word)
    {
        StringBuilder regexString = new();
        foreach (char c in word)
        {
            string cStr = c.ToString();
            if (!FilterSystem.HOMOGLYPHS.ContainsKey(cStr))
                return CommandResult.FromError($"Invalid character found in input: '{cStr}'.");
            regexString.Append($"[{cStr}{string.Concat(FilterSystem.HOMOGLYPHS[cStr])}]");
        }

        DbConfigOptionals optionals = await DbConfigOptionals.GetById(Context.Guild.Id);
        optionals.FilterRegexes.Add(regexString.ToString());
        optionals.FilteredWords.Add(word);
        await Context.User.NotifyAsync(Context.Channel, $"Added \"{word}\" as a filtered word.");
        return CommandResult.FromSuccess();
    }

    [Alias("delselfrole", "rmselfrole")]
    [Command("removeselfrole")]
    [Summary("Remove a registered self role.")]
    [Remarks("$removeselfrole :Sperg:")]
    public async Task<RuntimeResult> RemoveSelfRole(IEmote emote)
    {
        DbConfigSelfRoles selfRoles = await DbConfigSelfRoles.GetById(Context.Guild.Id);
        if (selfRoles.Channel == 0UL)
            return CommandResult.FromError("The self roles message has not been set. Please set it using ``$setselfrolesmsg``.");
        if (!selfRoles.SelfRoles.Remove(emote.ToString()))
            return CommandResult.FromError("There is no selfrole bound to that emote.");

        SocketTextChannel channel = Context.Guild.GetTextChannel(selfRoles.Channel);
        IMessage message = await channel.GetMessageAsync(selfRoles.Message);
        await message.RemoveAllReactionsForEmoteAsync(emote);

        await Context.User.NotifyAsync(Context.Channel, $"Successfully removed the selfrole bound to {emote}.");
        return CommandResult.FromSuccess();
    }

    [Command("setdjrole")]
    [Summary("Register the ID for the DJ role in your server so that some of the music commands work properly.")]
    [Remarks("$setdjrole 850827023982395413")]
    public async Task SetDJRole([Remainder] IRole role)
    {
        DbConfigRoles roles = await DbConfigRoles.GetById(Context.Guild.Id);
        roles.DJRole = role.Id;
        await Context.User.NotifyAsync(Context.Channel, $"Set {role} as the DJ role.");
    }

    [Command("setelectionannouncementschannel")]
    [Summary("Register the ID for the election announcements channel in your server so that elections work properly.")]
    [Remarks("$setelectionannouncementschannel \\#elections")]
    public async Task SetElectionAnnouncementsChannel(IChannel channel)
    {
        DbConfigChannels channels = await DbConfigChannels.GetById(Context.Guild.Id);
        channels.ElectionsAnnounceChannel = channel.Id;
        await Context.User.NotifyAsync(Context.Channel, $"Set election announcements channel to {channel.Mention()}.");
    }

    [Command("setelectionvotingchannel")]
    [Summary("Register the ID for the election voting channel in your server so that elections work properly.")]
    [Remarks("$setelectionvotingchannel \\#vote")]
    public async Task SetElectionVotingChannel(IChannel channel)
    {
        DbConfigChannels channels = await DbConfigChannels.GetById(Context.Guild.Id);
        channels.ElectionsVotingChannel = channel.Id;
        await Context.User.NotifyAsync(Context.Channel, $"Set election voting channel to {channel.Mention()}.");
    }

    [Command("setlogschannel")]
    [Summary("Register the ID for the logs channel in your server so that logging works properly.")]
    [Remarks("$setlogschannel \\#logs")]
    public async Task SetLogsChannel(IChannel channel)
    {
        DbConfigChannels channels = await DbConfigChannels.GetById(Context.Guild.Id);
        channels.LogsChannel = channel.Id;
        await Context.User.NotifyAsync(Context.Channel, $"Set logs channel to {channel.Mention()}.");
    }

    [Command("setpollschannel")]
    [Summary("Register the ID for the polls channel in your server so that polls work properly.")]
    [Remarks("$setpollschannel \\#polls")]
    public async Task SetPollsChannel(IChannel channel)
    {
        DbConfigChannels channels = await DbConfigChannels.GetById(Context.Guild.Id);
        channels.PollsChannel = channel.Id;
        await Context.User.NotifyAsync(Context.Channel, $"Set polls channel to {channel.Mention()}.");
    }

    [Command("setpotchannel")]
    [Summary("Register the ID for the pot channel in your server so that pot winnings are announced.")]
    [Remarks("$setpotchannel \\#bot-commands")]
    public async Task SetPotChannel(IChannel channel)
    {
        DbConfigChannels channels = await DbConfigChannels.GetById(Context.Guild.Id);
        channels.PotChannel = channel.Id;
        await Context.User.NotifyAsync(Context.Channel, $"Set pot channel to {channel.Mention()}.");
    }

    [Command("setselfrolesmsg")]
    [Summary("Register the ID for the message that users can react to to receive roles.")]
    [Remarks("$setselfrolesmsg \\#self-roles 837416517133271063")]
    public async Task<RuntimeResult> SetSelfRolesMsg(ITextChannel channel, ulong msgId)
    {
        IMessage msg = await channel.GetMessageAsync(msgId);
        DbConfigSelfRoles selfRoles = await DbConfigSelfRoles.GetById(Context.Guild.Id);
        selfRoles.Channel = channel.Id;
        selfRoles.Message = msg.Id;
        await Context.User.NotifyAsync(Context.Channel, $"Set self roles message to the one at {msg.GetJumpUrl()}.");
        return CommandResult.FromSuccess();
    }

    [Command("setstafflvl1role")]
    [Summary("Register the ID for the first level Staff role in your server so that staff-related operations work properly.")]
    [Remarks("$setstafflvl1role House")]
    public async Task SetStaffLvl1Role([Remainder] IRole role)
    {
        DbConfigRoles roles = await DbConfigRoles.GetById(Context.Guild.Id);
        roles.StaffLvl1Role = role.Id;
        await Context.User.NotifyAsync(Context.Channel, $"Set first level Staff role to {role}.");
    }

    [Command("setstafflvl2role")]
    [Summary("Register the ID for the second level Staff role in your server so that staff-related operations work properly.")]
    [Remarks("$setstafflvl2role Senate")]
    public async Task SetStaffLvl2Role([Remainder] IRole role)
    {
        DbConfigRoles roles = await DbConfigRoles.GetById(Context.Guild.Id);
        roles.StaffLvl2Role = role.Id;
        await Context.User.NotifyAsync(Context.Channel, $"Set second level Staff role to {role}.");
    }

    [Command("setvotingage")]
    [Summary("Set the minimum voting age for elections.")]
    [Remarks("$setvotingage 14")]
    public async Task SetVotingAge(int days)
    {
        DbConfigChannels channels = await DbConfigChannels.GetById(Context.Guild.Id);
        channels.MinimumVotingAgeDays = days;
        await Context.User.NotifyAsync(Context.Channel, $"Set minimum voting age to **{days} days**.");
    }

    [Command("toggleinvitefilter")]
    [Summary("Toggle the invite filter.")]
    public async Task ToggleInviteFilter()
    {
        DbConfigOptionals optionals = await DbConfigOptionals.GetById(Context.Guild.Id);
        optionals.InviteFilterEnabled = !optionals.InviteFilterEnabled;
        await Context.User.NotifyAsync(Context.Channel, $"Toggled invite filter {(optionals.InviteFilterEnabled ? "ON" : "OFF")}.");
    }

    [Command("togglensfw")]
    [Summary("Toggle the NSFW module.")]
    public async Task ToggleNSFW()
    {
        DbConfigOptionals optionals = await DbConfigOptionals.GetById(Context.Guild.Id);
        optionals.NSFWEnabled = !optionals.NSFWEnabled;
        await Context.User.NotifyAsync(Context.Channel, $"Toggled NSFW enabled {(optionals.NSFWEnabled ? "ON" : "OFF")}.");
    }

    [Command("togglescamfilter")]
    [Summary("Toggle the scam filter.")]
    public async Task ToggleScamFilter()
    {
        DbConfigOptionals optionals = await DbConfigOptionals.GetById(Context.Guild.Id);
        optionals.ScamFilterEnabled = !optionals.ScamFilterEnabled;
        await Context.User.NotifyAsync(Context.Channel, $"Toggled scam filter {(optionals.ScamFilterEnabled ? "ON" : "OFF")}.");
    }

    [Alias("blacklistchannel")]
    [Command("unwhitelistchannel")]
    [Summary("Removes a channel from the whitelist.")]
    [Remarks("$unwhitelistchannel \\#general")]
    public async Task<RuntimeResult> UnwhitelistChannel(IChannel channel)
    {
        DbConfigChannels channels = await DbConfigChannels.GetById(Context.Guild.Id);
        if (!channels.WhitelistedChannels.Remove(channel.Id))
            return CommandResult.FromError($"{channel.Mention()} is not in the whitelist!");
        await Context.User.NotifyAsync(Context.Channel, $"Removed {channel.Mention()} from the whitelist.");
        return CommandResult.FromSuccess();
    }

    [Command("whitelistchannel")]
    [Summary("Adds a channel to a list of whitelisted channels for bot commands. All moderation and music commands will still work in every channel.")]
    [Remarks("$whitelistchannel 837306775987683368")]
    public async Task WhitelistChannel(IChannel channel)
    {
        DbConfigChannels channels = await DbConfigChannels.GetById(Context.Guild.Id);
        channels.WhitelistedChannels.Add(channel.Id);
        await Context.User.NotifyAsync(Context.Channel, $"Whitelisted {channel.Mention()}.");
    }
    #endregion Commands

    #region Helpers
    private static string Pair(string descriptor, object obj)
        => obj is string s ? $"{descriptor}: {(!string.IsNullOrWhiteSpace(s) ? s : "N/A")}" : $"{descriptor}: {obj ?? "N/A"}";
    #endregion
}