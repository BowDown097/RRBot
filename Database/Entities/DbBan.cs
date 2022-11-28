namespace RRBot.Database.Entities;

[BsonCollection("bans")]
[BsonIgnoreExtraElements]
public class DbBan : DbObject
{
    public override ObjectId Id { get; set; }

    public ulong GuildId { get; init; }
    public ulong UserId { get; init; }

    public long Time { get; set; } = -1;
}