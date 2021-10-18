using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Microsoft.Extensions.DependencyInjection;
using RRBot.Systems;
using RRBot.TypeReaders;
using System.Reflection;
using System.Threading.Tasks;
using Victoria;

namespace RRBot
{
    internal static class Program
    {
        public static FirestoreDb database = FirestoreDb.Create("rushrebornbot",
            new FirestoreClientBuilder { CredentialsPath = Credentials.CREDENTIALS_PATH }.Build());

        private static async Task Main()
        {
            DiscordSocketClient client = new(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                GatewayIntents = GatewayIntents.All,
                LargeThreshold = 250,
                MessageCacheSize = 1500
            });

            ServiceProvider serviceProvider = new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton<CommandService>()
                .AddSingleton<LavaRestClient>()
                .AddSingleton<LavaSocketClient>()
                .AddSingleton<AudioSystem>()
                .BuildServiceProvider();

            new Events(serviceProvider).Initialize();
            CommandService commands = serviceProvider.GetRequiredService<CommandService>();
            commands.AddTypeReader(typeof(double), new DoubleTypeReader());
            commands.AddTypeReader(typeof(IEmote), new EmoteTypeReader());
            commands.AddTypeReader(typeof(IGuildUser), new RRGuildUserTypeReader());
            commands.AddTypeReader(typeof(SocketGuildUser), new RRGuildUserTypeReader());
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);
            await client.LoginAsync(TokenType.Bot, Credentials.TOKEN);
            await client.SetGameAsync(Constants.ACTIVITY, type: Constants.ACTIVITY_TYPE);
            await client.StartAsync();
            await Task.Delay(-1);
        }
    }
}
