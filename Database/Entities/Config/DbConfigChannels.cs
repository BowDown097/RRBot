namespace RRBot.Database.Entities.Config;

[BsonCollection("channelconfigs")]
[BsonIgnoreExtraElements]
public class DbConfigChannels : DbConfig
{
    public override ObjectId Id { get; set; }
    
    public override ulong GuildId { get; init; }

    public ulong ElectionsAnnounceChannel { get; set; }
    public ulong ElectionsVotingChannel { get; set; }
    public int MinimumVotingAgeDays { get; set; }
    public ulong LogsChannel { get; set; }
    public ulong PollsChannel { get; set; }
    public ulong PotChannel { get; set; }
    public List<ulong> WhitelistedChannels { get; set; } = new();
}