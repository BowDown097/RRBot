namespace RRBot.Entities.Commands.Goods;
public class Weapon : Item
{
    public int DamageMin { get; }
    public int DamageMax { get; }
    public string Information { get; }
    public override string Name { get; protected init; }
    public override decimal Price { get; protected init; }

    public Weapon(string name, decimal price, int damageMin, int damageMax, string information) : base(name, price)
    {
        DamageMin = damageMin;
        DamageMax = damageMax;
        Information = information;
    }
}