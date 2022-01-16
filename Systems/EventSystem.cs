#pragma warning disable RCS1163, IDE0060 // both warnings fire for events, which they shouldn't
using Discord.Interactions;

namespace RRBot.Systems;
public class EventSystem
{
    private readonly IAudioService audioService;
    private readonly AudioSystem audioSystem;
    private readonly CommandService commands;
    private readonly DiscordSocketClient client;
    private readonly InactivityTrackingService inactivityTracking;
    private readonly InteractionService interactions;
    private readonly ServiceProvider serviceProvider;

    public EventSystem(ServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        audioService = serviceProvider.GetRequiredService<IAudioService>();
        audioSystem = serviceProvider.GetRequiredService<AudioSystem>();
        commands = serviceProvider.GetRequiredService<CommandService>();
        client = serviceProvider.GetRequiredService<DiscordSocketClient>();
        inactivityTracking = serviceProvider.GetRequiredService<InactivityTrackingService>();
        interactions = serviceProvider.GetRequiredService<InteractionService>();
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
        DbConfigSelfRoles selfRoles = await DbConfigSelfRoles.GetById(channel.GetGuild().Id);
        if (reaction.MessageId != selfRoles.Message || !selfRoles.SelfRoles.ContainsKey(reaction.Emote.ToString()))
            return;

        ulong roleId = selfRoles.SelfRoles[reaction.Emote.ToString()];
        if (addedReaction)
            await user.AddRoleAsync(roleId);
        else
            await user.RemoveRoleAsync(roleId);
    }

    private async Task Client_ButtonExecuted(SocketMessageComponent interaction)
    {
        if (interaction.Message.Author.Id != client.CurrentUser.Id) // don't wanna interfere with other bots' stuff
            return;

        SocketInteractionContext<SocketMessageComponent> context = new(client, interaction);
        await interactions.ExecuteCommandAsync(context, serviceProvider);
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

    private static async Task Client_Log(LogMessage msg)
    {
        if (msg.Exception != null)
        {
            Console.WriteLine($"{msg.Exception.Message}\n{msg.Exception.StackTrace}");
            if (msg.Exception is CommandException ex)
                await ex.HandleDiscordErrors();
        }
        else
        {
            Console.WriteLine(msg);
        }
    }

    private async Task Client_MessageReceived(SocketMessage msg)
    {
        SocketUserMessage userMsg = msg as SocketUserMessage;
        SocketCommandContext context = new(client, userMsg);
        if (context.User.IsBot || string.IsNullOrWhiteSpace(userMsg.Content))
            return;

        await FilterSystem.DoInviteCheckAsync(userMsg, context.Guild, client);
        await FilterSystem.DoFilteredWordCheckAsync(userMsg, context.Guild);
        await FilterSystem.DoScamCheckAsync(userMsg, context.Guild);

        int argPos = 0;
        if (userMsg.HasStringPrefix(Constants.PREFIX, ref argPos))
        {
            Discord.Commands.SearchResult search = commands.Search(msg.Content[argPos..]);
            if (search.Error == CommandError.UnknownCommand)
                return;

            DbConfigChannels channelsConfig = await DbConfigChannels.GetById(context.Guild.Id);
            CommandInfo command = search.Commands[0].Command;
            if (!(command.Module.Name is "Administration" or "BotOwner" or "Moderation" or "Music")
                && channelsConfig.WhitelistedChannels.Count > 0 && !channelsConfig.WhitelistedChannels.Contains(context.Channel.Id))
            {
                await context.User.NotifyAsync(context.Channel, "Commands are disabled in this channel!");
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
        await FilterSystem.DoInviteCheckAsync(userMsgAfter, userMsgAfter.Author.GetGuild(), client);
        await FilterSystem.DoFilteredWordCheckAsync(userMsgAfter, userMsgAfter.Author.GetGuild());
        await FilterSystem.DoScamCheckAsync(userMsgAfter, userMsgAfter.Author.GetGuild());
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
        await audioService.InitializeAsync();
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

    private static async Task Commands_CommandExecuted(Optional<CommandInfo> command, ICommandContext context, Discord.Commands.IResult result)
    {
        string reason = Format.Sanitize(result.ErrorReason);
        if (await FilterSystem.ContainsFilteredWord(context.Guild, reason))
            return;

        string response = result.Error switch {
            CommandError.BadArgCount => $"You must specify {command.Value.Parameters.Count(p => !p.IsOptional)} argument(s)!\nCommand usage: ``{command.Value.Remarks}``",
            CommandError.ParseFailed => $"Couldn't understand something you passed into the command.\nThis error info might help: ``{reason}``\nOr maybe the command usage will: ``{command.Value.Remarks}``",
            CommandError.ObjectNotFound or CommandError.UnmetPrecondition or (CommandError)9 => reason,
            _ => !result.IsSuccess && result is CommandResult ? reason : ""
        };

        if (response != "")
            await context.User.NotifyAsync(context.Channel, response);
        else if (!result.IsSuccess)
            Console.WriteLine(reason);
    }
}