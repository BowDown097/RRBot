namespace RRBot.Entities.Goods;
public class Consumable(
    string name, string information, string negEffect, string posEffect, long duration, int max = -1) : Item
{
    public long Duration => duration;
    public string Information => information;
    public int Max => max;
    public string NegEffect => negEffect;
    public string PosEffect => posEffect;
    public override string Name => name;
    public override decimal Price => -1;
}