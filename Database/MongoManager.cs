namespace RRBot.Database;
public class MongoManager
{
    private static MongoClient Client { get; set; } = null!;
    private static IMongoDatabase Database => Client.GetDatabase("RR");

    public static IMongoCollection<DbBan> Bans => Database.GetCollection<DbBan>("bans");
    public static IMongoCollection<DbChill> Chills => Database.GetCollection<DbChill>("chills");
    public static IMongoCollection<DbElection> Elections => Database.GetCollection<DbElection>("elections");
    public static IMongoCollection<DbGang> Gangs => Database.GetCollection<DbGang>("gangs");
    private static IMongoCollection<DbGlobalConfig> GlobalConfig => Database.GetCollection<DbGlobalConfig>("globalconfig");
    public static IMongoCollection<DbPot> Pots => Database.GetCollection<DbPot>("pots");
    public static IMongoCollection<DbUser> Users => Database.GetCollection<DbUser>("users");
    
    public static IMongoCollection<DbConfigChannels> ChannelConfigs 
        => Database.GetCollection<DbConfigChannels>("channelconfigs");
    public static IMongoCollection<DbConfigMisc> MiscConfigs
        => Database.GetCollection<DbConfigMisc>("miscconfigs");
    public static IMongoCollection<DbConfigRanks> RankConfigs
        => Database.GetCollection<DbConfigRanks>("rankconfigs");
    public static IMongoCollection<DbConfigRoles> RoleConfigs
        => Database.GetCollection<DbConfigRoles>("roleconfigs");
    public static IMongoCollection<DbConfigSelfRoles> SelfRoleConfigs
        => Database.GetCollection<DbConfigSelfRoles>("selfroleconfigs");

    public static async Task<DbBan> FetchBanAsync(ulong userId, ulong guildId)
    {
        IAsyncCursor<DbBan> cursor = await Bans.FindAsync(b => b.GuildId == guildId && b.UserId == userId);
        DbBan ban = await cursor.FirstOrDefaultAsync();
        if (ban is not null)
            return ban;

        DbBan newBan = new(guildId, userId);
        await Bans.InsertOneAsync(newBan);
        return newBan;
    }

    public static async Task<DbChill> FetchChillAsync(ulong channelId, ulong guildId)
    {
        IAsyncCursor<DbChill> cursor = await Chills.FindAsync(c => c.ChannelId == channelId && c.GuildId == guildId);
        DbChill chill = await cursor.FirstOrDefaultAsync();
        if (chill is not null)
            return chill;

        DbChill newChill = new(guildId, channelId);
        await Chills.InsertOneAsync(newChill);
        return newChill;
    }

    public static async Task<T> FetchConfigAsync<T>(ulong guildId) where T : DbConfig
    {
        string collection = typeof(T).GetCustomAttribute<BsonCollectionAttribute>(true)!.CollectionName;
        IAsyncCursor<T> cursor = await Database.GetCollection<T>(collection).FindAsync(c => c.GuildId == guildId);
        T config = await cursor.FirstOrDefaultAsync();
        if (config is not null)
            return config;

        // technically unsafe, but constructor should always exist
        T newConfig = (T)Activator.CreateInstance(typeof(T), [guildId])!;
        await Database.GetCollection<T>(collection).InsertOneAsync(newConfig);
        return newConfig;
    }

    public static async Task<DbElection> FetchElectionAsync(ulong guildId, int? electionId = null, bool makeNew = true)
    {
        if (electionId is null)
        {
            SortDefinition<DbElection> sort = Builders<DbElection>.Sort.Descending(e => e.ElectionId);
            IAsyncCursor<DbElection> sortCursor = await Elections.FindAsync(e => e.GuildId == guildId,
                new FindOptions<DbElection> { Sort = sort });
            DbElection highestIdElection = await sortCursor.FirstOrDefaultAsync();
            electionId = highestIdElection is not null ? highestIdElection.ElectionId + 1 : 1;
        }

        IAsyncCursor<DbElection> cursor = await Elections.FindAsync(e =>
            e.ElectionId == electionId && e.GuildId == guildId);
        DbElection election = await cursor.FirstOrDefaultAsync();
        if (!makeNew || election is not null)
            return election;

        DbElection newElection = new(guildId, electionId.GetValueOrDefault());
        await Elections.InsertOneAsync(newElection);
        return newElection;
    }

    public static async Task<DbGang> FetchGangAsync(string name, ulong guildId)
    {
        IAsyncCursor<DbGang> cursor = await Gangs.FindAsync(g => g.GuildId == guildId
            && g.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return await cursor.FirstOrDefaultAsync();
    }

    public static async Task<DbGlobalConfig> FetchGlobalConfigAsync()
    {
        DbGlobalConfig globalConfig = await GlobalConfig.Aggregate().FirstOrDefaultAsync();
        if (globalConfig is not null)
            return globalConfig;

        DbGlobalConfig newGlobalConfig = new();
        await GlobalConfig.InsertOneAsync(newGlobalConfig);
        return newGlobalConfig;
    }

    public static async Task<DbPot> FetchPotAsync(ulong guildId)
    {
        IAsyncCursor<DbPot> cursor = await Pots.FindAsync(p => p.GuildId == guildId);
        DbPot pot = await cursor.FirstOrDefaultAsync();
        if (pot is not null)
            return pot;

        DbPot newPot = new(guildId);
        await Pots.InsertOneAsync(newPot);
        return newPot;
    }

    public static async Task<DbUser> FetchUserAsync(ulong userId, ulong guildId)
    {
        IAsyncCursor<DbUser> cursor = await Users.FindAsync(u => u.UserId == userId && u.GuildId == guildId);
        DbUser user = await cursor.FirstOrDefaultAsync();
        if (user is not null) 
            return user;

        DbUser newUser = new(guildId, userId);
        await Users.InsertOneAsync(newUser);
        return newUser;
    }

    public static async Task InitializeAsync(string connectionString)
    {
        Console.WriteLine($"Connecting to MongoDB server. Connection string: {connectionString}");

        MongoClientSettings settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        Client = new MongoClient(connectionString);
        await Client.StartSessionAsync();

        Console.WriteLine("Successfully connected to MongoDB server.");
    }

    public static async Task DeleteObjectAsync<T>(T obj) where T : DbObject
    {
        string collection = obj.GetType().GetCustomAttribute<BsonCollectionAttribute>(true)!.CollectionName;
        await Database.GetCollection<T>(collection).DeleteOneAsync(o => o.Id == obj.Id);
    }

    public static async Task UpdateObjectAsync<T>(T obj) where T : DbObject
    {
        string collection = obj.GetType().GetCustomAttribute<BsonCollectionAttribute>(true)!.CollectionName;
        await Database.GetCollection<T>(collection).ReplaceOneAsync(o => o.Id == obj.Id, obj);
    }
}