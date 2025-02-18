namespace RRBot.Database.Entities;

[BsonCollection("elections")]
[BsonIgnoreExtraElements]
public class DbElection(ulong guildId, int electionId) : DbObject
{
    public override ObjectId Id { get; set; }
    
    public int ElectionId { get; init; } = electionId;
    public ulong GuildId { get; init; } = guildId;

    public ulong AnnouncementMessage { get; set; }
    [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
    public Dictionary<ulong, int> Candidates { get; set; } = [];
    public long EndTime { get; set; } = -1;
    public int NumWinners { get; set; }
    [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
    public Dictionary<ulong, List<ulong>> Voters { get; set; } = [];
}