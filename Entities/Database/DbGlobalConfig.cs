namespace RRBot.Entities.Database
{
    [FirestoreData]
    public class DbGlobalConfig : DbObject
    {
        [FirestoreDocumentId]
        public override DocumentReference Reference { get; set; }
        [FirestoreProperty("bannedUsers")]
        public List<ulong> BannedUsers { get; set; } = new();
        [FirestoreProperty("disabledCommands")]
        public List<string> DisabledCommands { get; set; } = new();

        public static async Task<DbGlobalConfig> Get()
        {
            if (MemoryCache.Default.Contains("globalconfig"))
                return (DbGlobalConfig)MemoryCache.Default.Get("globalconfig");

            DocumentReference doc = Program.database.Collection("globalConfig").Document("banstuff");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (!snap.Exists)
            {
                await doc.CreateAsync(new { bannedUsers = new List<ulong>() });
                return await Get();
            }

            DbGlobalConfig config = snap.ConvertTo<DbGlobalConfig>();
            MemoryCache.Default.CacheDatabaseObject("globalconfig", config);
            return config;
        }
    }
}