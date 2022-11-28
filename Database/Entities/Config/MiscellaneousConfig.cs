namespace RRBot.Database.Entities.Config;

public class MiscellaneousConfig
{
    public List<string> DisabledCommands { get; set; } = new();
    public List<string> DisabledModules { get; set; } = new();
    public bool DropsDisabled { get; set; }
    public List<string> FilterRegexes { get; set; } = new();
    public List<string> FilteredWords { get; set; } = new();
    public bool InviteFilterEnabled { get; set; }
    public List<ulong> NoFilterChannels { get; set; } = new();
    public bool NsfwEnabled { get; set; }
    public bool ScamFilterEnabled { get; set; }
}