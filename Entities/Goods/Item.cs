namespace RRBot.Entities.Goods;
public abstract class Item
{
    public abstract string Name { get; }
    public abstract decimal Price { get; }

    public override string ToString() => Name;
}