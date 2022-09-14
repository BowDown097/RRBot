namespace RRBot.Entities.Database;
[FirestoreData]
public class DbGang : DbObject
{
    #region Variables
    [FirestoreDocumentId]
    public override DocumentReference Reference { get; set; }
    [FirestoreProperty]
    public bool IsPublic { get; set; }
    [FirestoreProperty]
    public ulong Leader { get; set; }
    [FirestoreProperty]
    public Dictionary<string, string> Members { get; set; } = new();
    [FirestoreProperty]
    public string Name { get; set; }
    [FirestoreProperty]
    public double VaultBalance { get; set; }
    [FirestoreProperty]
    public bool VaultUnlocked { get; set; }
    #endregion

    #region Methods
    public static async Task<DbGang> GetByName(ulong guildId, string name, bool useCache = true)
    {
        string dbName = name.ToLower();
        if (useCache && MemoryCache.Default.Contains($"gang-{guildId}-{dbName}"))
            return (DbGang)MemoryCache.Default.Get($"gang-{guildId}-{dbName}");

        DocumentReference doc = Program.Database.Collection($"servers/{guildId}/gangs").Document(dbName);
        DocumentSnapshot snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
        {
            await doc.CreateAsync(new { Name = name });
            return await GetByName(guildId, name);
        }

        DbGang gang = snap.ConvertTo<DbGang>();
        if (useCache)
            MemoryCache.Default.CacheDatabaseObject($"gang-{guildId}-{dbName}", gang);
        return gang;
    }
    #endregion
}