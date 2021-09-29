using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

namespace RRBot.Entities
{
    [FirestoreData]
    public class DbGlobalConfig
    {
        [FirestoreDocumentId]
        public DocumentReference Reference { get; set; }
        [FirestoreProperty("bannedUsers")]
        public List<ulong> BannedUsers { get; set; } = new();
        [FirestoreProperty("disabledCommands")]
        public List<string> DisabledCommands { get; set; } = new();

        public static async Task<DbGlobalConfig> Get()
        {
            DocumentReference doc = Program.database.Collection("globalConfig").Document("banstuff");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
                return snap.ConvertTo<DbGlobalConfig>();
            await doc.CreateAsync(new { bannedUsers = new List<ulong>() });
            return await Get();
        }

        public async Task Write() => await Reference.SetAsync(this);
    }
}