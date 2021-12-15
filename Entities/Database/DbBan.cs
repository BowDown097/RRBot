namespace RRBot.Entities.Database;
[FirestoreData]
public class DbBan : DbObject
{
    [FirestoreDocumentId]
    public override DocumentReference Reference { get; set; }
    [FirestoreProperty]
    public long Time { get; set; }

    public static async Task<DbBan> GetById(ulong guildId, ulong userId)
    {
        if (MemoryCache.Default.Contains($"ban-{guildId}-{userId}"))
            return (DbBan)MemoryCache.Default.Get($"ban-{guildId}-{userId}");

        DocumentReference doc = Program.database.Collection($"servers/{guildId}/bans").Document(userId.ToString());
        DocumentSnapshot snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
        {
            await doc.CreateAsync(new { Time = -1 });
            return await GetById(guildId, userId);
        }

        DbBan config = snap.ConvertTo<DbBan>();
        MemoryCache.Default.CacheDatabaseObject($"ban-{guildId}-{userId}", config);
        return config;
    }
}