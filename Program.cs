using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Microsoft.Extensions.DependencyInjection;
using RRBot.Systems;
using RRBot.TypeReaders;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Victoria;

namespace RRBot
{
    internal class Program
    {
        private static Task Main() => new Program().RunBotAsync();

        public static FirestoreDb database = FirestoreDb.Create("rushrebornbot",
            new FirestoreClientBuilder { CredentialsPath = Credentials.CREDENTIALS_PATH }.Build());
        private CommandService commands;
        private DiscordSocketClient client;
        private ServiceProvider serviceProvider;

        public async Task RunBotAsync()
        {
            client = new(new() { AlwaysDownloadUsers = true, ExclusiveBulkDelete = true, MessageCacheSize = 100 });
            CultureInfo currencyCulture = CultureInfo.CreateSpecificCulture("en-US");
            currencyCulture.NumberFormat.CurrencyNegativePattern = 2;
            serviceProvider = new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton<CommandService>()
                .AddSingleton(new LavaRestClient("127.0.0.1", 2333, "youshallnotpass"))
                .AddSingleton<LavaSocketClient>()
                .AddSingleton<AudioSystem>()
                .AddSingleton(currencyCulture)
                .BuildServiceProvider();
            commands = serviceProvider.GetRequiredService<CommandService>();

            Events events = new(serviceProvider);
            client.GuildMemberUpdated += Events.Client_GuildMemberUpdated;
            client.Log += Events.Client_Log;
            client.MessageReceived += events.Client_MessageReceived;
            client.MessageUpdated += events.Client_MessageUpdated;
            client.ReactionAdded += Events.Client_ReactionAdded;
            client.ReactionRemoved += Events.Client_ReactionRemoved;
            client.Ready += events.Client_Ready;
            client.UserJoined += Events.Client_UserJoined;
            commands.CommandExecuted += Events.Commands_CommandExecuted;

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

            commands.AddTypeReader(typeof(double), new DoubleTypeReader());
            commands.AddTypeReader(typeof(IEmote), new EmoteTypeReader());
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);
            await client.LoginAsync(TokenType.Bot, Credentials.TOKEN);
            await client.SetGameAsync(Constants.ACTIVITY, type: Constants.ACTIVITY_TYPE);
            await client.StartAsync();
            await Task.Delay(-1);
        }
    }
}
