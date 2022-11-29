namespace RRBot.Database.Entities.Config;

[BsonCollection("rankconfigs")]
[BsonIgnoreExtraElements]
public class DbConfigRanks : DbConfig
{
    public override ObjectId Id { get; set; }
    
    public override ulong GuildId { get; init; }
    
    [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
    public Dictionary<int, decimal> Costs { get; set; } = new();
    [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
    public Dictionary<int, ulong> Ids { get; set; } = new();
}