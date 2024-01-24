namespace RRBot.Entities.Goods;
public class Consumable(string name, string information, string negEffect, string posEffect, long duration, int max = -1)
    : Item(name, 0)
{
    public long Duration { get; } = duration;
    public string Information { get; } = information;
    public int Max { get; } = max;
    public string NegEffect { get; } = negEffect;
    public string PosEffect { get; } = posEffect;
    public override string Name { get; protected init; }
    public override decimal Price { get; protected init; }
}