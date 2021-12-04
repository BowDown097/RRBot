namespace RRBot.Entities.Database
{
    [FirestoreData]
    public class DbConfigChannels : DbObject
    {
        [FirestoreDocumentId]
        public override DocumentReference Reference { get; set; }
        [FirestoreProperty("logsChannel")]
        public ulong LogsChannel { get; set; }
        [FirestoreProperty("pollsChannel")]
        public ulong PollsChannel { get; set; }

        public static async Task<DbConfigChannels> GetById(ulong guildId)
        {
            if (MemoryCache.Default.Contains($"chanconf-{guildId}"))
                return (DbConfigChannels)MemoryCache.Default.Get($"chanconf-{guildId}");

            DocumentReference doc = Program.database.Collection($"servers/{guildId}/config").Document("channels");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (!snap.Exists)
            {
                await doc.CreateAsync(new { logsChannel = 0UL });
                return await GetById(guildId);
            }

            DbConfigChannels config = snap.ConvertTo<DbConfigChannels>();
            MemoryCache.Default.CacheDatabaseObject($"chanconf-{guildId}", config);
            return config;
        }
    }
}