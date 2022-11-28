namespace RRBot.Database.Entities.Config;

[BsonCollection("selfroleconfigs")]
[BsonIgnoreExtraElements]
public class DbConfigSelfRoles : DbConfig
{
    public override ObjectId Id { get; set; }
    
    public override ulong GuildId { get; init; }

    public ulong Channel { get; set; }
    public ulong Message { get; set; }
    public Dictionary<string, ulong> SelfRoles { get; set; } = new();
}