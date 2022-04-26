namespace RRBot.Entities.Database;
[FirestoreData]
public class DbConfigChannels : DbObject
{
    [FirestoreDocumentId]
    public override DocumentReference Reference { get; set; }
    [FirestoreProperty]
    public ulong LogsChannel { get; set; }
    [FirestoreProperty]
    public ulong PollsChannel { get; set; }
    [FirestoreProperty]
    public ulong PotChannel { get; set; }
    [FirestoreProperty]
    public List<ulong> WhitelistedChannels { get; set; } = new();

    public static async Task<DbConfigChannels> GetById(ulong guildId)
    {
        if (MemoryCache.Default.Contains($"chanconf-{guildId}"))
            return (DbConfigChannels)MemoryCache.Default.Get($"chanconf-{guildId}");

        DocumentReference doc = Program.database.Collection($"servers/{guildId}/config").Document("channels");
        DocumentSnapshot snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
        {
            await doc.CreateAsync(new { LogsChannel = 0UL });
            return await GetById(guildId);
        }

        DbConfigChannels config = snap.ConvertTo<DbConfigChannels>();
        MemoryCache.Default.CacheDatabaseObject($"chanconf-{guildId}", config);
        return config;
    }
}