namespace RRBot.Database.Entities;

[BsonCollection("chills")]
[BsonIgnoreExtraElements]
public class DbChill : DbObject
{
    public override ObjectId Id { get; set; }
    
    public ulong ChannelId { get; init; }
    public ulong GuildId { get; init; }

    public long Time { get; set; } = -1;
}