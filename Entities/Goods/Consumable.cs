namespace RRBot.Entities.Goods;
public class Consumable : Item
{
    public long Duration { get; }
    public string Information { get; }
    public int Max { get; }
    public string NegEffect { get; }
    public string PosEffect { get; }
    public override string Name { get; protected init; }
    public override decimal Price { get; protected init; }

    public Consumable(string name, string information, string negEffect, string posEffect, long duration, int max = -1) : base(name, 0)
    {
        Information = information;
        Max = max;
        NegEffect = negEffect;
        PosEffect = posEffect;
        Duration = duration;
    }
}