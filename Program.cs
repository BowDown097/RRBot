using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Microsoft.Extensions.DependencyInjection;
using RRBot.Extensions;
using RRBot.Systems;
using RRBot.TypeReaders;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private AudioSystem audioSystem;
        private CommandService commands;
        private CultureInfo currencyCulture;
        private DiscordSocketClient client;
        private IServiceProvider serviceProvider;
        private LavaRestClient lavaRestClient;
        private LavaSocketClient lavaSocketClient;
        private List<ulong> bannedUsers;

        public async Task RunBotAsync()
        {
            // services setup
            audioSystem = new AudioSystem(lavaRestClient, lavaSocketClient);
            bannedUsers = new List<ulong>();
            client = new DiscordSocketClient(new DiscordSocketConfig { AlwaysDownloadUsers = true, ExclusiveBulkDelete = true, MessageCacheSize = 100 });
            commands = new CommandService();
            currencyCulture = CultureInfo.CreateSpecificCulture("en-US");
            currencyCulture.NumberFormat.CurrencyNegativePattern = 2;
            lavaRestClient = new LavaRestClient("127.0.0.1", 2333, "youshallnotpass");
            lavaSocketClient = new LavaSocketClient();

            serviceProvider = new ServiceCollection()
                .AddSingleton(audioSystem)
                .AddSingleton(bannedUsers)
                .AddSingleton(client)
                .AddSingleton(commands)
                .AddSingleton(currencyCulture)
                .AddSingleton(lavaRestClient)
                .AddSingleton(lavaSocketClient)
                .BuildServiceProvider();

            // general events
            client.Log += Client_Log;
            client.MessageReceived += Client_MessageReceived;
            client.ReactionAdded += Client_ReactionAdded;
            client.ReactionRemoved += Client_ReactionRemoved;
            client.Ready += Client_Ready;
            client.UserJoined += Client_UserJoined;
            commands.CommandExecuted += Commands_CommandExecuted;

            // logger events
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

            // client setup
            commands.AddTypeReader(typeof(double), new DoubleTypeReader());
            commands.AddTypeReader(typeof(IEmote), new EmoteTypeReader());
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);
            await client.LoginAsync(TokenType.Bot, Credentials.TOKEN);
            await client.SetGameAsync("Pokimane fart compilations", type: ActivityType.Watching);
            await client.StartAsync();
            await Task.Delay(-1);
        }

        private static async Task HandleReactionAsync(ISocketMessageChannel channel, SocketReaction reaction, bool addedReaction)
        {
            SocketGuildUser user = await channel.GetUserAsync(reaction.UserId) as SocketGuildUser;
            if (user.IsBot)
                return;

            IGuild guild = (channel as ITextChannel)?.Guild;
            DocumentReference doc = database.Collection($"servers/{guild.Id}/config").Document("selfroles");
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

        private Task Client_Log(LogMessage arg)
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

            int argPos = 0;
            if (userMsg.HasCharPrefix('$', ref argPos))
            {
                if (bannedUsers.Contains(context.User.Id))
                {
                    await context.Channel.SendMessageAsync($"{context.User.Mention}, you are banned from using the bot!");
                    return;
                }

                await commands.ExecuteAsync(context, argPos, serviceProvider);
            }
            else
            {
                await CashSystem.TryMessageReward(context);
            }

            if ((context.User as IGuildUser)?.GuildPermissions.Has(GuildPermission.Administrator) == false)
            {
                await Filters.DoInviteCheckAsync(context);
                await Filters.DoNWordCheckAsync(context);
                await Filters.DoScamCheckAsync(context);
            }
        }

        private async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel,
            SocketReaction reaction) => await HandleReactionAsync(channel, reaction, true);

        private async Task Client_ReactionRemoved(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel,
            SocketReaction reaction) => await HandleReactionAsync(channel, reaction, false);

        private async Task Client_Ready()
        {
            QuerySnapshot globalConfig = await database.Collection("globalConfig").GetSnapshotAsync();
            foreach (DocumentSnapshot blacklist in globalConfig.Where(doc => doc.ContainsField("banned")))
                bannedUsers.Add(ulong.Parse(blacklist.Id));

            await new Monitors(client, database).Initialise();
            await lavaSocketClient.StartAsync(client);
            lavaSocketClient.OnPlayerUpdated += audioSystem.OnPlayerUpdated;
            lavaSocketClient.OnTrackFinished += audioSystem.OnTrackFinished;
        }

        private async Task Client_UserJoined(SocketGuildUser user)
        {
            // add 100 cash to user if they haven't joined already
            DocumentReference userDoc = database.Collection($"servers/{user.Guild.Id}/users").Document(user.Id.ToString());
            DocumentSnapshot userSnap = await userDoc.GetSnapshotAsync();
            if (!userSnap.TryGetValue<double>("cash", out _))
                await CashSystem.SetCash(user, null, 100);

            // circumvent mute bypasses
            DocumentReference rolesDoc = database.Collection($"servers/{user.Guild.Id}/config").Document("roles");
            DocumentSnapshot rolesSnap = await rolesDoc.GetSnapshotAsync();
            if (rolesSnap.TryGetValue("mutedRole", out ulong mutedId))
            {
                QuerySnapshot mutes = await database.Collection($"servers/{user.Guild.Id}/mutes").GetSnapshotAsync();
                foreach (DocumentSnapshot mute in mutes.Documents.Where(doc => doc.Id == user.Id.ToString()))
                {
                    long timestamp = mute.GetValue<long>("Time");
                    if (timestamp >= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                        await user.AddRoleAsync(mutedId);
                }
            }
        }

        private async Task Commands_CommandExecuted(Optional<CommandInfo> command, ICommandContext context, IResult result)
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
