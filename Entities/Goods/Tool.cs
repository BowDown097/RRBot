namespace RRBot.Entities.Goods;
public class Tool(
    string name, decimal price, decimal genericMin = 0, decimal genericMax = 0, decimal mult = 1) : Item
{
    public decimal GenericMin => genericMin;
    public decimal GenericMax => genericMax;
    public decimal Mult => mult;
    public override string Name => name;
    public override decimal Price => price;
}