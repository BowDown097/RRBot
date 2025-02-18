namespace RRBot.Database.Entities.Config;

[BsonCollection("miscconfigs")]
[BsonIgnoreExtraElements]
public class DbConfigMisc(ulong guildId) : DbConfig
{
    public override ObjectId Id { get; set; }
    
    public override ulong GuildId { get; init; } = guildId;

    public List<string> DisabledCommands { get; set; } = [];
    public List<string> DisabledModules { get; set; } = [];
    public bool DropsDisabled { get; set; }
    public bool InviteFilterEnabled { get; set; }
    public List<ulong> NoFilterChannels { get; set; } = [];
    public bool NsfwEnabled { get; set; }
    public bool ScamFilterEnabled { get; set; }
}