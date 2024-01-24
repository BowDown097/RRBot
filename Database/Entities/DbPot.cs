namespace RRBot.Database.Entities;

[BsonCollection("pots")]
[BsonIgnoreExtraElements]
public class DbPot : DbObject
{
    public override ObjectId Id { get; set; }
    
    public ulong GuildId { get; init; }

    public long EndTime { get; set; } = -1;
    [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
    public Dictionary<ulong, decimal> Members { get; set; } = new();
    public decimal Value { get; set; }

    public ulong DrawMember()
    {
        decimal[] ranges = [0, 0];
        decimal roll = RandomUtil.NextDecimal(100);
        foreach (KeyValuePair<ulong, decimal> mem in Members)
        {
            decimal odds = GetMemberOdds(mem.Key);
            (ranges[0], ranges[1]) = (ranges[1], ranges[0]);
            ranges[1] = ranges[0] + odds;
            if (roll > ranges[0] && roll <= ranges[1])
                return Convert.ToUInt64(mem.Key);
        }

        return 0;
    }

    public decimal GetMemberOdds(ulong userId)
        => Members.TryGetValue(userId, out decimal memValue) ? Math.Round(memValue / Value * 100, 2) : 0;
}