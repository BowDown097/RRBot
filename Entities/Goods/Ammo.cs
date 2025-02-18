namespace RRBot.Entities.Goods;
public class Ammo(string name, double crateMultiplier) : Item
{
    public double CrateMultiplier => crateMultiplier;
    public override string Name => name;
    public override decimal Price => -1;
}