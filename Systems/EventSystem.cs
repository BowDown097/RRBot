#pragma warning disable RCS1163, IDE0060 // both warnings fire for events, which they shouldn't
using Discord.Interactions;

namespace RRBot.Systems;
public class EventSystem
{
    private readonly IAudioService _audioService;
    private readonly CommandService _commands;
    private readonly DiscordSocketClient _client;
    private readonly InactivityTrackingService _inactivityTracking;
    private readonly InteractionService _interactions;
    private readonly ServiceProvider _serviceProvider;

    public EventSystem(ServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _audioService = serviceProvider.GetRequiredService<IAudioService>();
        _commands = serviceProvider.GetRequiredService<CommandService>();
        _client = serviceProvider.GetRequiredService<DiscordSocketClient>();
        _inactivityTracking = serviceProvider.GetRequiredService<InactivityTrackingService>();
        _interactions = serviceProvider.GetRequiredService<InteractionService>();
    }

    public void SubscribeEvents()
    {
        _client.ButtonExecuted += Client_ButtonExecuted;
        _client.GuildMemberUpdated += Client_GuildMemberUpdated;
        _client.JoinedGuild += Client_JoinedGuild;
        _client.Log += Client_Log;
        _client.MessageReceived += Client_MessageReceived;
        _client.MessageUpdated += Client_MessageUpdated;
        _client.ReactionAdded += Client_ReactionAdded;
        _client.ReactionRemoved += Client_ReactionRemoved;
        _client.Ready += Client_Ready;
        _client.ThreadCreated += Client_ThreadCreated;
        _client.ThreadUpdated += Client_ThreadUpdated;
        _client.UserJoined += Client_UserJoined;
        _commands.CommandExecuted += Commands_CommandExecuted;

        _client.ChannelCreated += LoggingSystem.Client_ChannelCreated;
        _client.ChannelDestroyed += LoggingSystem.Client_ChannelDestroyed;
        _client.ChannelUpdated += LoggingSystem.Client_ChannelUpdated;
        _client.GuildMemberUpdated += LoggingSystem.Client_GuildMemberUpdated;
        _client.GuildScheduledEventCancelled += LoggingSystem.Client_GuildScheduledEventCancelled;
        _client.GuildScheduledEventCompleted += LoggingSystem.Client_GuildScheduledEventCompleted;
        _client.GuildScheduledEventCreated += LoggingSystem.Client_GuildScheduledEventCreated;
        _client.GuildScheduledEventStarted += LoggingSystem.Client_GuildScheduledEventStarted;
        _client.GuildScheduledEventUpdated += LoggingSystem.Client_GuildScheduledEventUpdated;
        _client.GuildScheduledEventUserAdd += LoggingSystem.Client_GuildScheduledEventUserAdd;
        _client.GuildScheduledEventUserRemove += LoggingSystem.Client_GuildScheduledEventUserRemove;
        _client.GuildStickerCreated += LoggingSystem.Client_GuildStickerCreated;
        _client.GuildStickerDeleted += LoggingSystem.Client_GuildStickerDeleted;
        _client.GuildUpdated += LoggingSystem.Client_GuildUpdated;
        _client.InviteCreated += LoggingSystem.Client_InviteCreated;
        _client.InviteDeleted += LoggingSystem.Client_InviteDeleted;
        _client.MessageDeleted += LoggingSystem.Client_MessageDeleted;
        _client.MessageUpdated += LoggingSystem.Client_MessageUpdated;
        _client.ReactionAdded += LoggingSystem.Client_ReactionAdded;
        _client.ReactionRemoved += LoggingSystem.Client_ReactionRemoved;
        _client.RoleCreated += LoggingSystem.Client_RoleCreated;
        _client.RoleDeleted += LoggingSystem.Client_RoleDeleted;
        _client.RoleUpdated += LoggingSystem.Client_RoleUpdated;
        _client.SpeakerAdded += LoggingSystem.Client_SpeakerAdded;
        _client.SpeakerRemoved += LoggingSystem.Client_SpeakerRemoved;
        _client.StageEnded += LoggingSystem.Client_StageEnded;
        _client.StageStarted += LoggingSystem.Client_StageStarted;
        _client.StageUpdated += LoggingSystem.Client_StageUpdated;
        _client.ThreadCreated += LoggingSystem.Client_ThreadCreated;
        _client.ThreadDeleted += LoggingSystem.Client_ThreadDeleted;
        _client.ThreadMemberJoined += LoggingSystem.Client_ThreadMemberJoined;
        _client.ThreadMemberLeft += LoggingSystem.Client_ThreadMemberLeft;
        _client.ThreadUpdated += LoggingSystem.Client_ThreadUpdated;
        _client.UserBanned += LoggingSystem.Client_UserBanned;
        _client.UserJoined += LoggingSystem.Client_UserJoined;
        _client.UserLeft += LoggingSystem.Client_UserLeft;
        _client.UserUnbanned += LoggingSystem.Client_UserUnbanned;
        _client.UserVoiceStateUpdated += LoggingSystem.Client_UserVoiceStateUpdated;
    }

