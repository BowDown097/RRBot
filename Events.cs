using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using RRBot.Entities;
using RRBot.Extensions;
using RRBot.Interactions;
using RRBot.Systems;
using Victoria;

namespace RRBot
{
    public class Events
    {
        private readonly AudioSystem audioSystem;
        private readonly CommandService commands;
        private readonly DiscordSocketClient client;
        private readonly LavaSocketClient lavaSocketClient;
        private readonly ServiceProvider serviceProvider;

        public Events(ServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            audioSystem = serviceProvider.GetRequiredService<AudioSystem>();
            commands = serviceProvider.GetRequiredService<CommandService>();
            client = serviceProvider.GetRequiredService<DiscordSocketClient>();
            lavaSocketClient = serviceProvider.GetRequiredService<LavaSocketClient>();
        }

        public void Initialize()
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

            client.ChannelCreated += Logger.Client_ChannelCreated;
            client.ChannelDestroyed += Logger.Client_ChannelDestroyed;
            client.ChannelUpdated += Logger.Client_ChannelUpdated;
            client.GuildMemberUpdated += Logger.Client_GuildMemberUpdated;
            client.GuildStickerCreated += Logger.Client_GuildStickerCreated;
            client.GuildStickerDeleted += Logger.Client_GuildStickerDeleted;
            client.GuildUpdated += Logger.Client_GuildUpdated;
            client.InviteCreated += Logger.Client_InviteCreated;
            client.InviteDeleted += Logger.Client_InviteDeleted;
            client.MessageDeleted += Logger.Client_MessageDeleted;
            client.MessageUpdated += Logger.Client_MessageUpdated;
            client.ReactionAdded += Logger.Client_ReactionAdded;
            client.ReactionRemoved += Logger.Client_ReactionRemoved;
            client.RoleCreated += Logger.Client_RoleCreated;
            client.RoleDeleted += Logger.Client_RoleDeleted;
            client.RoleUpdated += Logger.Client_RoleUpdated;
            client.SpeakerAdded += Logger.Client_SpeakerAdded;
            client.SpeakerRemoved += Logger.Client_SpeakerRemoved;
            client.StageEnded += Logger.Client_StageEnded;
            client.StageStarted += Logger.Client_StageStarted;
            client.StageUpdated += Logger.Client_StageUpdated;
            client.ThreadCreated += Logger.Client_ThreadCreated;
            client.ThreadDeleted += Logger.Client_ThreadDeleted;
            client.ThreadMemberJoined += Logger.Client_ThreadMemberJoined;
            client.ThreadMemberLeft += Logger.Client_ThreadMemberLeft;
            client.ThreadUpdated += Logger.Client_ThreadUpdated;
            client.UserBanned += Logger.Client_UserBanned;
            client.UserJoined += Logger.Client_UserJoined;
            client.UserLeft += Logger.Client_UserLeft;
            client.UserUnbanned += Logger.Client_UserUnbanned;
            client.UserVoiceStateUpdated += Logger.Client_UserVoiceStateUpdated;
        }

