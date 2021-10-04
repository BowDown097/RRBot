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
            client.GuildMemberUpdated += Client_GuildMemberUpdated;
            client.Log += Client_Log;
            client.MessageReceived += Client_MessageReceived;
            client.MessageUpdated += Client_MessageUpdated;
            client.ReactionAdded += Client_ReactionAdded;
            client.ReactionRemoved += Client_ReactionRemoved;
            client.Ready += Client_Ready;
            client.UserJoined += Client_UserJoined;
            commands.CommandExecuted += Commands_CommandExecuted;

            client.ChannelCreated += Logger.Client_ChannelCreated;
            client.ChannelDestroyed += Logger.Client_ChannelDestroyed;
            client.ChannelUpdated += Logger.Client_ChannelUpdated;
            client.InviteCreated += Logger.Client_InviteCreated;
            client.MessageDeleted += Logger.Client_MessageDeleted;
            client.MessageUpdated += Logger.Client_MessageUpdated;
            client.RoleCreated += Logger.Client_RoleCreated;
            client.RoleDeleted += Logger.Client_RoleDeleted;
            client.UserBanned += Logger.Client_UserBanned;
            client.UserJoined += Logger.Client_UserJoined;
            client.UserLeft += Logger.Client_UserLeft;
            client.UserUnbanned += Logger.Client_UserUnbanned;
            client.UserVoiceStateUpdated += Logger.Client_UserVoiceStateUpdated;
        }

        private static async Task HandleReactionAsync(Cacheable<IUserMessage, ulong> msg,
            ISocketMessageChannel channel, SocketReaction reaction, bool addedReaction)
        {
            SocketGuildUser user = await channel.GetUserAsync(reaction.UserId) as SocketGuildUser;
            if (user.IsBot)
                return;

            IUserMessage message = await msg.GetOrDownloadAsync();
            // trivia check
            if (message.Embeds.Count > 0 && addedReaction)
            {
                Embed embed = message.Embeds.FirstOrDefault() as Embed;
                if (embed.Title == "Trivia!")
                {
                    if (!Constants.POLL_EMOTES.Any(kvp => kvp.Value == reaction.Emote.ToString()))
                    {
                        await message.RemoveReactionAsync(reaction.Emote, user);
                        return;
                    }

                    using StringReader reader = new(embed.Description);
                    for (string line = await reader.ReadLineAsync(); line != null; line = await reader.ReadLineAsync())
                    {
                        if (!line.Contains("​")) // determine correct answer by using zero-width space lol
                            continue;

                        Emoji numberEmoji = new(Constants.POLL_EMOTES[Convert.ToInt32(line[0].ToString())]);
                        if (reaction.Emote.ToString() == numberEmoji.ToString())
                        {
                            EmbedBuilder embedBuilder = new()
                            {
                                Color = Color.Red,
                                Title = "Trivia Over!",
                                Description = $"**{reaction.User}** was the first to get the correct answer of \"{line[3..]}\"!\n~~{embed.Description}~~"
                            };
                            await message.ModifyAsync(msg => msg.Embed = embedBuilder.Build());
                        }
                    }

                    return;
                }
            }

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

        private static async Task Client_GuildMemberUpdated(SocketGuildUser userBefore, SocketGuildUser userAfter)
        {
            if (userBefore.Nickname == userAfter.Nickname)
                return;

            char[] cleaned = userAfter.Nickname.Where(c => char.IsLetterOrDigit(c) || char.IsSymbol(c) || char.IsPunctuation(c)).ToArray();
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

                    DbGlobalConfig globalConfig = await DbGlobalConfig.Get();
                    if (globalConfig.BannedUsers.Contains(context.User.Id))
                    {
                        await context.User.NotifyAsync(context.Channel, "You are banned from using the bot!");
                        return;
                    }
                    if (globalConfig.DisabledCommands.Contains(search.Commands[0].Command.Name))
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
            if (!msgBeforeCached.HasValue || (msgAfter.Author as IGuildUser)?.GuildPermissions.Has(GuildPermission.Administrator) == true)
                return;

            SocketUserMessage userMsgAfter = msgAfter as SocketUserMessage;
            await Filters.DoInviteCheckAsync(userMsgAfter, client);
            await Filters.DoNWordCheckAsync(userMsgAfter, channel);
            await Filters.DoScamCheckAsync(userMsgAfter);
        }

        private static async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel,
            SocketReaction reaction) => await HandleReactionAsync(msg, channel, reaction, true);

        private static async Task Client_ReactionRemoved(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel,
            SocketReaction reaction) => await HandleReactionAsync(msg, channel, reaction, false);

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