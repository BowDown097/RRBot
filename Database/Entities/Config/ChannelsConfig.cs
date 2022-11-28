namespace RRBot.Database.Entities.Config;

public class ChannelsConfig
{
    public ulong ElectionsAnnounceChannel { get; set; }
    public ulong ElectionsVotingChannel { get; set; }
    public int MinimumVotingAgeDays { get; set; }
    public ulong LogsChannel { get; set; }
    public ulong PollsChannel { get; set; }
    public ulong PotChannel { get; set; }
    public List<ulong> WhitelistedChannels { get; set; } = new();
}