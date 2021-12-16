namespace RRBot.Systems;
public class EventSystem
{
    private readonly AudioSystem audioSystem;
    private readonly CommandService commands;
    private readonly DiscordSocketClient client;
    private readonly LavaSocketClient lavaSocketClient;
    private readonly ServiceProvider serviceProvider;

    public EventSystem(ServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        audioSystem = serviceProvider.GetRequiredService<AudioSystem>();
        commands = serviceProvider.GetRequiredService<CommandService>();
        client = serviceProvider.GetRequiredService<DiscordSocketClient>();
        lavaSocketClient = serviceProvider.GetRequiredService<LavaSocketClient>();
    }

    public void SubscribeEvents()
    {
        client.ButtonExecuted += Client_ButtonExecuted;
        client.GuildMemberUpdated += Client_GuildMemberUpdated;
        client.Log += Client_Log;
        client.MessageReceived += Client_MessageReceived;
        client.MessageUpdated += Client_MessageUpdated;
        client.ReactionAdded += Client_ReactionAdded;
        client.ReactionRemoved += Client_ReactionRemoved;
        client.Ready += Client_Ready;
        client.ThreadCreated += Client_ThreadCreated;
        client.ThreadUpdated += Client_ThreadUpdated;
        client.UserJoined += Client_UserJoined;
        commands.CommandExecuted += Commands_CommandExecuted;

        client.ChannelCreated += LoggingSystem.Client_ChannelCreated;
        client.ChannelDestroyed += LoggingSystem.Client_ChannelDestroyed;
        client.ChannelUpdated += LoggingSystem.Client_ChannelUpdated;
        client.GuildMemberUpdated += LoggingSystem.Client_GuildMemberUpdated;
        client.GuildScheduledEventCancelled += LoggingSystem.Client_GuildScheduledEventCancelled;
        client.GuildScheduledEventCompleted += LoggingSystem.Client_GuildScheduledEventCompleted;
        client.GuildScheduledEventCreated += LoggingSystem.Client_GuildScheduledEventCreated;
        client.GuildScheduledEventStarted += LoggingSystem.Client_GuildScheduledEventStarted;
        client.GuildScheduledEventUpdated += LoggingSystem.Client_GuildScheduledEventUpdated;
        client.GuildScheduledEventUserAdd += LoggingSystem.Client_GuildScheduledEventUserAdd;
        client.GuildScheduledEventUserRemove += LoggingSystem.Client_GuildScheduledEventUserRemove;
        client.GuildStickerCreated += LoggingSystem.Client_GuildStickerCreated;
        client.GuildStickerDeleted += LoggingSystem.Client_GuildStickerDeleted;
        client.GuildUpdated += LoggingSystem.Client_GuildUpdated;
        client.InviteCreated += LoggingSystem.Client_InviteCreated;
        client.InviteDeleted += LoggingSystem.Client_InviteDeleted;
        client.MessageDeleted += LoggingSystem.Client_MessageDeleted;
        client.MessageUpdated += LoggingSystem.Client_MessageUpdated;
        client.ReactionAdded += LoggingSystem.Client_ReactionAdded;
        client.ReactionRemoved += LoggingSystem.Client_ReactionRemoved;
        client.RoleCreated += LoggingSystem.Client_RoleCreated;
        client.RoleDeleted += LoggingSystem.Client_RoleDeleted;
        client.RoleUpdated += LoggingSystem.Client_RoleUpdated;
        client.SpeakerAdded += LoggingSystem.Client_SpeakerAdded;
        client.SpeakerRemoved += LoggingSystem.Client_SpeakerRemoved;
        client.StageEnded += LoggingSystem.Client_StageEnded;
        client.StageStarted += LoggingSystem.Client_StageStarted;
        client.StageUpdated += LoggingSystem.Client_StageUpdated;
        client.ThreadCreated += LoggingSystem.Client_ThreadCreated;
        client.ThreadDeleted += LoggingSystem.Client_ThreadDeleted;
        client.ThreadMemberJoined += LoggingSystem.Client_ThreadMemberJoined;
        client.ThreadMemberLeft += LoggingSystem.Client_ThreadMemberLeft;
        client.ThreadUpdated += LoggingSystem.Client_ThreadUpdated;
        client.UserBanned += LoggingSystem.Client_UserBanned;
        client.UserJoined += LoggingSystem.Client_UserJoined;
        client.UserLeft += LoggingSystem.Client_UserLeft;
        client.UserUnbanned += LoggingSystem.Client_UserUnbanned;
        client.UserVoiceStateUpdated += LoggingSystem.Client_UserVoiceStateUpdated;
    }

