namespace RRBot.Entities.Database;
[FirestoreData]
public class DbConfigModules : DbObject
{
    [FirestoreDocumentId]
    public override DocumentReference Reference { get; set; }
    [FirestoreProperty]
    public bool NSFWEnabled { get; set; }

    public static async Task<DbConfigModules> GetById(ulong guildId)
    {
        if (MemoryCache.Default.Contains($"moduleconf-{guildId}"))
            return (DbConfigModules)MemoryCache.Default.Get($"moduleconf-{guildId}");

        DocumentReference doc = Program.database.Collection($"servers/{guildId}/config").Document("modules");
        DocumentSnapshot snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
        {
            await doc.CreateAsync(new { NSFWEnabled = false });
            return await GetById(guildId);
        }

        DbConfigModules config = snap.ConvertTo<DbConfigModules>();
        MemoryCache.Default.CacheDatabaseObject($"moduleconf-{guildId}", config);
        return config;
    }
}