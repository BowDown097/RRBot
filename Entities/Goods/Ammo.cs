namespace RRBot.Entities.Goods;
public class Ammo(string name, double crateMultiplier) : Item(name, -1)
{
    public double CrateMultiplier { get; } = crateMultiplier;
    public override string Name { get; protected init; }
    public override decimal Price { get; protected init; }
}