    private static async Task HandleReactionAsync(Cacheable<IMessageChannel, ulong> channelCached, SocketReaction reaction, bool addedReaction)
    {
        IMessageChannel channel = await channelCached.GetOrDownloadAsync();
        if (await channel.GetUserAsync(reaction.UserId) is not SocketGuildUser user || user.IsBot)
            return;

        // selfroles check
        DbConfig config = await MongoManager.FetchConfigAsync(channel.GetGuild().Id);
        string emote = reaction.Emote.ToString() ?? string.Empty;
        if (reaction.MessageId != config.SelfRoles.Message || !config.SelfRoles.SelfRoles.ContainsKey(emote))
            return;

        ulong roleId = config.SelfRoles.SelfRoles[emote];
        if (addedReaction)
            await user.AddRoleAsync(roleId);
        else
            await user.RemoveRoleAsync(roleId);
    }

    private async Task Client_ButtonExecuted(SocketMessageComponent interaction)
    {
        if (interaction.Message.Author.Id != _client.CurrentUser.Id) // don't wanna interfere with other bots' stuff
            return;

        SocketInteractionContext<SocketMessageComponent> context = new(_client, interaction);
        await _interactions.ExecuteCommandAsync(context, _serviceProvider);
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

    private static async Task Client_JoinedGuild(SocketGuild guild)
    {
        SocketTextChannel hopefullyGeneral = Array.Find(guild.TextChannels.ToArray(), c => c.Name == "general")
            ?? guild.DefaultChannel;
        await hopefullyGeneral.SendMessageAsync(@"Thank you for inviting me to your server!
        You're gonna want to check out $modules. Use $module to view the commands in each module, and $help to see how to use a command.
        The Config module will probably be the most important to look at as an admin or server owner.
        There's a LOT to look at, so it's probably gonna take some time to get everything set up, but trust me, it's worth it.
        Have fun!");
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
        SocketCommandContext context = new(_client, userMsg);
        if (context.User.IsBot || userMsg is null || string.IsNullOrWhiteSpace(userMsg.Content))
            return;

        await FilterSystem.DoInviteCheckAsync(userMsg, context.Guild, _client);
        await FilterSystem.DoFilteredWordCheckAsync(userMsg, context.Guild);
        await FilterSystem.DoScamCheckAsync(userMsg, context.Guild);

        int argPos = 0;
        if (userMsg.HasStringPrefix(Constants.Prefix, ref argPos))
        {
            Discord.Commands.SearchResult search = _commands.Search(msg.Content[argPos..]);
            if (search.Error == CommandError.UnknownCommand)
                return;
            
            DbConfig config = await MongoManager.FetchConfigAsync(context.Guild.Id);
            CommandInfo command = search.Commands[0].Command;
            if (command.Module.Name is not ("Administration" or "BotOwner" or "Moderation" or "Music" or "Polls")
                && config.Channels.WhitelistedChannels.Count > 0 && !config.Channels.WhitelistedChannels.Contains(context.Channel.Id))
            {
                await context.User.NotifyAsync(context.Channel, "Commands are disabled in this channel!");
                return;
            }

            DbGlobalConfig globalConfig = await MongoManager.FetchGlobalConfigAsync();
            if (globalConfig.BannedUsers.Contains(context.User.Id))
            {
                await context.User.NotifyAsync(context.Channel, "You are banned from using the bot!");
                return;
            }
            if (globalConfig.DisabledCommands.Contains(command.Name) || config.Miscellaneous.DisabledCommands.Contains(command.Name))
            {
                await context.User.NotifyAsync(context.Channel, "This command is disabled!");
                return;
            }
            if (config.Miscellaneous.DisabledModules.Contains(command.Module.Name, StringComparer.OrdinalIgnoreCase))
            {
                await context.User.NotifyAsync(context.Channel, "The module for this command is disabled!");
                return;
            }

            await _commands.ExecuteAsync(context, argPos, _serviceProvider);
        }
        else
        {
            DbUser user = await MongoManager.FetchUserAsync(context.User.Id, context.Guild.Id);

            if (user.TimeTillCash == 0)
            {
                user.TimeTillCash = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.MessageCashCooldown);
            }
            else if (user.TimeTillCash <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                decimal messageCash = Constants.MessageCash * (1 + 0.20m * user.Prestige);
                await user.SetCash(context.User, user.Cash + messageCash);
                DbConfig config = await MongoManager.FetchConfigAsync(context.Guild.Id);

                if (!config.Miscellaneous.DropsDisabled && RandomUtil.Next(70) == 1)
                    await ItemSystem.GiveCollectible("Bank Cheque", context.Channel, user);
                if (user.Cash >= 1000000 && !user.HasReachedAMilli)
                {
                    user.HasReachedAMilli = true;
                    await ItemSystem.GiveCollectible("V Card", context.Channel, user);
                }

                user.TimeTillCash = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.MessageCashCooldown);
            }

            await MongoManager.UpdateObjectAsync(user);
        }
    }

