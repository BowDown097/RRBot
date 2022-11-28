namespace RRBot.Entities.Goods;
public abstract class Item
{
    public abstract string Name { get; protected init; }
    public abstract decimal Price { get; protected init; }

    protected Item(string name, decimal price)
    {
        Name = name;
        Price = price;
    }

    public override string ToString() => Name;
}