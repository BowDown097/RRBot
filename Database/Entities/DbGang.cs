namespace RRBot.Database.Entities;

[BsonCollection("gangs")]
[BsonIgnoreExtraElements]
public class DbGang : DbObject
{
    public override ObjectId Id { get; set; }
    
    public ulong GuildId { get; init; }

    public bool IsPublic { get; set; }
    public ulong Leader { get; set; }
    [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
    public Dictionary<ulong, string> Members { get; set; } = new();
    public string Name { get; set; }
    public decimal VaultBalance { get; set; }
    public bool VaultUnlocked { get; set; }
}