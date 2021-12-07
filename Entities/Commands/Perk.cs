namespace RRBot.Entities.Commands;
public class Perk
{
    public string name;
    public string description;
    public double price;
    public long duration;

    public Perk(string name, string description, double price, long duration)
    {
        this.name = name;
        this.description = description;
        this.price = price;
        this.duration = duration;
    }
}