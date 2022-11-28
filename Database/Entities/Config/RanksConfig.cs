namespace RRBot.Database.Entities.Config;

public class RanksConfig
{
    public Dictionary<int, decimal> Costs { get; set; } = new();
    public Dictionary<int, ulong> Ids { get; set; } = new();
}