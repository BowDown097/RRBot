namespace RRBot.Database;
public class MongoManager
{
    private static MongoClient Client { get; set; }
    private static IMongoDatabase Database => Client.GetDatabase("RR");

    public static IMongoCollection<DbBan> Bans => Database.GetCollection<DbBan>("bans");
    public static IMongoCollection<DbChill> Chills => Database.GetCollection<DbChill>("chills");
    public static IMongoCollection<DbConfig> Configs => Database.GetCollection<DbConfig>("configs");
    public static IMongoCollection<DbElection> Elections => Database.GetCollection<DbElection>("elections");
    public static IMongoCollection<DbGang> Gangs => Database.GetCollection<DbGang>("gangs");
    private static IMongoCollection<DbGlobalConfig> GlobalConfig => Database.GetCollection<DbGlobalConfig>("globalconfig");
    public static IMongoCollection<DbPot> Pots => Database.GetCollection<DbPot>("pots");
    public static IMongoCollection<DbUser> Users => Database.GetCollection<DbUser>("users");

    public static async Task<DbBan> FetchBanAsync(ulong userId, ulong guildId)
    {
        IAsyncCursor<DbBan> cursor = await Bans.FindAsync(b => b.GuildId == guildId && b.UserId == userId);
        DbBan ban = await cursor.FirstOrDefaultAsync();
        if (ban != null)
            return ban;

        DbBan newBan = new() { GuildId = guildId, UserId = userId };
        await Bans.InsertOneAsync(newBan);
        return newBan;
    }

    public static async Task<DbChill> FetchChillAsync(ulong channelId, ulong guildId)
    {
        IAsyncCursor<DbChill> cursor = await Chills.FindAsync(c => c.ChannelId == channelId && c.GuildId == guildId);
        DbChill chill = await cursor.FirstOrDefaultAsync();
        if (chill != null)
            return chill;

        DbChill newChill = new() { ChannelId = channelId, GuildId = guildId };
        await Chills.InsertOneAsync(newChill);
        return newChill;
    }

    public static async Task<DbConfig> FetchConfigAsync(ulong guildId)
    {
        IAsyncCursor<DbConfig> cursor = await Configs.FindAsync(c => c.GuildId == guildId);
        DbConfig config = await cursor.FirstOrDefaultAsync();
        if (config != null)
            return config;

        DbConfig newConfig = new() { GuildId = guildId };
        await Configs.InsertOneAsync(newConfig);
        return newConfig;
    }

    public static async Task<DbElection> FetchElectionAsync(ulong guildId, int? electionId = null, bool makeNew = true)
    {
        if (electionId == null)
        {
            SortDefinition<DbElection> sort = Builders<DbElection>.Sort.Descending(e => e.ElectionId);
            IAsyncCursor<DbElection> sortCursor = await Elections.FindAsync(e => e.GuildId == guildId,
                new FindOptions<DbElection> { Sort = sort });
            DbElection highestIdElection = await sortCursor.FirstOrDefaultAsync();
            electionId = highestIdElection != null ? highestIdElection.ElectionId + 1 : 1;
        }

        IAsyncCursor<DbElection> cursor = await Elections.FindAsync(e =>
            e.ElectionId == electionId && e.GuildId == guildId);
        DbElection election = await cursor.FirstOrDefaultAsync();
        if (!makeNew || election != null)
            return election;

        DbElection newElection = new() { ElectionId = electionId.GetValueOrDefault(), GuildId = guildId };
        await Elections.InsertOneAsync(newElection);
        return newElection;
    }

    public static async Task<DbGang> FetchGangAsync(string name, ulong guildId)
    {
        IAsyncCursor<DbGang> cursor = await Gangs.FindAsync(g =>
            g.GuildId == guildId && g.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return await cursor.FirstOrDefaultAsync();
    }

    public static async Task<DbGlobalConfig> FetchGlobalConfigAsync()
    {
        DbGlobalConfig globalConfig = await GlobalConfig.Aggregate().FirstOrDefaultAsync();
        if (globalConfig != null)
            return globalConfig;

        DbGlobalConfig newGlobalConfig = new();
        await GlobalConfig.InsertOneAsync(newGlobalConfig);
        return newGlobalConfig;
    }

    public static async Task<DbPot> FetchPotAsync(ulong guildId)
    {
        IAsyncCursor<DbPot> cursor = await Pots.FindAsync(p => p.GuildId == guildId);
        DbPot pot = await cursor.FirstOrDefaultAsync();
        if (pot != null)
            return pot;

        DbPot newPot = new() { GuildId = guildId };
        await Pots.InsertOneAsync(newPot);
        return newPot;
    }

    public static async Task<DbUser> FetchUserAsync(ulong userId, ulong guildId)
    {
        IAsyncCursor<DbUser> cursor = await Users.FindAsync(u => u.UserId == userId && u.GuildId == guildId);
        DbUser user = await cursor.FirstOrDefaultAsync();
        if (user != null) 
            return user;

        DbUser newUser = new() { GuildId = guildId, UserId = userId };
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
        string collection = obj.GetType().GetCustomAttribute<BsonCollectionAttribute>(true).CollectionName;
        await Database.GetCollection<T>(collection).DeleteOneAsync(o => o.Id == obj.Id);
    }

    public static async Task UpdateObjectAsync<T>(T obj) where T : DbObject
    {
        string collection = obj.GetType().GetCustomAttribute<BsonCollectionAttribute>(true).CollectionName;
        await Database.GetCollection<T>(collection).ReplaceOneAsync(o => o.Id == obj.Id, obj);
    }
}