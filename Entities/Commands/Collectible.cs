namespace RRBot.Entities.Commands;
public class Collectible : Item
{
    public string Description { get; set; }
    public string Image { get; set; }
    public override string Name { get; set; }
    public override double Price { get; set; }

    public Collectible(string name, string description, double price, string image) : base(name, price)
    {
        Description = description;
        Image = image;
    }
}