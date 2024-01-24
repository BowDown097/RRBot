namespace RRBot.Entities.Goods;
public class Weapon(string name, int accuracy, string ammo, int damageMin, int damageMax, int dropChance,
    string information, string[] insideCrates, string type) : Item(name, -1)
{
    public int Accuracy { get; } = accuracy;
    public string Ammo { get; } = ammo;
    public int DamageMin { get; } = damageMin;
    public int DamageMax { get; } = damageMax;
    public int DropChance { get; } = dropChance;
    public string Information { get; } = information;
    public string[] InsideCrates { get; } = insideCrates;
    public string Type { get; } = type;
    public override string Name { get; protected init; }
    public override decimal Price { get; protected init; }
}