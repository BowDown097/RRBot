namespace RRBot.Database.Entities;

[BsonCollection("globalconfig")]
[BsonIgnoreExtraElements]
public class DbGlobalConfig : DbObject
{
    public override ObjectId Id { get; set; }

    public List<ulong> BannedUsers { get; set; } = [];
    public List<string> DisabledCommands { get; set; } = [];
}