    private static async Task HandleReactionAsync(Cacheable<IMessageChannel, ulong> channelCached, SocketReaction reaction, bool addedReaction)
    {
        IMessageChannel channel = await channelCached.GetOrDownloadAsync();
        SocketGuildUser user = await channel.GetUserAsync(reaction.UserId) as SocketGuildUser;
        if (user.IsBot)
            return;

        // selfroles check
        IGuild guild = (channel as ITextChannel)?.Guild;
        DbConfigSelfRoles selfRoles = await DbConfigSelfRoles.GetById(guild.Id);
        if (reaction.MessageId != selfRoles.Message || !selfRoles.SelfRoles.ContainsKey(reaction.Emote.ToString()))
            return;

        ulong roleId = selfRoles.SelfRoles[reaction.Emote.ToString()];
        if (addedReaction)
            await user.AddRoleAsync(roleId);
        else
            await user.RemoveRoleAsync(roleId);
    }

    private async Task Client_ButtonExecuted(SocketMessageComponent component)
    {
        if (component.Message.Author.Id != client.CurrentUser.Id) // don't wanna interfere with other bots' stuff
            return;

        string[] split = component.Data.CustomId.Split('-');
        switch (split[0])
        {
            case "lbnext":
                ulong executorId = Convert.ToUInt64(split[1]);
                if (component.User.Id != executorId)
                {
                    await component.RespondAsync("Action not permitted: You did not execute the original command.", ephemeral: true);
                    return;
                }

                await LeaderboardInteractions.GetNext(component, executorId, split[2], int.Parse(split[3]), int.Parse(split[4]), int.Parse(split[5]), bool.Parse(split[6]));
                break;
            case "trivia":
                await TriviaInteractions.Respond(component, split[2], bool.Parse(split[3]));
                break;
        }
    }

    private static async Task Client_GuildMemberUpdated(Cacheable<SocketGuildUser, ulong> userBeforeCached,
        SocketGuildUser userAfter)
    {
        SocketGuildUser userBefore = await userBeforeCached.GetOrDownloadAsync();
        if (userBefore.Nickname == userAfter.Nickname)
            return;

        if (await FilterSystem.ContainsFilteredWord(userAfter.Guild, userAfter.Nickname))
            await userAfter.ModifyAsync(properties => properties.Nickname = userAfter.Username);
    }

    private static Task Client_Log(LogMessage arg)
    {
        Console.WriteLine(arg);
        return Task.CompletedTask;
    }

