namespace RRBot.Modules;
[Summary("This is where all the BORING administration stuff goes. Here, you can change how the bot does things in the server in a variety of ways. Huge generalization, but that's the best I can do.")]
[RequireUserPermission(GuildPermission.Administrator)]
public class Config : ModuleBase<SocketCommandContext>
{
    [Command("addrank")]
    [Summary("Register a rank, its level, and the money required to get it.")]
    [Remarks("$addrank [role] [level] [cost]")]
    public async Task AddRank(IRole role, int level, double cost)
    {
        DbConfigRanks ranks = await DbConfigRanks.GetById(Context.Guild.Id);
        ranks.Costs.Add(level.ToString(), cost);
        ranks.Ids.Add(level.ToString(), role.Id);

        await Context.User.NotifyAsync(Context.Channel, $"Added {role} as a level {level} rank that costs {cost:C2}.");
    }

    [Command("addselfrole")]
    [Summary("Add a self role for the self role message.")]
    [Remarks("$addselfrole [emoji] [role]")]
    public async Task<RuntimeResult> AddSelfRole(IEmote emote, IRole role)
    {
        DbConfigSelfRoles selfRoles = await DbConfigSelfRoles.GetById(Context.Guild.Id);
        if (selfRoles.Channel == 0UL)
            return CommandResult.FromError("The self roles message has not been set. Please set it using ``$setselfrolesmsg``.");

        SocketTextChannel channel = Context.Guild.GetChannel(selfRoles.Channel) as SocketTextChannel;
        IMessage message = await channel.GetMessageAsync(selfRoles.Message);
        await message.AddReactionAsync(emote);

        selfRoles.SelfRoles.Add(emote.ToString(), role.Id);

        await Context.User.NotifyAsync(Context.Channel, $"Added {role} as a self role bound to {emote}.");
        return CommandResult.FromSuccess();
    }

    [Command("clearconfig")]
    [Summary("Clear all configuration that has been set.")]
    [Remarks("$clearconfig")]
    public async Task ClearConfig()
    {
        CollectionReference collection = Program.database.Collection($"servers/{Context.Guild.Id}/config");
        foreach (DocumentReference doc in collection.ListDocumentsAsync().ToEnumerable())
            await doc.DeleteAsync();
        await Context.User.NotifyAsync(Context.Channel, "All configuration cleared!");
    }

    [Command("clearselfroles")]
    [Summary("Clear the self roles that are registered, if any.")]
    [Remarks("$clearselfroles")]
    public async Task ClearSelfRoles()
    {
        DbConfigSelfRoles selfRoles = await DbConfigSelfRoles.GetById(Context.Guild.Id);
        await selfRoles.Reference.DeleteAsync();
        await Context.User.NotifyAsync(Context.Channel, "Any registered self roles removed!");
    }

