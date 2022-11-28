namespace RRBot.Entities.Commands.Goods;
public class Perk : Item
{
    public string Description { get; }
    public long Duration { get; }
    public override string Name { get; protected init; }
    public override decimal Price { get; protected init; }

    public Perk(string name, string description, decimal price, long duration) : base(name, price)
    {
        Description = description;
        Duration = duration;
    }
}