namespace RRBot.Entities.Goods;

public class Ammo : Item
{
    public double CrateMultiplier { get; }
    public override string Name { get; protected init; }
    public override decimal Price { get; protected init; }

    public Ammo(string name, double crateMultiplier) : base(name, -1)
    {
        CrateMultiplier = crateMultiplier;
    }
}