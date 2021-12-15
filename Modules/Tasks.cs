namespace RRBot.Modules;
[Summary("The best way to earn money by far, at least for those lucky or rich enough to get themselves an item.")]
public class Tasks : ModuleBase<SocketCommandContext>
{
    private async Task GenericTask(string itemType, string activity, string thing, string cooldown, double duration)
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        string item = ItemSystem.GetBestItem(user.Items, itemType);
        int numMined = 0;

        if (item.StartsWith("Wooden"))
            numMined = RandomUtil.Next(Constants.GENERIC_TASK_WOOD_MIN, Constants.GENERIC_TASK_WOOD_MAX); // default for wooden
        else if (item.StartsWith("Stone"))
            numMined = RandomUtil.Next(Constants.GENERIC_TASK_STONE_MIN, Constants.GENERIC_TASK_STONE_MAX);
        else if (item.StartsWith("Iron"))
            numMined = RandomUtil.Next(Constants.GENERIC_TASK_IRON_MIN, Constants.GENERIC_TASK_IRON_MAX);
        else if (item.StartsWith("Diamond"))
            numMined = RandomUtil.Next(Constants.GENERIC_TASK_DIAMOND_MIN, Constants.GENERIC_TASK_DIAMOND_MAX);

        if (user.Perks.ContainsKey("Enchanter"))
        {
            int randNum = RandomUtil.Next(100);
            if (randNum == 1 || randNum == 2)
            {
                user.Items.Remove(item);
                await Context.User.NotifyAsync(Context.Channel, $"Your {item} broke into pieces as soon as you tried to use it. You made no money.");
                return;
            }

            numMined = (int)(numMined * 1.2);
        }

        double cashGained = numMined * 2.5;
        double totalCash = user.Cash + cashGained;

        await Context.User.NotifyAsync(Context.Channel, $"You {activity} {numMined} {thing} with your {item} and earned **{cashGained:C2}**." +
            $"\nBalance: {totalCash:C2}");

