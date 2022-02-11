namespace RRBot.Entities.Commands;
public class Consumable : Item
{
    public long Duration { get; set; }
    public string Information { get; set; }
    public string NegEffect { get; set; }
    public string PosEffect { get; set; }
    public override string Name { get; set; }
    public override double Price { get; set; }

    public Consumable(string name, string information, string negEffect, string posEffect, double price, long duration)
        : base(name, price)
    {
        Information = information;
        NegEffect = negEffect;
        PosEffect = posEffect;
        Duration = duration;
    }
}