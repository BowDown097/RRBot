namespace RRBot.Entities.Goods;
public class Collectible(string name, string description, decimal price, string image, bool discardable = true)
    : Item(name, price)
{
    public string Description { get; } = description;
    public bool Discardable { get; } = discardable;
    public string Image { get; } = image;
    public override string Name { get; protected init; }
    public override decimal Price { get; protected init; }
}