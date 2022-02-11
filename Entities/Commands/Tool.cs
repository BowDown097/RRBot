namespace RRBot.Entities.Commands;
public class Tool : Item
{
    public double GenericMin { get; set; }
    public double GenericMax { get; set; }
    public double Mult { get; set; }
    public override string Name { get; set; }
    public override double Price { get; set; }

    public Tool(string name, double price, double genericMin = 0, double genericMax = 0, double mult = 1)
        : base(name, price)
    {
        GenericMin = genericMin;
        GenericMax = genericMax;
        Mult = mult;
    }
}