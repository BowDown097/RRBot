using Discord.Interactions;

namespace RRBot;
internal static class Program
{

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
                Password = "hey stop reading this source code because you can easily guess this password and thats bad for security so making it as hard to guess as possible is a very good idea which is what im doing right now, without using the bee movie script just for filler because im too lazy to download it, put it into atom, and use regex to remove all newlines and then do some weird shit, uh i think this is enough, if its not someone make a pr and make it even longer and harder to guess please and thanks",
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
