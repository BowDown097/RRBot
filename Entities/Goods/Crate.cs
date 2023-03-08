namespace RRBot.Entities.Goods;
public class Crate : Item
{
    public decimal Cash { get; }
    public int ConsumableCount { get; }
    private int Tier { get; }
    public int ToolCount { get; }
    public override string Name { get; protected init; }
    public override decimal Price { get; protected init; }

    public Crate(string name, decimal price, int tier, int consumableCount = 0, int toolCount = 0, decimal cash = 0)
        : base(name, price)
    {
        Cash = cash;
        ConsumableCount = consumableCount;
        Tier = tier;
        ToolCount = toolCount;
    }

    public List<Item> Open(DbUser user)
    {
        List<Item> items = new();

        if (Tier > 0)
        {
            int ammoRoll = RandomUtil.Next(Tier + 1);
            if (ammoRoll > 0)
            {
                Ammo randomAmmo = RandomUtil.GetRandomElement(Constants.Ammo.Where(a => a.CrateMultiplier * ammoRoll >= 1));
                for (int i = 0; i < randomAmmo.CrateMultiplier * ammoRoll; i++)
                    items.Add(randomAmmo);
            }
        }

        for (int i = 0; i < ConsumableCount; i++)
            items.Add(RandomUtil.GetRandomElement(Constants.Consumables));

        Tool[] availableTools = Constants.Tools
            .Where(t => Tier == 4 ? !items.Contains(t) : !items.Contains(t) && !t.Name.StartsWith("Netherite"))
            .ToArray();
        for (int i = 0; i < ToolCount; i++)
            items.Add(RandomUtil.GetRandomElement(availableTools));

        int weaponRolls = RandomUtil.Next(Tier + 1);
        for (int i = 0; i < weaponRolls; i++)
        {
            int weaponDropRoll = RandomUtil.Next(1, 101);
            Weapon[] availableWeapons = Constants.Weapons
                .Where(w => w.DropChance > weaponDropRoll && w.InsideCrates.Contains(Name))
                .ToArray();

            if (availableWeapons.Length > 0)
            {
                items.Add(RandomUtil.GetRandomElement(availableWeapons));
                break;
            }
        }

        foreach (Item item in items.ToList().Where(i => user.Tools.Contains(i.Name)))
        {
            items.Remove(item);
            for (int i = 0; i < 3; i++)
                items.Add(RandomUtil.GetRandomElement(Constants.Consumables));
        }
        
        return items;
    }
}