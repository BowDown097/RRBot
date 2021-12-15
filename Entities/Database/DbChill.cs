namespace RRBot.Entities.Database;
[FirestoreData]
public class DbChill : DbObject
{
    [FirestoreDocumentId]
    public override DocumentReference Reference { get; set; }
    [FirestoreProperty]
    public long Time { get; set; }

    public static async Task<DbChill> GetById(ulong guildId, ulong channelId)
    {
        if (MemoryCache.Default.Contains($"chill-{guildId}-{channelId}"))
            return (DbChill)MemoryCache.Default.Get($"chill-{guildId}-{channelId}");

        DocumentReference doc = Program.database.Collection($"servers/{guildId}/chills").Document(channelId.ToString());
        DocumentSnapshot snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
        {
            await doc.CreateAsync(new { Time = -1 });
            return await GetById(guildId, channelId);
        }

        DbChill config = snap.ConvertTo<DbChill>();
        MemoryCache.Default.CacheDatabaseObject($"chill-{guildId}-{channelId}", config);
        return config;
    }
}