    private async Task Client_MessageUpdated(Cacheable<IMessage, ulong> msgBeforeCached, SocketMessage msgAfter, ISocketMessageChannel channel)
    {
        if (msgAfter is not SocketUserMessage userMsgAfter)
            return;

        await FilterSystem.DoInviteCheckAsync(userMsgAfter, userMsgAfter.Author.GetGuild(), _client);
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
        await MongoManager.Users.UpdateManyAsync(u => u.UsingSlots,
            Builders<DbUser>.Update.Set(u => u.UsingSlots, false));

        await new MonitorSystem(_client).Initialise();
        await _audioService.InitializeAsync();
        _inactivityTracking.BeginTracking();
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
        if (await FilterSystem.ContainsFilteredWord(user.Guild, user.Username))
            await user.KickAsync();
    }

    private static async Task Commands_CommandExecuted(Discord.Optional<CommandInfo> command,
        ICommandContext context, Discord.Commands.IResult result)
    {
        string reason = Format.Sanitize(result.ErrorReason).Replace("\\*", "*").Replace("\\.", ".").Replace("\\:", ":");
        if (await FilterSystem.ContainsFilteredWord(context.Guild, reason))
            return;

        string args = command.Value.Parameters.Any(p => p.IsOptional)
            ? $"{command.Value.Parameters.Count(p => !p.IsOptional)}-{command.Value.Parameters.Count}"
            : command.Value.Parameters.Count.ToString();
        string response = result.Error switch {
            CommandError.BadArgCount => $"You must specify {args} argument(s)!\nCommand usage: ``{command.Value.GetUsage()}``",
            CommandError.ParseFailed => $"Couldn't understand something you passed into the command.\nThis error info might help: ``{reason}``\nOr maybe the command usage will: ``{command.Value.GetUsage()}``",
            CommandError.ObjectNotFound or CommandError.UnmetPrecondition or (CommandError)9 => reason,
            _ => !result.IsSuccess && result is CommandResult ? reason : ""
        };

        if (response != "")
            await context.User.NotifyAsync(context.Channel, response);
        else if (!result.IsSuccess)
            Console.WriteLine(reason);
    }
}