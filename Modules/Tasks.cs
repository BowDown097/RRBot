namespace RRBot.Modules;
[Summary("The best way to earn money by far, at least for those lucky or rich enough to get themselves a tool.")]
public class Tasks : ModuleBase<SocketCommandContext>
{
    #region Commands
    [Command("chop")]
    [Summary("Go chop some wood.")]
    [RequireCooldown("ChopCooldown", "You cannot chop wood for {0}.")]
    [RequireTool("Axe")]
    public async Task Chop() => await GenericTask("Axe", "chopped down", "trees", "ChopCooldown", Constants.ChopCooldown);

    [Command("dig")]
    [Summary("Go digging.")]
    [RequireCooldown("DigCooldown", "You cannot go digging for {0}.")]
    [RequireTool("Shovel")]
    public async Task Dig() => await GenericTask("Shovel", "mined", "dirt", "DigCooldown", Constants.DigCooldown);

    [Command("farm")]
    [Summary("Go farming.")]
    [RequireCooldown("FarmCooldown", "You cannot farm for {0}.")]
    [RequireTool("Hoe")]
    public async Task Farm() => await GenericTask("Hoe", "farmed", "crops", "FarmCooldown", Constants.FarmCooldown);

    [Command("fish")]
    [Summary("Go fishing.")]
    [RequireCooldown("FishCooldown", "You cannot fish for {0}.")]
    [RequireTool("Fishing Rod")]
    public async Task Fish()
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        KeyValuePair<string, double> fish = Constants.Fish.ElementAt(RandomUtil.Next(Constants.Fish.Count));
        int numCaught = RandomUtil.Next(7, 15);

        if (user.Perks.ContainsKey("Enchanter"))
        {
            int randNum = RandomUtil.Next(100);
            if (randNum is 1 or 2)
            {
                user.Tools.Remove("Fishing Rod");
                await Context.User.NotifyAsync(Context.Channel, "Your Fishing Rod broke into pieces as soon as you tried to use it. You made no money.");
                return;
            }

            numCaught = (int)(numCaught * 1.2);
        }

        double cashGained = numCaught * fish.Value;
        double totalCash = user.Cash + cashGained;

        if (RandomUtil.NextDouble(1, 101) < Constants.FishCoconutOdds)
            await ItemSystem.GiveCollectible("Coconut", Context.Channel, user);

        user.AddToStats(new Dictionary<string, string>
        {
            { "Tasks Done", "1" },
            { "Money Gained from Tasks", cashGained.ToString("C2") }
        });

        await user.SetCash(Context.User, totalCash, Context.Channel, $"You caught {numCaught} {fish.Key} with your rod and earned **{cashGained:C2}**.\nBalance: {totalCash:C2}");
        await user.SetCooldown("FishCooldown", Constants.FishCooldown, Context.Guild, Context.User);
    }

    [Command("hunt")]
    [Summary("Go hunting.")]
    [RequireCooldown("HuntCooldown", "You cannot go hunting for {0}.")]
    [RequireTool("Sword")]
    public async Task Hunt() => await GenericTask("Sword", "hunted", "mobs", "HuntCooldown", Constants.HuntCooldown);

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
            if (randNum is 1 or 2)
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

        user.AddToStats(new Dictionary<string, string>
        {
            { "Tasks Done", "1" },
            { "Money Gained from Tasks", cashGained.ToString("C2") }
        });

        await user.SetCash(Context.User, totalCash, Context.Channel, response);
        await user.SetCooldown("MineCooldown", Constants.MineCooldown, Context.Guild, Context.User);
    }
    #endregion

    #region Helpers
    private async Task GenericTask(string toolType, string activity, string thing, string cooldown, long duration)
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        string tool = ItemSystem.GetBestTool(user.Tools, toolType);
        int numMined = 0;

        if (tool.StartsWith("Wooden"))
            numMined = RandomUtil.Next(Constants.GenericTaskWoodMin, Constants.GenericTaskWoodMax); // default for wooden
        else if (tool.StartsWith("Stone"))
            numMined = RandomUtil.Next(Constants.GenericTaskStoneMin, Constants.GenericTaskStoneMax);
        else if (tool.StartsWith("Iron"))
            numMined = RandomUtil.Next(Constants.GenericTaskIronMin, Constants.GenericTaskIronMax);
        else if (tool.StartsWith("Diamond"))
            numMined = RandomUtil.Next(Constants.GenericTaskDiamondMin, Constants.GenericTaskDiamondMax);

        if (user.Perks.ContainsKey("Enchanter"))
        {
            int randNum = RandomUtil.Next(100);
            if (randNum is 1 or 2)
            {
                user.Tools.Remove(tool);
                await Context.User.NotifyAsync(Context.Channel, $"Your {tool} broke into pieces as soon as you tried to use it. You made no money.");
                return;
            }

            numMined = (int)(numMined * 1.2);
        }

        double cashGained = numMined * 2.5;
        double totalCash = user.Cash + cashGained;

        user.AddToStats(new Dictionary<string, string>
        {
            { "Tasks Done", "1" },
            { "Money Gained from Tasks", cashGained.ToString("C2") }
        });

        await user.SetCash(Context.User, totalCash, Context.Channel, $"You {activity} {numMined} {thing} with your {tool} and earned **{cashGained:C2}**.\nBalance: {totalCash:C2}");
        await user.SetCooldown(cooldown, duration, Context.Guild, Context.User);
    }
    #endregion
}