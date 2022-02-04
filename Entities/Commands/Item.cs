namespace RRBot.Entities.Commands;
public abstract class Item
{
    public abstract string Name { get; set; }
    public abstract double Price { get; set; }

    protected Item(string name, double price)
    {
        Name = name;
        Price = price;
    }

    public override string ToString() => Name;
}