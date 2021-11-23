namespace RRBot.Entities.Database
{
    [FirestoreData]
    public class DbConfigRanks
    {
        [FirestoreDocumentId]
        public DocumentReference Reference { get; set; }
        [FirestoreProperty("costs")]
        public Dictionary<string, double> Costs { get; set; } = new();
        [FirestoreProperty("ids")]
        public Dictionary<string, ulong> Ids { get; set; } = new();

        public static async Task<DbConfigRanks> GetById(ulong guildId)
        {
            DocumentReference doc = Program.database.Collection($"servers/{guildId}/config").Document("ranks");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (!snap.Exists)
            {
                await doc.CreateAsync(new { Costs = new Dictionary<string, double>() });
                return await GetById(guildId);
            }

            return snap.ConvertTo<DbConfigRanks>();
        }

        public async Task Write() => await Reference.SetAsync(this);
    }
}