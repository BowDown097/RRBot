namespace RRBot.Entities.Database
{
    [FirestoreData]
    public class DbSupportTicket : DbObject
    {
        [FirestoreDocumentId]
        public override DocumentReference Reference { get; set; }
        [FirestoreProperty("helper")]
        public ulong Helper { get; set; }
        [FirestoreProperty("issuer")]
        public ulong Issuer { get; set; }
        [FirestoreProperty("message")]
        public ulong Message { get; set; }
        [FirestoreProperty("req")]
        public string Request { get; set; }

        public static async Task<DbSupportTicket> GetById(ulong guildId, ulong userId)
        {
            if (MemoryCache.Default.Contains($"ticket-{guildId}-{userId}"))
                return (DbSupportTicket)MemoryCache.Default.Get($"ticket-{guildId}-{userId}");

            DocumentReference doc = Program.database.Collection($"servers/{guildId}/supportTickets").Document(userId.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (!snap.Exists)
            {
                await doc.CreateAsync(new { req = "" });
                return await GetById(guildId, userId);
            }

            DbSupportTicket config = snap.ConvertTo<DbSupportTicket>();
            MemoryCache.Default.CacheDatabaseObject($"ticket-{guildId}-{userId}", config);
            return config;
        }
    }
}