namespace RRBot.Entities.Database
{
    [FirestoreData]
    public class DbSupportTicket
    {
        [FirestoreDocumentId]
        public DocumentReference Reference { get; set; }
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
            DocumentReference doc = Program.database.Collection($"servers/{guildId}/supportTickets").Document(userId.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (!snap.Exists)
            {
                await doc.CreateAsync(new { req = "" });
                return await GetById(guildId, userId);
            }

            return snap.ConvertTo<DbSupportTicket>();
        }

        public async Task Write() => await Reference.SetAsync(this);
    }
}