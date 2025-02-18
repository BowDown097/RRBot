namespace RRBot.Database.Entities.Config;

[BsonCollection("roleconfigs")]
[BsonIgnoreExtraElements]
public class DbConfigRoles(ulong guildId) : DbConfig
{
    public override ObjectId Id { get; set; }
    
    public override ulong GuildId { get; init; } = guildId;

    public ulong DjRole { get; set; }
    public ulong StaffLvl1Role { get; set; }
    public ulong StaffLvl2Role { get; set; }
}