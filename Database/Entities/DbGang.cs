namespace RRBot.Database.Entities;

[BsonCollection("gangs")]
[BsonIgnoreExtraElements]
public class DbGang(ulong guildId, string name) : DbObject
{
    public override ObjectId Id { get; set; }
    
    public ulong GuildId { get; init; } = guildId;

    public bool IsPublic { get; set; }
    public ulong Leader { get; set; }
    [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
    public Dictionary<ulong, string> Members { get; set; } = [];
    public string Name { get; set; } = name;
    public decimal VaultBalance { get; set; }
    public bool VaultUnlocked { get; set; }
}