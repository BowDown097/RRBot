namespace RRBot.Entities.Goods;
public class Collectible(
    string name, string description, decimal price, string image, bool discardable = true) : Item
{
    public string Description => description;
    public bool Discardable => discardable;
    public string Image => image;
    public override string Name => name;
    public override decimal Price => price;
}