        private static async Task HandleReactionAsync(Cacheable<IMessageChannel, ulong> channelCached, SocketReaction reaction, bool addedReaction)
        {
            IMessageChannel channel = await channelCached.GetOrDownloadAsync();
            SocketGuildUser user = await channel.GetUserAsync(reaction.UserId) as SocketGuildUser;
            if (user.IsBot)
                return;

            // selfroles check
            IGuild guild = (channel as ITextChannel)?.Guild;
            DocumentReference doc = Program.database.Collection($"servers/{guild.Id}/config").Document("selfroles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("message", out ulong msgId) && snap.TryGetValue(reaction.Emote.ToString(), out ulong roleId))
            {
                if (reaction.MessageId != msgId)
                    return;

                if (addedReaction)
                    await user.AddRoleAsync(roleId);
                else
                    await user.RemoveRoleAsync(roleId);
            }
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

            char[] cleaned = userAfter.Nickname
                .Where(c => char.IsLetterOrDigit(c) || Filters.NWORD_SPCHARS.Contains(c))
                .Distinct()
                .ToArray();
            if (Filters.NWORD_REGEX.Matches(new string(cleaned).ToLower()).Count != 0)
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

            if ((context.User as IGuildUser)?.GuildPermissions.Has(GuildPermission.Administrator) == false)
            {
                await Filters.DoInviteCheckAsync(userMsg, client);
                await Filters.DoNWordCheckAsync(userMsg, context.Channel);
                await Filters.DoScamCheckAsync(userMsg);
            }

            int argPos = 0;
            if (userMsg.HasCharPrefix('$', ref argPos))
            {
                try
                {
                    SearchResult search = commands.Search(msg.Content[argPos..]);
                    if (!search.IsSuccess)
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
                catch (RpcException rpcE)
                {
                    await context.User.NotifyAsync(context.Channel, "I cannot connect to the database at the moment. Try again later.");
                    Console.WriteLine(rpcE.Message + "\n" + rpcE.StackTrace);
                }
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

                await user.Write();
            }
        }

        private async Task Client_MessageUpdated(Cacheable<IMessage, ulong> msgBeforeCached, SocketMessage msgAfter, ISocketMessageChannel channel)
        {
            if ((msgAfter.Author as IGuildUser)?.GuildPermissions.Has(GuildPermission.Administrator) == true)
                return;

            SocketUserMessage userMsgAfter = msgAfter as SocketUserMessage;
            await Filters.DoInviteCheckAsync(userMsgAfter, client);
            await Filters.DoNWordCheckAsync(userMsgAfter, channel);
            await Filters.DoScamCheckAsync(userMsgAfter);
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
                    .WhereEqualTo("usingSlots", true).GetSnapshotAsync();
                foreach (DocumentSnapshot user in usingQuery.Documents)
                    await user.Reference.SetAsync(new { usingSlots = FieldValue.Delete }, SetOptions.MergeAll);
            }

            await new Monitors(client, Program.database).Initialise();
            await lavaSocketClient.StartAsync(client);
            lavaSocketClient.OnPlayerUpdated += audioSystem.OnPlayerUpdated;
            lavaSocketClient.OnTrackFinished += audioSystem.OnTrackFinished;
        }

        private static async Task Client_ThreadCreated(SocketThreadChannel thread)
        {
            await thread.JoinAsync();
            char[] cleaned = thread.Name
                .Where(c => char.IsLetterOrDigit(c) || Filters.NWORD_SPCHARS.Contains(c))
                .Distinct()
                .ToArray();
            if (Filters.NWORD_REGEX.Matches(new string(cleaned).ToLower()).Count != 0)
                await thread.DeleteAsync();
        }

        private static async Task Client_ThreadUpdated(Cacheable<SocketThreadChannel, ulong> threadBefore, SocketThreadChannel threadAfter)
        {
            char[] cleaned = threadAfter.Name
                .Where(c => char.IsLetterOrDigit(c) || Filters.NWORD_SPCHARS.Contains(c))
                .Distinct()
                .ToArray();
            if (Filters.NWORD_REGEX.Matches(new string(cleaned).ToLower()).Count != 0)
                await threadAfter.DeleteAsync();
        }

        private static async Task Client_UserJoined(SocketGuildUser user)
        {
            // circumvent mute bypasses
            DbConfigRoles roles = await DbConfigRoles.GetById(user.Guild.Id);
            if (user.Guild.Roles.Any(role => role.Id == roles.MutedRole))
            {
                QuerySnapshot mutes = await Program.database.Collection($"servers/{user.Guild.Id}/mutes").GetSnapshotAsync();
                foreach (DocumentSnapshot mute in mutes.Documents.Where(doc => doc.Id == user.Id.ToString()))
                {
                    long timestamp = mute.GetValue<long>("Time");
                    if (timestamp >= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                        await user.AddRoleAsync(roles.MutedRole);
                }
            }
        }

        private static async Task Commands_CommandExecuted(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            switch (result.Error)
            {
                case CommandError.BadArgCount:
                    await (context.User as SocketUser).NotifyAsync(context.Channel as ISocketMessageChannel,
                        $"You must specify {command.Value.Parameters.Count(p => !p.IsOptional)} argument(s)!\nCommand usage: ``{command.Value.Remarks}``");
                    break;
                case CommandError.ObjectNotFound:
                    await (context.User as SocketUser).NotifyAsync(context.Channel as ISocketMessageChannel,
                        "Couldn't resolve a user from your input!");
                    break;
                case CommandError.ParseFailed:
                    await (context.User as SocketUser).NotifyAsync(context.Channel as ISocketMessageChannel,
                        $"Couldn't understand something you passed into the command.\nThis error info might help: ``{result.ErrorReason}``" +
                        $"\nOr maybe the command usage will: ``{command.Value.Remarks}``");
                    break;
                case CommandError.UnmetPrecondition:
                    await (context.User as SocketUser).NotifyAsync(context.Channel as ISocketMessageChannel, result.ErrorReason);
                    break;
                case CommandError.Unsuccessful:
                    if (result.ErrorReason.StartsWith("Your user input") || result.ErrorReason.StartsWith("You have no"))
                        await (context.User as SocketUser).NotifyAsync(context.Channel as ISocketMessageChannel, result.ErrorReason);
                    if (result is CommandResult rwm && !string.IsNullOrWhiteSpace(rwm.Reason))
                        await (context.User as SocketUser).NotifyAsync(context.Channel as ISocketMessageChannel, rwm.Reason);
                    break;
            }

            if (!result.IsSuccess)
                Console.WriteLine(result.ErrorReason);
        }
    }
}