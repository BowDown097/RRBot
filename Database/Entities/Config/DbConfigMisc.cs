namespace RRBot.Database.Entities.Config;

[BsonCollection("miscconfigs")]
[BsonIgnoreExtraElements]
public class DbConfigMisc : DbConfig
{
    public override ObjectId Id { get; set; }
    
    public override ulong GuildId { get; init; }

    public List<string> DisabledCommands { get; set; } = new();
    public List<string> DisabledModules { get; set; } = new();
    public bool DropsDisabled { get; set; }
    public bool InviteFilterEnabled { get; set; }
    public List<ulong> NoFilterChannels { get; set; } = new();
    public bool NsfwEnabled { get; set; }
    public bool ScamFilterEnabled { get; set; }
}