namespace RRBot.Entities.Commands.Goods;
public class Weapon : Item
{
    public int DamageMin { get; set; }
    public int DamageMax { get; set; }
    public string Information { get; set; }
    public override string Name { get; set; }
    public override double Price { get; set; }

    public Weapon(string name, double price, int damageMin, int damageMax, string information) : base(name, price)
    {
        DamageMin = damageMin;
        DamageMax = damageMax;
        Information = information;
    }
}