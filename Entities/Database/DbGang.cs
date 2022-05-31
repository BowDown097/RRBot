namespace RRBot.Entities.Database;
[FirestoreData]
public class DbGang : DbObject
{
    #region Variables
    [FirestoreDocumentId]
    public override DocumentReference Reference { get; set; }
    [FirestoreProperty]
    public List<ulong> JoinRequests { get; set; }
    [FirestoreProperty]
    public ulong Leader { get; set; }
    [FirestoreProperty]
    public Dictionary<ulong, string> Members { get; set; } = new();
    [FirestoreProperty]
    public double VaultBalance { get; set; }
    [FirestoreProperty]
    public bool VaultUnlocked { get; set; }
    #endregion

    #region Methods
    public static async Task<DbGang> GetByName(ulong guildId, string name)
    {
        if (MemoryCache.Default.Contains($"gang-{guildId}-{name}"))
            return (DbGang)MemoryCache.Default.Get($"gang-{guildId}-{name}");

        DocumentReference doc = Program.database.Collection($"servers/{guildId}/gangs").Document(name);
        DocumentSnapshot snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
        {
            await doc.CreateAsync(new { Leader = 0UL });
            return await GetByName(guildId, name);
        }

        DbGang gang = snap.ConvertTo<DbGang>();
        MemoryCache.Default.CacheDatabaseObject($"gang-{guildId}-{name}", gang);
        return gang;
    }
    #endregion
}