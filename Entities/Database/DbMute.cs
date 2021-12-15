namespace RRBot.Entities.Database;
[FirestoreData]
public class DbMute : DbObject
{
    [FirestoreDocumentId]
    public override DocumentReference Reference { get; set; }
    [FirestoreProperty]
    public long Time { get; set; }

    public static async Task<DbMute> GetById(ulong guildId, ulong userId)
    {
        if (MemoryCache.Default.Contains($"mute-{guildId}-{userId}"))
            return (DbMute)MemoryCache.Default.Get($"mute-{guildId}-{userId}");

        DocumentReference doc = Program.database.Collection($"servers/{guildId}/mutes").Document(userId.ToString());
        DocumentSnapshot snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
        {
            await doc.CreateAsync(new { Time = -1 });
            return await GetById(guildId, userId);
        }

        DbMute config = snap.ConvertTo<DbMute>();
        MemoryCache.Default.CacheDatabaseObject($"mute-{guildId}-{userId}", config);
        return config;
    }
}