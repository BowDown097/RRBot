// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault
using Discord.Interactions;
using RRBot;

Credentials.ValidationResult validation = Credentials.Instance.Validate();
switch (validation)
{
    case Credentials.ValidationResult.MissingCredentialsFile:
        Console.WriteLine("The credentials.json file is missing.");
        return;
    case Credentials.ValidationResult.NeedMongoConnectionString:
        Console.WriteLine("A MongoDB connection string was not supplied in the credentials.json.");
        return;
    case Credentials.ValidationResult.NeedToken:
        Console.WriteLine("A token was not supplied in the credentials.json.");
        return;
}

DiscordShardedClient client = new(new DiscordSocketConfig
{
    AlwaysDownloadUsers = true,
    GatewayIntents = GatewayIntents.All,
    LargeThreshold = 250,
    MessageCacheSize = 1500
});

HostApplicationBuilder builder = new(args);
builder.Services.AddSingleton(client)
    .AddSingleton<CommandService>()
    .AddSingleton<InteractionService>()
    .AddSingleton<InteractiveService>()
    .AddHostedService<DiscordClientHost>()
    .AddLavalink()
    .AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Warning))
    .AddLyrics()
    .ConfigureLyrics(options => options.SuppressExceptions = true)
    .AddInactivityTracking()
    .ConfigureInactivityTracking(options => options.DefaultTimeout = TimeSpan.FromSeconds(Constants.InactivityTimeoutSecs))
    .AddSingleton<AudioSystem>();

builder.Build().Run();