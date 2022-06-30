namespace RRBot.Entities.Commands.Goods;
public class Consumable : Item
{
    public long Duration { get; set; }
    public string Information { get; set; }
    public int Max { get; set; }
    public string NegEffect { get; set; }
    public string PosEffect { get; set; }
    public override string Name { get; set; }
    public override double Price { get; set; }

    public Consumable(string name, string information, string negEffect, string posEffect, long duration, int max = -1) : base(name, 0)
    {
        Information = information;
        Max = max;
        NegEffect = negEffect;
        PosEffect = posEffect;
        Duration = duration;
    }
}