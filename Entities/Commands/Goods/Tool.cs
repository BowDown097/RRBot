namespace RRBot.Entities.Commands.Goods;
public class Tool : Item
{
    public decimal GenericMin { get; }
    public decimal GenericMax { get; }
    public decimal Mult { get; }
    public override string Name { get; protected init; }
    public override decimal Price { get; protected init; }

    public Tool(string name, decimal price, decimal genericMin = 0, decimal genericMax = 0, decimal mult = 1)
        : base(name, price)
    {
        GenericMin = genericMin;
        GenericMax = genericMax;
        Mult = mult;
    }
}