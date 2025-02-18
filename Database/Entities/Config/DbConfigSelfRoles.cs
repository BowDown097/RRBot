namespace RRBot.Database.Entities.Config;

[BsonCollection("selfroleconfigs")]
[BsonIgnoreExtraElements]
public class DbConfigSelfRoles(ulong guildId) : DbConfig
{
    public override ObjectId Id { get; set; }
    
    public override ulong GuildId { get; init; } = guildId;

    public ulong Channel { get; set; }
    public ulong Message { get; set; }
    public Dictionary<string, ulong> SelfRoles { get; set; } = [];
}