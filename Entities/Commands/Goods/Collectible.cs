namespace RRBot.Entities.Commands.Goods;
public class Collectible : Item
{
    public string Description { get; }
    public bool Discardable { get; }
    public string Image { get; }
    public override string Name { get; protected init; }
    public override decimal Price { get; protected init; }

    public Collectible(string name, string description, decimal price, string image, bool discardable = true)
        : base(name, price)
    {
        Description = description;
        Discardable = discardable;
        Image = image;
    }
}