namespace RRBot.Entities.Commands.Goods;
public class Collectible : Item
{
    public string Description { get; set; }
    public bool Discardable { get; set; }
    public string Image { get; set; }
    public override string Name { get; set; }
    public override double Price { get; set; }

    public Collectible(string name, string description, double price, string image, bool discardable = true)
        : base(name, price)
    {
        Description = description;
        Discardable = discardable;
        Image = image;
    }
}