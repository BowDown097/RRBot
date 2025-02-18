namespace RRBot.Entities.Goods;
public class Weapon(
    string name, int accuracy, string ammo, int damageMin, int damageMax, int dropChance,
    string information, string[] insideCrates, string type) : Item
{
    public int Accuracy => accuracy;
    public string Ammo => ammo;
    public int DamageMin => damageMin;
    public int DamageMax => damageMax;
    public int DropChance => dropChance;
    public string Information => information;
    public string[] InsideCrates => insideCrates;
    public string Type => type;
    public override string Name => name;
    public override decimal Price => -1;
}