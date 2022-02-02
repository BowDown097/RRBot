using Discord.Interactions;

namespace RRBot;
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
            .AddSingleton<InteractionService>()
            .AddSingleton<InteractiveService>()
            .AddSingleton<IDiscordClientWrapper, DiscordClientWrapper>()
            .AddSingleton<IAudioService, LavalinkNode>()
            .AddSingleton(new LavalinkNodeOptions
            {
                RestUri = "http://localhost:2333/",
                WebSocketUri = "ws://localhost:2333/",
                Password = "youshallnotpass",
            })
            .AddSingleton<InactivityTrackingService>()
            .AddSingleton(new InactivityTrackingOptions
            {
                DisconnectDelay = TimeSpan.Zero,
                PollInterval = TimeSpan.FromSeconds(Constants.POLL_INTERVAL_SECS),
                TrackInactivity = true
            })
            .AddSingleton<AudioSystem>()
            .BuildServiceProvider();

        CommandService commands = serviceProvider.GetRequiredService<CommandService>();
        commands.AddTypeReader<double>(new DoubleTypeReader());
        commands.AddTypeReader<IEmote>(new EmoteTypeReader());
        commands.AddTypeReader<IGuildUser>(new RRGuildUserTypeReader());
        commands.AddTypeReader<SocketGuildUser>(new RRGuildUserTypeReader());
        commands.AddTypeReader<string>(new SanitizedStringTypeReader());
        await commands.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);
        InteractionService interactions = serviceProvider.GetRequiredService<InteractionService>();
        await interactions.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);
        new EventSystem(serviceProvider).SubscribeEvents();

        await client.LoginAsync(TokenType.Bot, Credentials.TOKEN);
        await client.SetGameAsync(Constants.ACTIVITY, type: Constants.ACTIVITY_TYPE);
        await client.StartAsync();
        await Task.Delay(-1);
    }
}