namespace RRBot.Modules;
[Summary("The best way to earn money by far, at least for those lucky or rich enough to get themselves a tool.")]
public class Tasks : ModuleBase<SocketCommandContext>
{
    [Command("chop")]
    [Summary("Go chop some wood.")]
    [RequireCooldown("ChopCooldown", "You cannot chop wood for {0}.")]
    [RequireTool("Axe")]
    public async Task Chop() => await GenericTask("Axe", "chopped down", "trees", "ChopCooldown", Constants.CHOP_COOLDOWN);

    [Command("dig")]
    [Summary("Go digging.")]
    [RequireCooldown("DigCooldown", "You cannot go digging for {0}.")]
    [RequireTool("Shovel")]
    public async Task Dig() => await GenericTask("Shovel", "mined", "dirt", "DigCooldown", Constants.DIG_COOLDOWN);

    [Command("farm")]
    [Summary("Go farming.")]
    [RequireCooldown("FarmCooldown", "You cannot farm for {0}.")]
    [RequireTool("Hoe")]
    public async Task Farm() => await GenericTask("Hoe", "farmed", "crops", "FarmCooldown", Constants.FARM_COOLDOWN);

    [Command("fish")]
    [Summary("Go fishing.")]
    [RequireCooldown("FishCooldown", "You cannot fish for {0}.")]
    [RequireTool("Fishing Rod")]
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
                user.Tools.Remove("Fishing Rod");
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
    [RequireCooldown("HuntCooldown", "You cannot go hunting for {0}.")]
    [RequireTool("Sword")]
    public async Task Hunt() => await GenericTask("Sword", "hunted", "mobs", "HuntCooldown", Constants.HUNT_COOLDOWN);

    [Command("mine")]
    [Summary("Go mining.")]
    [RequireCooldown("MineCooldown", "You cannot go mining for {0}.")]
    [RequireTool("Pickaxe")]
    public async Task Mine()
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        string toolName = ItemSystem.GetBestTool(user.Tools, "Pickaxe");
        Tool tool = ItemSystem.GetItem(toolName) as Tool;

        int numMined = RandomUtil.Next(32, 65);
        if (user.Perks.ContainsKey("Enchanter"))
        {
            int randNum = RandomUtil.Next(100);
            if (randNum == 1 || randNum == 2)
            {
                user.Tools.Remove(toolName);
                await Context.User.NotifyAsync(Context.Channel, $"Your {toolName} broke into pieces as soon as you tried to use it. You made no money.");
                return;
            }

            numMined = (int)(numMined * 1.2);
        }

        double cashGained = numMined * 4 * tool.Mult;
        double totalCash = user.Cash + cashGained;
        string response = toolName switch
        {
            "Wooden Pickaxe" => $"You mined {numMined} stone with your {toolName} and earned **{cashGained:C2}**.\nBalance: {totalCash:C2}",
            "Stone Pickaxe" => $"You mined {numMined} iron with your {toolName} and earned **{cashGained:C2}**.\nBalance: {totalCash:C2}",
            "Iron Pickaxe" => $"You mined {numMined} diamonds with your {toolName} and earned **{cashGained:C2}**.\nBalance: {totalCash:C2}",
            "Diamond Pickaxe" => $"You mined {numMined} obsidian with your {toolName} and earned **{cashGained:C2}**.\nBalance: {totalCash:C2}",
            _ => ""
        };

        await Context.User.NotifyAsync(Context.Channel, response);
        user.AddToStats(new()
        {
            { "Tasks Done", "1" },
            { "Money Gained from Tasks", cashGained.ToString("C2") }
        });
        await user.SetCash(Context.User, totalCash);
        user.MineCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.MINE_COOLDOWN);
    }

    private async Task GenericTask(string toolType, string activity, string thing, string cooldown, double duration)
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        string tool = ItemSystem.GetBestTool(user.Tools, toolType);
        int numMined = 0;

        if (tool.StartsWith("Wooden"))
            numMined = RandomUtil.Next(Constants.GENERIC_TASK_WOOD_MIN, Constants.GENERIC_TASK_WOOD_MAX); // default for wooden
        else if (tool.StartsWith("Stone"))
            numMined = RandomUtil.Next(Constants.GENERIC_TASK_STONE_MIN, Constants.GENERIC_TASK_STONE_MAX);
        else if (tool.StartsWith("Iron"))
            numMined = RandomUtil.Next(Constants.GENERIC_TASK_IRON_MIN, Constants.GENERIC_TASK_IRON_MAX);
        else if (tool.StartsWith("Diamond"))
            numMined = RandomUtil.Next(Constants.GENERIC_TASK_DIAMOND_MIN, Constants.GENERIC_TASK_DIAMOND_MAX);

        if (user.Perks.ContainsKey("Enchanter"))
        {
            int randNum = RandomUtil.Next(100);
            if (randNum == 1 || randNum == 2)
            {
                user.Tools.Remove(tool);
                await Context.User.NotifyAsync(Context.Channel, $"Your {tool} broke into pieces as soon as you tried to use it. You made no money.");
                return;
            }

            numMined = (int)(numMined * 1.2);
        }

        double cashGained = numMined * 2.5;
        double totalCash = user.Cash + cashGained;

        await Context.User.NotifyAsync(Context.Channel, $"You {activity} {numMined} {thing} with your {tool} and earned **{cashGained:C2}**." +
            $"\nBalance: {totalCash:C2}");

        await user.SetCash(Context.User, totalCash);
        user.AddToStats(new()
        {
            { "Tasks Done", "1" },
            { "Money Gained from Tasks", cashGained.ToString("C2") }
        });
        user[cooldown] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(duration);
    }
}