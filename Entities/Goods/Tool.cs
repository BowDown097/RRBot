namespace RRBot.Entities.Goods;
public class Tool(string name, decimal price, decimal genericMin = 0, decimal genericMax = 0, decimal mult = 1)
    : Item(name, price)
{
    public decimal GenericMin { get; } = genericMin;
    public decimal GenericMax { get; } = genericMax;
    public decimal Mult { get; } = mult;
    public override string Name { get; protected init; }
    public override decimal Price { get; protected init; }
}