        await user.SetCash(Context.User, totalCash);
        user.AddToStats(new()
        {
            { "Tasks Done", "1" },
            { "Money Gained from Tasks", cashGained.ToString("C2") }
        });
        user[cooldown] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(duration);
    }

    [Command("chop")]
    [Summary("Go chop some wood.")]
    [Remarks("$chop")]
    [RequireCooldown("ChopCooldown", "You cannot chop wood for {0}.")]
    [RequireItem("Axe")]
    public async Task Chop() => await GenericTask("Axe", "chopped down", "trees", "ChopCooldown", Constants.CHOP_COOLDOWN);

    [Command("dig")]
    [Summary("Go digging.")]
    [Remarks("$dig")]
    [RequireCooldown("DigCooldown", "You cannot go digging for {0}.")]
    [RequireItem("Shovel")]
    public async Task Dig() => await GenericTask("Shovel", "mined", "dirt", "DigCooldown", Constants.DIG_COOLDOWN);

    [Command("farm")]
    [Summary("Go farming.")]
    [Remarks("$farm")]
    [RequireCooldown("FarmCooldown", "You cannot farm for {0}.")]
    [RequireItem("Hoe")]
    public async Task Farm() => await GenericTask("Hoe", "farmed", "crops", "FarmCooldown", Constants.FARM_COOLDOWN);

    [Command("fish")]
    [Summary("Go fishing.")]
    [Remarks("$fish")]
    [RequireCooldown("FishCooldown", "You cannot fish for {0}.")]
    [RequireItem("Fishing Rod")]
    public async Task Fish()
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        KeyValuePair<string, double> fish = Constants.FISH.ElementAt(RandomUtil.Next(Constants.FISH.Count));
        int numCaught = RandomUtil.Next(7, 15);

        if (user.Perks.ContainsKey("Enchanter"))
        {
            int randNum = RandomUtil.Next(100);
            if (randNum == 1 || randNum == 2)
            {
                user.Items.Remove("Fishing Rod");
                await Context.User.NotifyAsync(Context.Channel, "Your Fishing Rod broke into pieces as soon as you tried to use it. You made no money.");
                return;
            }

            numCaught = (int)(numCaught * 1.2);
        }

        double cashGained = numCaught * fish.Value;
        double totalCash = user.Cash + cashGained;

        await Context.User.NotifyAsync(Context.Channel, $"You caught {numCaught} {fish.Key} with your rod and earned **{cashGained:C2}**.\nBalance: {totalCash:C2}");

        if (RandomUtil.NextDouble(1, 101) < Constants.FISH_COCONUT_ODDS)
        {
            cashGained += 3;
            totalCash += 3;
            await ReplyAsync("What's this? The fish came with a coconut! You sold it to some dude for **$3.00**.");
        }

        user.AddToStats(new()
        {
            { "Tasks Done", "1" },
            { "Money Gained from Tasks", cashGained.ToString("C2") }
        });
        await user.SetCash(Context.User, totalCash);
        user.FishCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.FISH_COOLDOWN);
    }

    [Command("hunt")]
    [Summary("Go hunting.")]
    [Remarks("$hunt")]
    [RequireCooldown("HuntCooldown", "You cannot go hunting for {0}.")]
    [RequireItem("Sword")]
    public async Task Hunt() => await GenericTask("Sword", "hunted", "mobs", "HuntCooldown", Constants.HUNT_COOLDOWN);

    [Command("mine")]
    [Summary("Go mining.")]
    [Remarks("$mine")]
    [RequireCooldown("MineCooldown", "You cannot go mining for {0}.")]
    [RequireItem("Pickaxe")]
    public async Task Mine()
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        string item = ItemSystem.GetBestItem(user.Items, "Pickaxe");

        int numMined = RandomUtil.Next(32, 65);
        if (user.Perks.ContainsKey("Enchanter"))
        {
            int randNum = RandomUtil.Next(100);
            if (randNum == 1 || randNum == 2)
            {
                user.Items.Remove(item);
                await Context.User.NotifyAsync(Context.Channel, $"Your {item} broke into pieces as soon as you tried to use it. You made no money.");
                return;
            }

            numMined = (int)(numMined * 1.2);
        }

        double cashGained = numMined * 4;
        double totalCash = user.Cash + cashGained;

        if (item.StartsWith("Wooden"))
        {
            await Context.User.NotifyAsync(Context.Channel, $"You mined {numMined} stone with your {item} and earned **{cashGained:C2}**.\nBalance: {totalCash:C2}");
        }
        else if (item.StartsWith("Stone"))
        {
            cashGained *= Constants.MINE_STONE_MULTIPLIER;
            totalCash = user.Cash + cashGained;
            await Context.User.NotifyAsync(Context.Channel, $"You mined {numMined} iron with your {item} and earned **{cashGained:C2}**.\nBalance: {totalCash:C2}");
        }
        else if (item.StartsWith("Iron"))
        {
            cashGained *= Constants.MINE_IRON_MULTIPLIER;
            totalCash = user.Cash + cashGained;
            await Context.User.NotifyAsync(Context.Channel, $"You mined {numMined} diamonds with your {item} and earned **{cashGained:C2}**.\nBalance: {totalCash:C2}");
        }
        else if (item.StartsWith("Diamond"))
        {
            cashGained *= Constants.MINE_DIAMOND_MULTIPLIER;
            totalCash = user.Cash + cashGained;
            await Context.User.NotifyAsync(Context.Channel, $"You mined {numMined} obsidian with your {item} and earned **{cashGained:C2}**.\nBalance: {totalCash:C2}");
        }

        user.AddToStats(new()
        {
            { "Tasks Done", "1" },
            { "Money Gained from Tasks", cashGained.ToString("C2") }
        });
        await user.SetCash(Context.User, totalCash);
        user.MineCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.MINE_COOLDOWN);
    }
}