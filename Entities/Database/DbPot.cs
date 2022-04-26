namespace RRBot.Entities.Database;
[FirestoreData]
public class DbPot : DbObject
{
    [FirestoreDocumentId]
    public override DocumentReference Reference { get; set; }
    [FirestoreProperty]
    public long EndTime { get; set; }
    [FirestoreProperty]
    public Dictionary<string, double> Members { get; set; } = new();
    [FirestoreProperty]
    public double Value { get; set; }

    public ulong DrawMember()
    {
        double[] ranges = {0, 0};
        double roll = RandomUtil.NextDouble(0, 100);
        foreach (KeyValuePair<string, double> mem in Members)
        {
            double odds = GetMemberOdds(mem.Key);
            (ranges[0], ranges[1]) = (ranges[1], ranges[0]);
            ranges[1] = ranges[0] + odds;
            if (roll > ranges[0] && roll <= ranges[1])
                return Convert.ToUInt64(mem.Key);
        }

        return 0;
    }

    public double GetMemberOdds(string userId)
        => Members.TryGetValue(userId, out double memValue) ? Math.Round(memValue / Value * 100, 2) : 0;

    public static async Task<DbPot> GetById(ulong guildId)
    {
        if (MemoryCache.Default.Contains($"pot-{guildId}"))
            return (DbPot)MemoryCache.Default.Get($"pot-{guildId}");

        DocumentReference doc = Program.database.Collection($"servers/{guildId}/pots").Document("1"); // might add support for multiple pots at some point
        DocumentSnapshot snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
        {
            await doc.CreateAsync(new { EndTime = -1 });
            return await GetById(guildId);
        }

        DbPot pot = snap.ConvertTo<DbPot>();
        MemoryCache.Default.CacheDatabaseObject($"pot-{guildId}", pot);
        return pot;
    }
}