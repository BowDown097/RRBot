namespace RRBot.Entities.Database;
[FirestoreData]
public class DbGlobalConfig : DbObject
{
    #region Variables
    [FirestoreDocumentId]
    public override DocumentReference Reference { get; set; }
    [FirestoreProperty]
    public List<ulong> BannedUsers { get; set; } = new();
    [FirestoreProperty]
    public List<string> DisabledCommands { get; set; } = new();
    #endregion

    #region Methods
    public static async Task<DbGlobalConfig> Get()
    {
        if (MemoryCache.Default.Contains("globalconfig"))
            return (DbGlobalConfig)MemoryCache.Default.Get("globalconfig");

        DocumentReference doc = Program.database.Collection("globalConfig").Document("banstuff");
        DocumentSnapshot snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
        {
            await doc.CreateAsync(new { BannedUsers = new List<ulong>() });
            return await Get();
        }

        DbGlobalConfig config = snap.ConvertTo<DbGlobalConfig>();
        MemoryCache.Default.CacheDatabaseObject("globalconfig", config);
        return config;
    }
    #endregion
}