    [Command("currentconfig")]
    [Summary("List the current configuration that has been set for the bot.")]
    [Remarks("$currentconfig")]
    public async Task GetCurrentConfig()
    {
        QuerySnapshot config = await Program.database.Collection($"servers/{Context.Guild.Id}/config").GetSnapshotAsync();
        StringBuilder description = new();
        foreach (DocumentSnapshot entry in config.Documents)
        {
            description.AppendLine($"***{entry.Id}***");
            switch (entry.Id)
            {
                case "channels":
                    DbConfigChannels channels = await DbConfigChannels.GetById(Context.Guild.Id);
                    SocketGuildChannel logsChannel = Context.Guild.GetChannel(channels.LogsChannel);
                    SocketGuildChannel pollsChannel = Context.Guild.GetChannel(channels.PollsChannel);
                    description.AppendLine($"Logs Channel: #{logsChannel?.ToString() ?? "deleted-channel"}");
                    description.AppendLine($"Polls Channel: #{pollsChannel?.ToString() ?? "deleted-channel"}");
                    break;
                case "modules":
                    DbConfigModules modules = await DbConfigModules.GetById(Context.Guild.Id);
                    description.AppendLine($"NSFW Enabled: {modules.NSFWEnabled}");
                    break;
                case "ranks":
                    DbConfigRanks ranks = await DbConfigRanks.GetById(Context.Guild.Id);
                    foreach (KeyValuePair<string, double> kvp in ranks.Costs.OrderBy(kvp => int.Parse(kvp.Key)))
                    {
                        SocketRole role = Context.Guild.GetRole(ranks.Ids[kvp.Key]);
                        description.AppendLine($"Level {kvp.Key} Role: {role?.ToString() ?? "(deleted role)"}");
                        description.AppendLine($"Level {kvp.Key} Cost: {kvp.Value:C2}");
                    }
                    break;
                case "roles":
                    DbConfigRoles roles = await DbConfigRoles.GetById(Context.Guild.Id);
                    SocketRole djRole = Context.Guild.GetRole(roles.DJRole);
                    SocketRole mutedRole = Context.Guild.GetRole(roles.MutedRole);
                    SocketRole staffLvl1Role = Context.Guild.GetRole(roles.StaffLvl1Role);
                    SocketRole staffLvl2Role = Context.Guild.GetRole(roles.StaffLvl2Role);
                    description.AppendLine($"DJ Role: {djRole?.ToString() ?? "(deleted role)"}");
                    description.AppendLine($"Muted Role: {mutedRole?.ToString() ?? "(deleted role)"}");
                    description.AppendLine($"Staff Level 1 Role: {staffLvl1Role?.ToString() ?? "(deleted role)"}");
                    description.AppendLine($"Staff Level 2 Role: {staffLvl2Role?.ToString() ?? "(deleted role)"}");
                    break;
                case "selfroles":
                    DbConfigSelfRoles selfRoles = await DbConfigSelfRoles.GetById(Context.Guild.Id);
                    SocketTextChannel channel = Context.Guild.GetTextChannel(selfRoles.Channel);
                    IMessage message = await channel?.GetMessageAsync(selfRoles.Message);
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

    [Command("setdjrole")]
    [Summary("Register the ID for the DJ role in your server so that most of the music commands work properly with the bot.")]
    [Remarks("$setdjrole [role]")]
    public async Task SetDJRole(IRole role)
    {
        DbConfigRoles roles = await DbConfigRoles.GetById(Context.Guild.Id);
        roles.DJRole = role.Id;
        await Context.User.NotifyAsync(Context.Channel, $"Set {role} as the DJ role.");
    }

    [Command("setlogschannel")]
    [Summary("Register the ID for the logs channel in your server so that logging works properly with the bot.")]
    [Remarks("$setlogschannel [channel]")]
    public async Task SetLogsChannel(IChannel channel)
    {
        DbConfigChannels channels = await DbConfigChannels.GetById(Context.Guild.Id);
        channels.LogsChannel = channel.Id;
        await Context.User.NotifyAsync(Context.Channel, $"Set logs channel to #{channel}.");
    }

    [Command("setmutedrole")]
    [Summary("Register the ID for the Muted role in your server so that mutes work properly with the bot.")]
    [Remarks("$setmutedrole [role]")]
    public async Task SetMutedRole(IRole role)
    {
        DbConfigRoles roles = await DbConfigRoles.GetById(Context.Guild.Id);
        roles.MutedRole = role.Id;
        await Context.User.NotifyAsync(Context.Channel, $"Set muted role to {role}.");
    }

    [Command("setpollschannel")]
    [Summary("Register the ID for the polls channel in your server so that polls work properly with the bot.")]
    [Remarks("$setpollschannel [channel]")]
    public async Task SetPollsChannel(IChannel channel)
    {
        DbConfigChannels channels = await DbConfigChannels.GetById(Context.Guild.Id);
        channels.PollsChannel = channel.Id;
        await Context.User.NotifyAsync(Context.Channel, $"Set polls channel to #{channel}.");
    }

    [Command("setselfrolesmsg")]
    [Summary("Register the ID for the message that users can react to to receive roles.")]
    [Remarks("$setselfrolesmsg [channel] [msg-id]")]
    public async Task<RuntimeResult> SetSelfRolesMsg(IChannel channel, ulong msgId)
    {
        IMessage msg = await (channel as ITextChannel)?.GetMessageAsync(msgId);
        if (msg == null)
            return CommandResult.FromError("You specified an invalid message!");

        DbConfigSelfRoles selfRoles = await DbConfigSelfRoles.GetById(Context.Guild.Id);
        selfRoles.Channel = channel.Id;
        selfRoles.Message = msgId;
        await Context.User.NotifyAsync(Context.Channel, $"Set self roles message to the one at {msg.GetJumpUrl()}.");
        return CommandResult.FromSuccess();
    }

    [Command("setstafflvl1role")]
    [Summary("Register the ID for the first level Staff role in your server so that staff-related operations work properly with the bot.")]
    [Remarks("$setstafflvl1role [role]")]
    public async Task SetStaffLvl1Role(IRole role)
    {
        DbConfigRoles roles = await DbConfigRoles.GetById(Context.Guild.Id);
        roles.StaffLvl1Role = role.Id;
        await Context.User.NotifyAsync(Context.Channel, $"Set first level Staff role to {role}.");
    }

    [Command("setstafflvl2role")]
    [Summary("Register the ID for the second level Staff role in your server so that staff-related operations work properly with the bot.")]
    [Remarks("$setstafflvl2role [role]")]
    public async Task SetStaffLvl2Role(IRole role)
    {
        DbConfigRoles roles = await DbConfigRoles.GetById(Context.Guild.Id);
        roles.StaffLvl2Role = role.Id;
        await Context.User.NotifyAsync(Context.Channel, $"Set second level Staff role to {role}.");
    }

    [Command("togglensfw")]
    [Summary("Toggle the NSFW module.")]
    [Remarks("$togglensfw")]
    public async Task ToggleNSFW()
    {
        DbConfigModules modules = await DbConfigModules.GetById(Context.Guild.Id);
        modules.NSFWEnabled = !modules.NSFWEnabled;
        await ReplyAsync($"Toggled NSFW enabled to {modules.NSFWEnabled}.");
    }
}