namespace RRBot.Entities.Goods;

public class Weapon : Item
{
    public int Accuracy { get; }
    public string Ammo { get; }
    public int DamageMin { get; }
    public int DamageMax { get; }
    public int DropChance { get; }
    public string Information { get; }
    public string[] InsideCrates { get; }
    public string Type { get; }
    public override string Name { get; protected init; }
    public override decimal Price { get; protected init; }

    public Weapon(string name, int accuracy, string ammo, int damageMin, int damageMax, int dropChance,
        string information, string[] insideCrates, string type)
        : base(name, -1)
    {
        Accuracy = accuracy;
        Ammo = ammo;
        DamageMin = damageMin;
        DamageMax = damageMax;
        DropChance = dropChance;
        Information = information;
        InsideCrates = insideCrates;
        Type = type;
    }
}