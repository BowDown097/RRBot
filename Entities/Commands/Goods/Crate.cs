namespace RRBot.Entities.Commands.Goods;
public class Crate : Item
{
    public double Cash { get; set; }
    public int ConsumableCount { get; set; }
    public int ToolCount { get; set; }
    public override string Name { get; set; }
    public override double Price { get; set; }

    public Crate(string name, double price, int consumableCount = 0, int toolCount = 0, double cash = 0) : base(name, price)
    {
        Cash = cash;
        ConsumableCount = consumableCount;
        ToolCount = toolCount;
    }

    public List<Item> Open(DbUser user)
    {
        List<Item> items = new();
        for (int i = 0; i < ConsumableCount; i++)
            items.Add(ItemSystem.Consumables[RandomUtil.Next(ItemSystem.Consumables.Length)]);
        for (int i = 0; i < ToolCount; i++)
            items.Add(ItemSystem.Tools.Where(t => !items.Contains(t)).ElementAt(RandomUtil.Next(ItemSystem.Tools.Length)));

        foreach (Item item in items.ToList().Where(i => user.Tools.Contains(i.Name)))
        {
            items.Remove(item);
            for (int i = 0; i < 3; i++)
                items.Add(ItemSystem.Consumables[RandomUtil.Next(ItemSystem.Consumables.Length)]);
        }

        return items;
    }
}