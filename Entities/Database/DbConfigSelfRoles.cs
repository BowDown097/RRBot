namespace RRBot.Entities.Database
{
    [FirestoreData]
    public class DbConfigSelfRoles : DbObject
    {
        [FirestoreDocumentId]
        public override DocumentReference Reference { get; set; }
        [FirestoreProperty]
        public ulong Channel { get; set; }
        [FirestoreProperty]
        public ulong Message { get; set; }
        [FirestoreProperty]
        public Dictionary<string, ulong> SelfRoles { get; set; } = new();

        public static async Task<DbConfigSelfRoles> GetById(ulong guildId)
        {
            if (MemoryCache.Default.Contains($"selfroleconf-{guildId}"))
                return (DbConfigSelfRoles)MemoryCache.Default.Get($"selfroleconf-{guildId}");

            DocumentReference doc = Program.database.Collection($"servers/{guildId}/config").Document("selfroles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (!snap.Exists)
            {
                await doc.CreateAsync(new { Channel = 0UL });
                return await GetById(guildId);
            }

            DbConfigSelfRoles config = snap.ConvertTo<DbConfigSelfRoles>();
            MemoryCache.Default.CacheDatabaseObject($"selfroleconf-{guildId}", config);
            return config;
        }
    }
}