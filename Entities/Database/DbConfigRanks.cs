namespace RRBot.Entities.Database;
[FirestoreData]
public class DbConfigRanks : DbObject
{
    #region Variables
    [FirestoreDocumentId]
    public override DocumentReference Reference { get; set; }
    [FirestoreProperty]
    public Dictionary<string, double> Costs { get; set; } = new();
    [FirestoreProperty]
    public Dictionary<string, ulong> Ids { get; set; } = new();
    #endregion

    #region Methods
    public static async Task<DbConfigRanks> GetById(ulong guildId)
    {
        if (MemoryCache.Default.Contains($"rankconf-{guildId}"))
            return (DbConfigRanks)MemoryCache.Default.Get($"rankconf-{guildId}");

        DocumentReference doc = Program.database.Collection($"servers/{guildId}/config").Document("ranks");
        DocumentSnapshot snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
        {
            await doc.CreateAsync(new { Costs = new Dictionary<string, double>() });
            return await GetById(guildId);
        }

        DbConfigRanks config = snap.ConvertTo<DbConfigRanks>();
        MemoryCache.Default.CacheDatabaseObject($"rankconf-{guildId}", config);
        return config;
    }
    #endregion
}