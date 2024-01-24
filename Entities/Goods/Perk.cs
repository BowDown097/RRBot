namespace RRBot.Entities.Goods;
public class Perk(string name, string description, decimal price, long duration)
    : Item(name, price)
{
    public string Description { get; } = description;
    public long Duration { get; } = duration;
    public override string Name { get; protected init; }
    public override decimal Price { get; protected init; }
}