namespace RRBot.Database.Entities.Config;

[BsonCollection("rankconfigs")]
[BsonIgnoreExtraElements]
public class DbConfigRanks : DbConfig
{
    public override ObjectId Id { get; set; }
    
    public override ulong GuildId { get; init; }

    public Dictionary<int, decimal> Costs { get; set; } = new();
    public Dictionary<int, ulong> Ids { get; set; } = new();
}