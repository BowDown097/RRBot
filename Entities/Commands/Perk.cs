namespace RRBot.Entities.Commands;
public class Perk : Item
{
    public string Description { get; set; }
    public long Duration { get; set; }
    public override string Name { get; set; }
    public override double Price { get; set; }

    public Perk(string name, string description, double price, long duration) : base(name, price)
    {
        Description = description;
        Duration = duration;
    }
}