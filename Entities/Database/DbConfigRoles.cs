namespace RRBot.Entities.Database
{
    [FirestoreData]
    public class DbConfigRoles : DbObject
    {
        [FirestoreDocumentId]
        public override DocumentReference Reference { get; set; }
        [FirestoreProperty("djRole")]
        public ulong DJRole { get; set; }
        [FirestoreProperty("mutedRole")]
        public ulong MutedRole { get; set; }
        [FirestoreProperty("staffLvl1Role")]
        public ulong StaffLvl1Role { get; set; }
        [FirestoreProperty("staffLvl2Role")]
        public ulong StaffLvl2Role { get; set; }

        public static async Task<DbConfigRoles> GetById(ulong guildId)
        {
            if (MemoryCache.Default.Contains($"roleconf-{guildId}"))
                return (DbConfigRoles)MemoryCache.Default.Get($"roleconf-{guildId}");

            DocumentReference doc = Program.database.Collection($"servers/{guildId}/config").Document("roles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (!snap.Exists)
            {
                await doc.CreateAsync(new { djRole = 0UL });
                return await GetById(guildId);
            }

            DbConfigRoles config = snap.ConvertTo<DbConfigRoles>();
            MemoryCache.Default.CacheDatabaseObject($"roleconf-{guildId}", config);
            return config;
        }
    }
}