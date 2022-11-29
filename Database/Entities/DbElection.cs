namespace RRBot.Database.Entities;

[BsonCollection("elections")]
[BsonIgnoreExtraElements]
public class DbElection : DbObject
{
    public override ObjectId Id { get; set; }
    
    public int ElectionId { get; init; }
    public ulong GuildId { get; init; }

    public ulong AnnouncementMessage { get; set; }
    [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
    public Dictionary<ulong, int> Candidates { get; set; } = new();
    public long EndTime { get; set; } = -1;
    public int NumWinners { get; set; }
    [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
    public Dictionary<ulong, List<ulong>> Voters { get; set; } = new();
}