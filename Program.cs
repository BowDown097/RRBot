using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Microsoft.Extensions.DependencyInjection;
using RRBot.Services;
using RRBot.Systems;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Victoria;

namespace RRBot
{
    internal class Program
    {
        private static void Main() => new Program().RunBotAsync().GetAwaiter().GetResult();

        public static FirestoreDb database = FirestoreDb.Create("rushrebornbot", new FirestoreClientBuilder { CredentialsPath = Credentials.CREDENTIALS_PATH }.Build());
        public static Logger logger;
        private CommandService commands;
        private DiscordSocketClient client;
        private IServiceProvider serviceProvider;
        private LavaRestClient lavaRestClient;
        private LavaSocketClient lavaSocketClient;

        public async Task StartBanCheckAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
                foreach (SocketGuild guild in client.Guilds)
                {
                    CollectionReference bans = database.Collection($"servers/{guild.Id}/bans");
                    foreach (DocumentReference banDoc in bans.ListDocumentsAsync().ToEnumerable())
                    {
                        DocumentSnapshot snapshot = await banDoc.GetSnapshotAsync();
                        long timestamp = snapshot.GetValue<long>("Time");
                        ulong userId = Convert.ToUInt64(banDoc.Id);

                        if (!(await guild.GetBansAsync()).Any(ban => ban.User.Id == userId))
                        {
                            await banDoc.DeleteAsync();
                            continue;
                        }

                        if (timestamp <= Global.UnixTime())
                        {
                            await guild.RemoveBanAsync(userId);
                            await banDoc.DeleteAsync();
                        }
                    }
                }
            }
        }

        public async Task StartMuteCheckAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
                foreach (SocketGuild guild in client.Guilds)
                {
                    DocumentReference doc = database.Collection($"servers/{guild.Id}/config").Document("roles");
                    DocumentSnapshot snap = await doc.GetSnapshotAsync();
                    if (snap.TryGetValue("mutedRole", out ulong mutedId))
                    {
                        CollectionReference mutes = database.Collection($"servers/{guild.Id}/mutes");
                        foreach (DocumentReference muteDoc in mutes.ListDocumentsAsync().ToEnumerable())
                        {
                            DocumentSnapshot snapshot = await muteDoc.GetSnapshotAsync();
                            long timestamp = snapshot.GetValue<long>("Time");
                            SocketGuildUser user = guild.GetUser(Convert.ToUInt64(muteDoc.Id));

                            if (timestamp <= Global.UnixTime())
                            {
                                if (user != null) await user.RemoveRoleAsync(mutedId);
                                await muteDoc.DeleteAsync();
                            }
                        }
                    }
                }
            }
        }

        public async Task RunBotAsync()
        {
            // services setup
            client = new DiscordSocketClient(new DiscordSocketConfig { AlwaysDownloadUsers = true, ExclusiveBulkDelete = true, MessageCacheSize = 100 });
            lavaRestClient = new LavaRestClient("127.0.0.1", 2333, "youshallnotpass");
            lavaSocketClient = new LavaSocketClient();
            commands = new CommandService();
            serviceProvider = new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton(commands)
                .AddSingleton(lavaRestClient)
                .AddSingleton(lavaSocketClient)
                .AddSingleton<AudioService>()
                .BuildServiceProvider();

            // general events
            client.JoinedGuild += Client_JoinedGuild;
            client.Log += Client_Log;
            client.ReactionAdded += Client_ReactionAdded;
            client.ReactionRemoved += Client_ReactionRemoved;
            client.Ready += Client_Ready;
            client.UserJoined += async (SocketGuildUser user) => await CashSystem.SetCash(user, 10);
            commands.CommandExecuted += Commands_CommandExecuted;

            // logger events
            logger = new Logger(client);
            client.ChannelCreated += logger.Client_ChannelCreated;
            client.ChannelDestroyed += logger.Client_ChannelDestroyed;
            client.ChannelUpdated += logger.Client_ChannelUpdated;
            client.InviteCreated += logger.Client_InviteCreated;
            client.MessageDeleted += logger.Client_MessageDeleted;
            client.MessageUpdated += logger.Client_MessageUpdated;
            client.RoleCreated += logger.Client_RoleCreated;
            client.RoleDeleted += logger.Client_RoleDeleted;
            client.UserBanned += logger.Client_UserBanned;
            client.UserJoined += logger.Client_UserJoined;
            client.UserLeft += logger.Client_UserLeft;
            client.UserUnbanned += logger.Client_UserUnbanned;
            client.UserVoiceStateUpdated += logger.Client_UserVoiceStateUpdated;

            // client setup
            commands.AddTypeReader(typeof(IEmote), new EmoteTypeReader());
            await RegisterCommandsAsync();
            await client.LoginAsync(TokenType.Bot, Credentials.TOKEN);
            await client.SetGameAsync("with your father");
            await client.StartAsync();
            await Task.Delay(-1);
        }

        private async Task Client_Ready()
        {
            Global.RunInBackground(async () => await StartBanCheckAsync());
            Global.RunInBackground(async () => await StartMuteCheckAsync());
            await lavaSocketClient.StartAsync(client);
            lavaSocketClient.OnPlayerUpdated += serviceProvider.GetService<AudioService>().OnPlayerUpdated;
            lavaSocketClient.OnTrackFinished += serviceProvider.GetService<AudioService>().OnTrackFinished;
        }

        private Task Client_Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        private async Task Client_JoinedGuild(SocketGuild guild)
        {
            await guild.DefaultChannel.SendMessageAsync("Thank you for inviting me to your server! Make sure you take a look at ``$help`` and ``$modules Config`` to get started.");
        }

        private async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> msgCached, ISocketMessageChannel channel, SocketReaction reaction)
        {
            SocketGuildUser user = await channel.GetUserAsync(reaction.UserId) as SocketGuildUser;
            if (user.IsBot) return;

            IGuild guild = (channel as ITextChannel).Guild;
            DocumentReference doc = database.Collection($"servers/{guild.Id}/config").Document("selfroles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("message", out ulong msgId) && snap.TryGetValue(reaction.Emote.ToString(), out ulong roleId))
            {
                if (reaction.MessageId != msgId) return;
                await user.AddRoleAsync(roleId);
            }
        }

        private async Task Client_ReactionRemoved(Cacheable<IUserMessage, ulong> msgCached, ISocketMessageChannel channel, SocketReaction reaction)
        {
            SocketGuildUser user = await channel.GetUserAsync(reaction.UserId) as SocketGuildUser;
            if (user.IsBot) return;

            IGuild guild = (channel as ITextChannel).Guild;
            DocumentReference doc = database.Collection($"servers/{guild.Id}/config").Document("selfroles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("message", out ulong msgId) && snap.TryGetValue(reaction.Emote.ToString(), out ulong roleId))
            {
                if (reaction.MessageId != msgId) return;
                await user.RemoveRoleAsync(roleId);
            }
        }

        private async Task Commands_CommandExecuted(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            switch (result)
            {
                case CommandResult rwm:
                    if (rwm.Error == CommandError.Unsuccessful) await context.Channel.SendMessageAsync(rwm.Reason);
                    if (rwm.Error == CommandError.BadArgCount) 
                        await context.Channel.SendMessageAsync($"{context.User.Mention}, you must specify {command.Value.Parameters.Count(p => !p.IsOptional)} (or more) argument(s)!");
                    break;
                default:
                    if (!result.IsSuccess) Console.WriteLine(result.ErrorReason);
                    if (result.Error == CommandError.UnmetPrecondition) await context.Channel.SendMessageAsync(result.ErrorReason);
                    if (result.Error == CommandError.BadArgCount)
                        await context.Channel.SendMessageAsync($"{context.User.Mention}, you must specify {command.Value.Parameters.Count(p => !p.IsOptional)} (or more) argument(s)!");
                    break;
            }
        }

        private async Task RegisterCommandsAsync()
        {
            client.MessageReceived += HandleCommandAsync;
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            SocketUserMessage message = arg as SocketUserMessage;
            SocketCommandContext context = new SocketCommandContext(client, message);
            if (context.User.IsBot) return;

            // no good very bad word check
            if (arg.Channel.Name != "extremely-funny" && Global.niggerRegex.Matches(new string(message.Content.Where(char.IsLetter).ToArray()).ToLower()).Count != 0)
            {
                Global.RunInBackground(async () => 
                {
                    await Task.Delay(TimeSpan.FromSeconds(10));
                    await message.DeleteAsync();
                });
            }

            // command handler
            DocumentReference doc = database.Collection("globalConfig").Document(context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (!snap.TryGetValue("banned", out bool banned))
            {
                int argPos = 0;
                if (message.HasCharPrefix('$', ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos))
                    await commands.ExecuteAsync(context, argPos, serviceProvider);
                else
                    await CashSystem.TryMessageReward(context);
            }
            else
            {
                await context.Channel.SendMessageAsync($"{context.User.Mention}, you are banned from using the bot!");
            }
        }
    }
}