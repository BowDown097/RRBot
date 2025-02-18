namespace RRBot.Database.Entities;

[BsonCollection("bans")]
[BsonIgnoreExtraElements]
public class DbBan(ulong guildId, ulong userId) : DbObject
{
    public override ObjectId Id { get; set; }

    public ulong GuildId { get; init; } = guildId;
    public ulong UserId { get; init; } = userId;

    public long Time { get; set; } = -1;
}