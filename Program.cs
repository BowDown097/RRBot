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

            CommandService commands = serviceProvider.GetRequiredService<CommandService>();
            commands.AddTypeReader<double>(new DoubleTypeReader());
            commands.AddTypeReader<IEmote>(new EmoteTypeReader());
            commands.AddTypeReader<IGuildUser>(new RRGuildUserTypeReader());
            commands.AddTypeReader<SocketGuildUser>(new RRGuildUserTypeReader());
            commands.AddTypeReader<string>(new SanitizedStringTypeReader());
            new EventSystem(serviceProvider).SubscribeEvents();
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);
            await client.LoginAsync(TokenType.Bot, Credentials.TOKEN);
            await client.SetGameAsync(Constants.ACTIVITY, type: Constants.ACTIVITY_TYPE);
            await client.StartAsync();
            await Task.Delay(-1);
        }
    }
}
