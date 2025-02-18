namespace RRBot.Database.Entities.Config;

[BsonCollection("rankconfigs")]
[BsonIgnoreExtraElements]
public class DbConfigRanks(ulong guildId) : DbConfig
{
    public override ObjectId Id { get; set; }
    
    public override ulong GuildId { get; init; } = guildId;
    
    [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
    public Dictionary<int, decimal> Costs { get; set; } = [];
    [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
    public Dictionary<int, ulong> Ids { get; set; } = [];
}