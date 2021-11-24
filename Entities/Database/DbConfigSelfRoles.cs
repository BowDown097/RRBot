namespace RRBot.Entities.Database
{
    [FirestoreData]
    public class DbConfigSelfRoles
    {
        [FirestoreDocumentId]
        public DocumentReference Reference { get; set; }
        [FirestoreProperty("channel")]
        public ulong Channel { get; set; }
        [FirestoreProperty("message")]
        public ulong Message { get; set; }
        [FirestoreProperty("selfroles")]
        public Dictionary<string, ulong> SelfRoles { get; set; } = new();

        public static async Task<DbConfigSelfRoles> GetById(ulong guildId)
        {
            DocumentReference doc = Program.database.Collection($"servers/{guildId}/config").Document("selfroles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (!snap.Exists)
            {
                await doc.CreateAsync(new { Channel = 0UL });
                return await GetById(guildId);
            }

            return snap.ConvertTo<DbConfigSelfRoles>();
        }

        public async Task Write() => await Reference.SetAsync(this);
    }
}