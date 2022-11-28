namespace RRBot.Database.Entities;

[BsonCollection("configs")]
[BsonIgnoreExtraElements]
public class DbConfig : DbObject
{
    public override ObjectId Id { get; set; }
    
    public ulong GuildId { get; init; }

    public ChannelsConfig Channels { get; } = new();
    public MiscellaneousConfig Miscellaneous { get; } = new();
    public RanksConfig Ranks { get; } = new();
    public RolesConfig Roles { get; } = new();
    public SelfRolesConfig SelfRoles { get; } = new();
}