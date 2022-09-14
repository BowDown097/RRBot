namespace RRBot.Entities.Database;
[FirestoreData]
public class DbConfigChannels : DbObject
{
    #region Variables
    [FirestoreDocumentId]
    public override DocumentReference Reference { get; set; }
    [FirestoreProperty]
    public ulong ElectionsAnnounceChannel { get; set; }
    [FirestoreProperty]
    public ulong ElectionsVotingChannel { get; set; }
    [FirestoreProperty]
    public int MinimumVotingAgeDays { get; set; }
    [FirestoreProperty]
    public ulong LogsChannel { get; set; }
    [FirestoreProperty]
    public ulong PollsChannel { get; set; }
    [FirestoreProperty]
    public ulong PotChannel { get; set; }
    [FirestoreProperty]
    public List<ulong> WhitelistedChannels { get; set; } = new();
    #endregion

    #region Methods
    public static async Task<DbConfigChannels> GetById(ulong guildId)
    {
        if (MemoryCache.Default.Contains($"chanconf-{guildId}"))
            return (DbConfigChannels)MemoryCache.Default.Get($"chanconf-{guildId}");

        DocumentReference doc = Program.Database.Collection($"servers/{guildId}/config").Document("channels");
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
    #endregion
}