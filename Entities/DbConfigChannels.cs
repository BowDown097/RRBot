using System.Threading.Tasks;
using Google.Cloud.Firestore;

namespace RRBot.Entities
{
    [FirestoreData]
    public class DbConfigChannels
    {
        [FirestoreDocumentId]
        public DocumentReference Reference { get; set; }
        [FirestoreProperty("logsChannel")]
        public ulong LogsChannel { get; set; }
        [FirestoreProperty("pollsChannel")]
        public ulong PollsChannel { get; set; }

        public static async Task<DbConfigChannels> GetById(ulong guildId)
        {
            DocumentReference doc = Program.database.Collection($"servers/{guildId}/config").Document("channels");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
                return snap.ConvertTo<DbConfigChannels>();
            await doc.CreateAsync(new { logsChannel = 0UL });
            return await GetById(guildId);
        }

        public async Task Write() => await Reference.SetAsync(this);
    }
}