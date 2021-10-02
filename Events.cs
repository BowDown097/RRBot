using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
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
                Embed embed = message.Embeds.ElementAt(0) as Embed;
                if (embed.Title == "Trivia!")
                {
                    using StringReader reader = new(embed.Description);
                    for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
                    {
                        // determine correct answer by using zero-width space lol
                        if (line.Contains("​"))
                        {
                            Emoji numberEmoji = new(Constants.POLL_EMOTES[Convert.ToInt32(line[0].ToString())]);
                            if (reaction.Emote.ToString() == numberEmoji.ToString())
                            {
                                EmbedBuilder embedBuilder = new()
                                {
                                    Color = Color.Red,
                                    Title = "Trivia Over!",
                                    Description = $"**{reaction.User}** was the first to get the correct answer!"
                                };
                                await message.ModifyAsync(msg => msg.Embed = embedBuilder.Build());
                            }
                        }
                    }
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

        public static async Task Client_GuildMemberUpdated(SocketGuildUser userBefore, SocketGuildUser userAfter)
        {
            if (userBefore.Nickname == userAfter.Nickname)
                return;

            char[] cleaned = userAfter.Nickname.Where(c => char.IsLetterOrDigit(c) || char.IsSymbol(c) || char.IsPunctuation(c)).ToArray();
            if (Filters.NWORD_REGEX.Matches(new string(cleaned).ToLower()).Count != 0)
                await userAfter.ModifyAsync(properties => properties.Nickname = userAfter.Username);
        }

        public static Task Client_Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        public async Task Client_MessageReceived(SocketMessage msg)
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
                DbGlobalConfig globalConfig = await DbGlobalConfig.Get();
                if (globalConfig.BannedUsers.Contains(context.User.Id))
                {
                    await context.Channel.SendMessageAsync($"{context.User.Mention}, you are banned from using the bot!");
                    return;
                }
                foreach (string cmd in globalConfig.DisabledCommands)
                {
                    if (context.Message.Content.StartsWith($"${cmd}", StringComparison.OrdinalIgnoreCase))
                    {
                        await context.Channel.SendMessageAsync($"{context.User.Mention}, this command is temporarily disabled!");
                        return;
                    }
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
                    await user.SetCash(context.User, context.Channel, user.Cash + Constants.MESSAGE_CASH);
                    user.TimeTillCash = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.MESSAGE_CASH_COOLDOWN);
                }

                await user.Write();
            }
        }

        public async Task Client_MessageUpdated(Cacheable<IMessage, ulong> msgBeforeCached, SocketMessage msgAfter, ISocketMessageChannel channel)
        {
            if (!msgBeforeCached.HasValue || (msgAfter.Author as IGuildUser)?.GuildPermissions.Has(GuildPermission.Administrator) == true)
                return;

            SocketUserMessage userMsgAfter = msgAfter as SocketUserMessage;
            await Filters.DoInviteCheckAsync(userMsgAfter, client);
            await Filters.DoNWordCheckAsync(userMsgAfter, channel);
            await Filters.DoScamCheckAsync(userMsgAfter);
        }

        public static async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel,
            SocketReaction reaction) => await HandleReactionAsync(msg, channel, reaction, true);

        public static async Task Client_ReactionRemoved(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel,
            SocketReaction reaction) => await HandleReactionAsync(msg, channel, reaction, false);

        public async Task Client_Ready()
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

        public static async Task Client_UserJoined(SocketGuildUser user)
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

        public static async Task Commands_CommandExecuted(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            switch (result.Error)
            {
                case CommandError.BadArgCount:
                    await (context.User as SocketUser).NotifyAsync(context.Channel as ISocketMessageChannel,
                        $"You must specify {command.Value.Parameters.Count(p => !p.IsOptional)} argument(s)!\nCommand usage: ``{command.Value.Remarks}``");
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
                    if (result is CommandResult rwm && !string.IsNullOrWhiteSpace(rwm.Reason))
                        await (context.User as SocketUser).NotifyAsync(context.Channel as ISocketMessageChannel, rwm.Reason);
                    break;
            }

            if (!result.IsSuccess)
            {
                if (result.ErrorReason == "User not found.")
                {
                    await (context.User as SocketUser).NotifyAsync(context.Channel as ISocketMessageChannel,
                        "Couldn't resolve a user from your input!");
                }

                Console.WriteLine(result.ErrorReason);
            }
        }
    }
}