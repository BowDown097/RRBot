namespace RRBot.Entities.Goods;
public class Perk(string name, string description, decimal price, long duration) : Item
{
    public string Description => description;
    public long Duration => duration;
    public override string Name => name;
    public override decimal Price => price;
}