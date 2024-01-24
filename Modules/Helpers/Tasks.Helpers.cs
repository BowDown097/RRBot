namespace RRBot.Modules;
public partial class Tasks
{
    private async Task GenericTask(string toolType, string activity, string thing, string cooldown, long duration)
    {
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        string tool = ItemSystem.GetBestTool(user.Tools, toolType);
        int numMined = 0;

        if (tool.StartsWith("Wooden"))
            numMined = RandomUtil.Next(Constants.GenericTaskWoodMin, Constants.GenericTaskWoodMax);
        else if (tool.StartsWith("Stone"))
            numMined = RandomUtil.Next(Constants.GenericTaskStoneMin, Constants.GenericTaskStoneMax);
        else if (tool.StartsWith("Iron"))
            numMined = RandomUtil.Next(Constants.GenericTaskIronMin, Constants.GenericTaskIronMax);
        else if (tool.StartsWith("Diamond"))
            numMined = RandomUtil.Next(Constants.GenericTaskDiamondMin, Constants.GenericTaskDiamondMax);
        else if (tool.StartsWith("Netherite"))
            numMined = RandomUtil.Next(Constants.GenericTaskNetheriteMin, Constants.GenericTaskNetheriteMax);

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

        decimal cashGained = numMined * 2.5m;
        decimal totalCash = user.Cash + cashGained;

        user.AddToStats(new Dictionary<string, string>
        {
            { "Tasks Done", "1" },
            { "Money Gained from Tasks", cashGained.ToString("C2") }
        });

        await user.SetCash(Context.User, totalCash, Context.Channel, $"You {activity} {numMined} {thing} with your {tool} and earned **{cashGained:C2}**.\nBalance: {totalCash:C2}");
        await user.SetCooldown(cooldown, duration, Context.Guild, Context.User);
        await MongoManager.UpdateObjectAsync(user);
    }
}