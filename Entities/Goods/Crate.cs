namespace RRBot.Entities.Goods;
public class Crate(
    string name, decimal price, int tier, int consumableCount = 0, int toolCount = 0, decimal cash = 0) : Item
{
    public decimal Cash => cash;
    public int ConsumableCount => consumableCount;
    private int Tier => tier;
    public int ToolCount => toolCount;
    public override string Name => name;
    public override decimal Price => price;

    public List<Item> Open(DbUser user)
    {
        List<Item> items = [];

        if (Tier > 0)
        {
            int ammoRoll = RandomUtil.Next(Tier + 1);
            if (ammoRoll > 0)
            {
                Ammo randomAmmo = RandomUtil.GetRandomElement(
                    Constants.Ammo.Where(a => a.CrateMultiplier * ammoRoll >= 1));
                for (int i = 0; i < randomAmmo.CrateMultiplier * ammoRoll; i++)
                    items.Add(randomAmmo);
            }
        }

        for (int i = 0; i < ConsumableCount; i++)
            items.Add(RandomUtil.GetRandomElement(Constants.Consumables));

        Tool[] availableTools = [..Constants.Tools.Where(
            t => Tier == 4 ? !items.Contains(t) : !items.Contains(t) &&!t.Name.StartsWith("Netherite"))];
        for (int i = 0; i < ToolCount; i++)
            items.Add(RandomUtil.GetRandomElement(availableTools));

        int weaponRolls = RandomUtil.Next(Tier + 1);
        for (int i = 0; i < weaponRolls; i++)
        {
            int weaponDropRoll = RandomUtil.Next(1, 101);
            Weapon[] availableWeapons = [..Constants.Weapons.Where(
                w => w.DropChance > weaponDropRoll && w.InsideCrates.Contains(Name))];

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