    private async Task Client_MessageReceived(SocketMessage msg)
    {
        SocketUserMessage userMsg = msg as SocketUserMessage;
        SocketCommandContext context = new(client, userMsg);
        if (context.User.IsBot)
            return;

        await FilterSystem.DoInviteCheckAsync(userMsg, context.Guild, client);
        await FilterSystem.DoFilteredWordCheckAsync(userMsg, context.Guild, context.Channel);
        await FilterSystem.DoScamCheckAsync(userMsg, context.Guild);

        int argPos = 0;
        if (userMsg.HasCharPrefix('$', ref argPos))
        {
            Discord.Commands.SearchResult search = commands.Search(msg.Content[argPos..]);
            if (search.Error == CommandError.UnknownCommand)
                return;

            CommandInfo command = search.Commands[0].Command;
            if ((client.CurrentUser as IGuildUser)?.GetPermissions(context.Channel as IGuildChannel).Has(ChannelPermission.SendMessages) == false
                && command.Module.Name != "Moderation")
            {
                return;
            }

            DbGlobalConfig globalConfig = await DbGlobalConfig.Get();
            if (globalConfig.BannedUsers.Contains(context.User.Id))
            {
                await context.User.NotifyAsync(context.Channel, "You are banned from using the bot!");
                return;
            }
            if (globalConfig.DisabledCommands.Contains(command.Name))
            {
                await context.User.NotifyAsync(context.Channel, "This command is temporarily disabled!");
                return;
            }

            await commands.ExecuteAsync(context, argPos, serviceProvider);
        }
        else
        {
            DbUser user = await DbUser.GetById(context.Guild.Id, context.User.Id);

            if (user.TimeTillCash == 0)
            {
                user.TimeTillCash = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.MESSAGE_CASH_COOLDOWN);
            }
            else if (user.TimeTillCash <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                await user.SetCash(context.User, user.Cash + Constants.MESSAGE_CASH);
                user.TimeTillCash = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.MESSAGE_CASH_COOLDOWN);
            }
        }
    }

    private async Task Client_MessageUpdated(Cacheable<IMessage, ulong> msgBeforeCached, SocketMessage msgAfter, ISocketMessageChannel channel)
    {
        SocketUserMessage userMsgAfter = msgAfter as SocketUserMessage;
        SocketGuild guild = (userMsgAfter.Author as SocketGuildUser)?.Guild;
        await FilterSystem.DoInviteCheckAsync(userMsgAfter, guild, client);
        await FilterSystem.DoFilteredWordCheckAsync(userMsgAfter, guild, channel);
        await FilterSystem.DoScamCheckAsync(userMsgAfter, guild);
    }

    private static async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> msg, Cacheable<IMessageChannel, ulong> channel,
        SocketReaction reaction) => await HandleReactionAsync(channel, reaction, true);

    private static async Task Client_ReactionRemoved(Cacheable<IUserMessage, ulong> msg, Cacheable<IMessageChannel, ulong> channel,
        SocketReaction reaction) => await HandleReactionAsync(channel, reaction, false);

    private async Task Client_Ready()
    {
        // reset usingSlots if someone happened to be using slots during bot restart
        foreach (SocketGuild guild in client.Guilds)
        {
            QuerySnapshot usingQuery = await Program.database.Collection($"servers/{guild.Id}/users")
                .WhereEqualTo("UsingSlots", true).GetSnapshotAsync();
            foreach (DocumentSnapshot user in usingQuery.Documents)
                await user.Reference.SetAsync(new { usingSlots = FieldValue.Delete }, SetOptions.MergeAll);
        }

        await new MonitorSystem(client, Program.database).Initialise();
        await lavaSocketClient.StartAsync(client);
        lavaSocketClient.OnPlayerUpdated += audioSystem.OnPlayerUpdated;
        lavaSocketClient.OnTrackFinished += audioSystem.OnTrackFinished;
    }

    private static async Task Client_ThreadCreated(SocketThreadChannel thread)
    {
        await thread.JoinAsync();
        if (await FilterSystem.ContainsFilteredWord(thread.Guild, thread.Name))
            await thread.DeleteAsync();
    }

    private static async Task Client_ThreadUpdated(Cacheable<SocketThreadChannel, ulong> threadBefore, SocketThreadChannel threadAfter)
    {
        if (await FilterSystem.ContainsFilteredWord(threadAfter.Guild, threadAfter.Name))
            await threadAfter.DeleteAsync();
    }

    private static async Task Client_UserJoined(SocketGuildUser user)
    {
        // circumvent mute bypasses
        DbConfigRoles roles = await DbConfigRoles.GetById(user.Guild.Id);
        if (user.Guild.Roles.Any(role => role.Id == roles.MutedRole))
        {
            QuerySnapshot mutes = await Program.database.Collection($"servers/{user.Guild.Id}/mutes").GetSnapshotAsync();
            if (mutes.Any(doc => doc.Id == user.Id.ToString()))
            {
                DbMute mute = await DbMute.GetById(user.Guild.Id, user.Id);
                if (mute.Time >= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    await user.AddRoleAsync(roles.MutedRole);
            }
        }
    }

    private static async Task Commands_CommandExecuted(Optional<CommandInfo> command, ICommandContext context, IResult result)
    {
        string reason = RRFormat.Sanitize(result.ErrorReason);
        if (await FilterSystem.ContainsFilteredWord(context.Guild as SocketGuild, reason))
            return;

        string response = result.Error switch {
            CommandError.BadArgCount => $"You must specify {command.Value.Parameters.Count(p => !p.IsOptional)} argument(s)!\nCommand usage: ``{command.Value.Remarks}``",
            CommandError.ParseFailed => $"Couldn't understand something you passed into the command.\nThis error info might help: ``{reason}``\nOr maybe the command usage will: ``{command.Value.Remarks}``",
            CommandError.ObjectNotFound or CommandError.UnmetPrecondition or (CommandError)9 => reason,
            _ => ""
        };

        if (response != "" || (!result.IsSuccess && result is CommandResult))
            await (context.User as SocketUser).NotifyAsync(context.Channel as ISocketMessageChannel, reason);
        else if (!result.IsSuccess)
            Console.WriteLine(reason);
    }
}