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
        _client.JoinedGuild += Client_JoinedGuild;
        _client.Log += Client_Log;
        _client.MessageReceived += Client_MessageReceived;
        _client.MessageUpdated += Client_MessageUpdated;
        _client.ReactionAdded += async (_, _, reaction) => await HandleReactionAsync(reaction, true);
        _client.ReactionRemoved += async (_, _, reaction) => await HandleReactionAsync(reaction, false);
        _client.Ready += Client_Ready;
        _commands.CommandExecuted += Commands_CommandExecuted;

        _client.AutoModRuleCreated += LoggingSystem.Client_AutoModRuleCreated;
        _client.AutoModRuleDeleted += LoggingSystem.Client_AutoModRuleDeleted;
        _client.AutoModRuleUpdated += LoggingSystem.Client_AutoModRuleUpdated;
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

    private async Task Client_ButtonExecuted(SocketMessageComponent interaction)
    {
        if (interaction.Message.Author.Id != _client.CurrentUser.Id) // don't wanna interfere with other bots' stuff
            return;

        SocketInteractionContext<SocketMessageComponent> context = new(_client, interaction);
        await _interactions.ExecuteCommandAsync(context, _serviceProvider);
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
        if (msg is not SocketUserMessage userMsg || msg.Author.IsBot || string.IsNullOrWhiteSpace(userMsg.Content))
            return;

        SocketCommandContext context = new(_client, userMsg);
        await FilterSystem.DoInviteCheckAsync(userMsg, context.Guild, _client);
        await FilterSystem.DoScamCheckAsync(userMsg, context.Guild);

        int argPos = 0;
        DbUser user = await MongoManager.FetchUserAsync(context.User.Id, context.Guild.Id);
        if (userMsg.HasStringPrefix(Constants.Prefix, ref argPos))
        {
            SearchResult search = _commands.Search(msg.Content[argPos..]);
            if (search.Error == CommandError.UnknownCommand)
                return;
            
            DbConfigChannels channels = await MongoManager.FetchConfigAsync<DbConfigChannels>(context.Guild.Id);
            CommandInfo command = search.Commands[0].Command;
            if (command.Module.Name is not ("Administration" or "BotOwner" or "Config" or "Moderation" or "Music" or "Polls")
                && channels.WhitelistedChannels.Count > 0 && !channels.WhitelistedChannels.Contains(context.Channel.Id))
            {
                await context.User.NotifyAsync(context.Channel, "Commands are disabled in this channel!");
                return;
            }

            DbGlobalConfig globalConfig = await MongoManager.FetchGlobalConfigAsync();
            DbConfigMisc misc = await MongoManager.FetchConfigAsync<DbConfigMisc>(context.Guild.Id);
            if (globalConfig.BannedUsers.Contains(context.User.Id))
            {
                await context.User.NotifyAsync(context.Channel, "You are banned from using the bot!");
                return;
            }
            if (globalConfig.DisabledCommands.Contains(command.Name) || misc.DisabledCommands.Contains(command.Name))
            {
                await context.User.NotifyAsync(context.Channel, "This command is disabled!");
                return;
            }
            if (misc.DisabledModules.Contains(command.Module.Name, StringComparer.OrdinalIgnoreCase))
            {
                await context.User.NotifyAsync(context.Channel, "The module for this command is disabled!");
                return;
            }

            if (user.UsingSlots)
            {
                await context.User.NotifyAsync(context.Channel, "You appear to be currently using the slot machine. To be safe, you cannot run any command until it is finished.");
                return;
            }

            await _commands.ExecuteAsync(context, argPos, _serviceProvider);
        }
        else
        {
            if (user.TimeTillCash == 0)
            {
                user.TimeTillCash = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.MessageCashCooldown);
            }
            else if (user.TimeTillCash <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                decimal messageCash = Constants.MessageCash * (1 + 0.20m * user.Prestige);
                await user.SetCash(context.User, user.Cash + messageCash);

                DbConfigMisc misc = await MongoManager.FetchConfigAsync<DbConfigMisc>(context.Guild.Id);
                if (!misc.DropsDisabled && RandomUtil.Next(70) == 1)
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
        if (msgAfter is not SocketUserMessage userMsgAfter || string.IsNullOrWhiteSpace(userMsgAfter.Content))
            return;

        await FilterSystem.DoInviteCheckAsync(userMsgAfter, userMsgAfter.Author.GetGuild(), _client);
        await FilterSystem.DoScamCheckAsync(userMsgAfter, userMsgAfter.Author.GetGuild());
    }

    private async Task Client_Ready()
    {
        // reset usingSlots if someone happened to be using slots during bot restart
        await MongoManager.Users.UpdateManyAsync(u => u.UsingSlots,
            Builders<DbUser>.Update.Set(u => u.UsingSlots, false));

        await new MonitorSystem(_client).Initialise();
        await _audioService.InitializeAsync();
        _inactivityTracking.BeginTracking();
    }

    private static async Task Commands_CommandExecuted(Discord.Optional<CommandInfo> commandOpt,
        ICommandContext context, Discord.Commands.IResult result)
    {
        string reason = StringCleaner.Sanitize(result.ErrorReason, new[] { "_", "`", "~", ">" });
        CommandInfo command = commandOpt.GetValueOrDefault();
        string args = command.Parameters.Any(p => p.IsOptional)
            ? $"{command.Parameters.Count(p => !p.IsOptional)}-{command.Parameters.Count}"
            : command.Parameters.Count.ToString();
        string response = result.Error switch {
            CommandError.BadArgCount => $"You must specify {args} argument(s)!\nCommand usage: ``{command.GetUsage()}``",
            CommandError.ParseFailed => $"Couldn't understand something you passed into the command.\nThis error info might help: ``{reason}``\nOr maybe the command usage will: ``{command.GetUsage()}``",
            CommandError.ObjectNotFound or CommandError.UnmetPrecondition or (CommandError)9 => reason,
            _ => !result.IsSuccess && result is CommandResult ? reason : ""
        };

        if (response != "")
            await context.User.NotifyAsync(context.Channel, response);
        else if (!result.IsSuccess)
            Console.WriteLine(reason);
    }
    
    private static async Task HandleReactionAsync(SocketReaction reaction, bool addedReaction)
    {
        if (reaction.User.GetValueOrDefault() is not SocketGuildUser user || user.IsBot)
            return;

        // selfroles check
        DbConfigSelfRoles selfRoles = await MongoManager.FetchConfigAsync<DbConfigSelfRoles>(user.Guild.Id);
        string emote = reaction.Emote.ToString();
        if (string.IsNullOrEmpty(emote) || reaction.MessageId != selfRoles.Message || !selfRoles.SelfRoles.ContainsKey(emote))
            return;

        ulong roleId = selfRoles.SelfRoles[emote];
        if (addedReaction)
            await user.AddRoleAsync(roleId);
        else
            await user.RemoveRoleAsync(roleId);
    }
}