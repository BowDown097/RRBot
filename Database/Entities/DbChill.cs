namespace RRBot.Database.Entities;

[BsonCollection("chills")]
[BsonIgnoreExtraElements]
public class DbChill(ulong guildId, ulong channelId) : DbObject
{
    public override ObjectId Id { get; set; }
    
    public ulong ChannelId { get; init; } = channelId;
    public ulong GuildId { get; init; } = guildId;

    public long Time { get; set; } = -1;
}