namespace RRBot.Entities.Database;
[FirestoreData]
public class DbElection : DbObject
{
    #region Variables
    [FirestoreDocumentId]
    public override DocumentReference Reference { get; set; }
    [FirestoreProperty]
    public ulong AnnouncementMessage { get; set; }
    [FirestoreProperty]
    public Dictionary<string, int> Candidates { get; set; } = new();
    [FirestoreProperty]
    public long EndTime { get; set; }
    [FirestoreProperty]
    public int NumWinners { get; set; }
    [FirestoreProperty]
    public List<ulong> Voters { get; set; } = new();
    #endregion

    #region Methods
    public static async Task<DbElection> GetById(ulong guildId, int? electionId = null)
    {
        if (electionId == null)
        {
            QuerySnapshot elections = await Program.database.Collection($"servers/{guildId}/elections").GetSnapshotAsync();
            IOrderedEnumerable<string> orderedCacheElections = MemoryCache.Default.Where(kvp => kvp.Key.StartsWith("election-")).Select(kvp => kvp.Key.Split('-')[2]).OrderBy(id => Convert.ToInt32(id));
            IOrderedEnumerable<string> orderedDbElections = elections.Select(r => r.Id).OrderBy(id => Convert.ToInt32(id));
            IEnumerable<string> orderedElections = orderedDbElections.Concat(orderedCacheElections.Where(e => !orderedDbElections.Contains(e)));
            electionId = orderedElections.Any() ? Convert.ToInt32(orderedElections.Last()) + 1 : 1;
        }

        if (MemoryCache.Default.Contains($"election-{guildId}-{electionId}"))
            return (DbElection)MemoryCache.Default.Get($"election-{guildId}-{electionId}");

        DocumentReference doc = Program.database.Collection($"servers/{guildId}/elections").Document(electionId.ToString());
        DocumentSnapshot snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
        {
            await doc.CreateAsync(new { EndTime = -1 });
            return await GetById(guildId, electionId);
        }

        DbElection election = snap.ConvertTo<DbElection>();
        MemoryCache.Default.CacheDatabaseObject($"election-{guildId}-{electionId}", election);
        return election;
    }
    #endregion
}