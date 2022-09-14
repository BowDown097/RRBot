namespace RRBot.Entities.Database;
[FirestoreData]
public class DbConfigRoles : DbObject
{
    #region Variables
    [FirestoreDocumentId]
    public override DocumentReference Reference { get; set; }
    [FirestoreProperty]
    public ulong DjRole { get; set; }
    [FirestoreProperty]
    public ulong StaffLvl1Role { get; set; }
    [FirestoreProperty]
    public ulong StaffLvl2Role { get; set; }
    #endregion

    #region Methods
    public static async Task<DbConfigRoles> GetById(ulong guildId)
    {
        if (MemoryCache.Default.Contains($"roleconf-{guildId}"))
            return (DbConfigRoles)MemoryCache.Default.Get($"roleconf-{guildId}");

        DocumentReference doc = Program.Database.Collection($"servers/{guildId}/config").Document("roles");
        DocumentSnapshot snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
        {
            await doc.CreateAsync(new { DJRole = 0UL });
            return await GetById(guildId);
        }

        DbConfigRoles config = snap.ConvertTo<DbConfigRoles>();
        MemoryCache.Default.CacheDatabaseObject($"roleconf-{guildId}", config);
        return config;
    }
    #endregion
}