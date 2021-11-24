namespace RRBot.Entities.Database
{
    [FirestoreData]
    public class DbConfigModules
    {
        [FirestoreDocumentId]
        public DocumentReference Reference { get; set; }
        [FirestoreProperty("nsfw")]
        public bool NSFWEnabled { get; set; }

        public static async Task<DbConfigModules> GetById(ulong guildId)
        {
            DocumentReference doc = Program.database.Collection($"servers/{guildId}/config").Document("modules");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (!snap.Exists)
            {
                await doc.CreateAsync(new { nsfw = false });
                return await GetById(guildId);
            }

            return snap.ConvertTo<DbConfigModules>();
        }

        public async Task Write() => await Reference.SetAsync(this);
    }
}