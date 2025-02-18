using Discord.Interactions;

namespace RRBot;
internal sealed class DiscordClientHost(DiscordShardedClient client, CommandService commands,
    InteractionService interactions, IServiceProvider serviceProvider) : IHostedService
{
    private static bool _clientReady;
    private static readonly string[] SensitiveCommandErrorCharacters = ["_", "`", "~", ">"];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        client.ButtonExecuted += Client_ButtonExecuted;
        client.JoinedGuild += Client_JoinedGuild;
        client.Log += Client_Log;
        client.MessageReceived += Client_MessageReceived;
        client.MessageUpdated += Client_MessageUpdated;
        client.ReactionAdded += async (_, _, reaction) => await HandleReactionAsync(reaction, true);
        client.ReactionRemoved += async (_, _, reaction) => await HandleReactionAsync(reaction, false);
        client.ShardReady += Client_ShardReady;
        commands.CommandExecuted += Commands_CommandExecuted;
        
        client.AutoModRuleCreated += LoggingSystem.Client_AutoModRuleCreated;
        client.AutoModRuleDeleted += LoggingSystem.Client_AutoModRuleDeleted;
        client.AutoModRuleUpdated += LoggingSystem.Client_AutoModRuleUpdated;
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

        await client.LoginAsync(TokenType.Bot, Credentials.Instance.Token);
        await client.SetGameAsync(Constants.Activity, type: Constants.ActivityType);
        await client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken) => await client.StopAsync();
    
    private async Task Client_ButtonExecuted(SocketMessageComponent interaction)
    {
        if (interaction.Message.Author.Id != client.CurrentUser.Id) return;
        ShardedInteractionContext<SocketMessageComponent> context = new(client, interaction);
        await interactions.ExecuteCommandAsync(context, serviceProvider);
    }
    
    private static async Task Client_JoinedGuild(SocketGuild guild)
    {
        SocketTextChannel hopefullyGeneral = Array.Find(guild.TextChannels.ToArray(), c => c.Name == "general") ?? guild.DefaultChannel;
        await hopefullyGeneral.SendMessageAsync("""
        Thank you for inviting me to your server!
        You're gonna want to check out $modules. Use $module to view the commands in each module, and $help to see how to use a command.
        The Config module will probably be the most important to look at as an admin or server owner.
        There's a LOT to look at, so it's probably gonna take some time to get everything set up, but trust me, it's worth it.
        Have fun!
        """);
    }
    
    private static Task Client_Log(LogMessage msg)
    {
        Console.WriteLine(msg);
        return Task.CompletedTask;
    }
    
    private async Task Client_MessageReceived(SocketMessage msg)
    {
        if (msg is not SocketUserMessage userMsg || msg.Author.IsBot || string.IsNullOrWhiteSpace(msg.Content))
            return;

        ShardedCommandContext context = new(client, userMsg);
        await FilterSystem.DoInviteCheckAsync(msg, context.Guild, client);
        await FilterSystem.DoScamCheckAsync(msg, context.Guild);

        int argPos = 0;
        DbUser user = await MongoManager.FetchUserAsync(context.User.Id, context.Guild.Id);
        if (userMsg.HasStringPrefix(Constants.Prefix, ref argPos))
        {
            SearchResult search = commands.Search(msg.Content[argPos..]);
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

            Discord.Commands.IResult result = await commands.ExecuteAsync(context, argPos, serviceProvider);
            if (result is not Discord.Commands.ExecuteResult executeResult)
                return;
            
            if (executeResult.Exception is HttpException httpEx)
                await ExceptionHandler.HandleHttpException(httpEx, context);
            else if (executeResult.Exception is not null)
                Console.WriteLine(executeResult.Exception);
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
        if (string.IsNullOrWhiteSpace(msgAfter.Content) || msgAfter.Author is not SocketGuildUser user)
            return;
        
        await FilterSystem.DoInviteCheckAsync(msgAfter, user.Guild, client);
        await FilterSystem.DoScamCheckAsync(msgAfter, user.Guild);
    }

    private async Task Client_ShardReady(DiscordSocketClient _)
    {
        if (_clientReady) return;
        _clientReady = true;

        commands.AddTypeReader<decimal>(new DecimalTypeReader());
        commands.AddTypeReader<IEmote>(new EmoteTypeReader());
        commands.AddTypeReader<IGuildUser>(new RrGuildUserTypeReader());
        commands.AddTypeReader<IUser>(new RrUserTypeReader());
        commands.AddTypeReader<List<ulong>>(new ListTypeReader<ulong>());
        commands.AddTypeReader<string>(new SanitizedStringTypeReader());

        await commands.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);
        await interactions.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);

        await MongoManager.InitializeAsync(Credentials.Instance.MongoConnectionString);
        await MongoManager.Users.UpdateManyAsync(u => u.UsingSlots, Builders<DbUser>.Update.Set(u => u.UsingSlots, false));

        await new MonitorSystem(client).Initialize();
    }
    
    private static async Task Commands_CommandExecuted(Discord.Optional<CommandInfo> commandOpt,
        ICommandContext context, Discord.Commands.IResult result)
    {
        string reason = StringCleaner.Sanitize(result.ErrorReason, SensitiveCommandErrorCharacters);
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

        DbConfigSelfRoles selfRoles = await MongoManager.FetchConfigAsync<DbConfigSelfRoles>(user.Guild.Id);
        string emote = reaction.Emote.ToString()!;
        if (reaction.MessageId != selfRoles.Message || !selfRoles.SelfRoles.TryGetValue(emote, out ulong value))
            return;

        if (addedReaction)
            await user.AddRoleAsync(value);
        else
            await user.RemoveRoleAsync(value);
    }
}