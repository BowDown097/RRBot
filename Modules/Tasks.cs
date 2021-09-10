using Discord;
using Discord.Commands;
using Google.Cloud.Firestore;
using RRBot.Extensions;
using RRBot.Preconditions;
using RRBot.Systems;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace RRBot.Modules
{
    [Summary("The best way to earn money by far, at least for those lucky or rich enough to get themselves an item.")]
    public class Tasks : ModuleBase<SocketCommandContext>
    {
        private static readonly Random random = new();

        private async Task GenericTask(string itemType, string activity, string thing, object cooldown)
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            List<string> items = snap.GetValue<List<string>>("items");
            double cash = snap.GetValue<double>("cash");

            string item = Items.GetBestItem(items, itemType);

            int numMined = random.Next(Constants.GENERIC_TASK_WOOD_MIN, Constants.GENERIC_TASK_WOOD_MAX); // default for wooden
            if (item.StartsWith("Stone", StringComparison.Ordinal))
                numMined = random.Next(Constants.GENERIC_TASK_STONE_MIN, Constants.GENERIC_TASK_STONE_MAX);
            else if (item.StartsWith("Iron", StringComparison.Ordinal))
                numMined = random.Next(Constants.GENERIC_TASK_IRON_MIN, Constants.GENERIC_TASK_IRON_MAX);
            else if (item.StartsWith("Diamond", StringComparison.Ordinal))
                numMined = random.Next(Constants.GENERIC_TASK_DIAMOND_MIN, Constants.GENERIC_TASK_DIAMOND_MAX);

            if (snap.TryGetValue("perks", out Dictionary<string, long> perks) && perks.Keys.Contains("Enchanter"))
            {
                int randNum = random.Next(100);
                if (randNum == 1 || randNum == 2)
                {
                    perks.Remove(item);
                    Dictionary<string, object> newPerks = new() { { "perks", perks } };
                    await doc.UpdateAsync(newPerks);
                    await Context.User.NotifyAsync(Context.Channel, $"Your {item} broke into pieces as soon as you tried to use it. You made no money.");
                    return;
                }

                numMined = (int)(numMined * 1.1);
            }

            double cashGained = numMined * 2.5;
            double totalCash = cash + cashGained;

            await Context.User.NotifyAsync(Context.Channel, $"You {activity} {numMined} {thing} with your {item} and earned **{cashGained:C2}**." +
                $"\nBalance: {totalCash:C2}");

            await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, totalCash);
            await Context.User.AddToStatsAsync(CultureInfo.CurrentCulture, Context.Guild, new Dictionary<string, string>
            {
                { "Tasks Done", "1" },
                { "Money Gained from Tasks", cashGained.ToString("C2") }
            });
            await doc.SetAsync(cooldown, SetOptions.MergeAll);
        }

        [Command("chop")]
        [Summary("Go chop some wood.")]
        [Remarks("$chop")]
        [RequireCooldown("chopCooldown", "You cannot chop wood for {0}.")]
        [RequireItem("Axe")]
        public async Task Chop()
        {
            await GenericTask("Axe", "chopped down", "trees",
                new { chopCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.CHOP_COOLDOWN) });
        }

        [Command("dig")]
        [Summary("Go digging.")]
        [Remarks("$dig")]
        [RequireCooldown("digCooldown", "You cannot go digging for {0}.")]
        [RequireItem("Shovel")]
        public async Task Dig()
        {
            await GenericTask("Shovel", "mined", "dirt",
                new { digCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.DIG_COOLDOWN) });
        }

        [Command("farm")]
        [Summary("Go farming.")]
        [Remarks("$farm")]
        [RequireCooldown("farmCooldown", "You cannot farm for {0}.")]
        [RequireItem("Hoe")]
        public async Task Farm()
        {
            await GenericTask("Hoe", "farmed", "crops",
                new { farmCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.FARM_COOLDOWN) });
        }

        [Command("fish")]
        [Summary("Go fishing.")]
        [Remarks("$fish")]
        [RequireCooldown("fishCooldown", "You cannot fish for {0}.")]
        [RequireItem("Fishing Rod")]
        public async Task Fish()
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            double cash = snap.GetValue<double>("cash");
            KeyValuePair<string, double> fish = Constants.FISH.ElementAt(random.Next(Constants.FISH.Count));
            int numCaught = random.Next(7, 15);

            if (snap.TryGetValue("perks", out Dictionary<string, long> perks) && perks.Keys.Contains("Enchanter"))
            {
                int randNum = random.Next(100);
                if (randNum == 1 || randNum == 2)
                {
                    List<string> items = snap.GetValue<List<string>>("items");
                    items.Remove("Fishing Rod");
                    Dictionary<string, object> newItems = new() { { "items", items } };
                    await doc.UpdateAsync(newItems);
                    await Context.User.NotifyAsync(Context.Channel, "Your Fishing Rod broke into pieces as soon as you tried to use it. You made no money.");
                    return;
                }

                numCaught = (int)(numCaught * 1.1);
            }

            double cashGained = numCaught * fish.Value;
            double totalCash = cash + cashGained;

            await Context.User.NotifyAsync(Context.Channel, $"You caught {numCaught} {fish.Key} with your rod and earned **{cashGained:C2}**.\nBalance: {totalCash:C2}");

            await Context.User.AddToStatsAsync(CultureInfo.CurrentCulture, Context.Guild, new Dictionary<string, string>
            {
                { "Tasks Done", "1" },
                { "Money Gained From Tasks", cashGained.ToString("C2") }
            });
            await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, totalCash);
            await doc.SetAsync(new { fishCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.FISH_COOLDOWN) },
                SetOptions.MergeAll);
        }

        [Command("hunt")]
        [Summary("Go hunting.")]
        [Remarks("$hunt")]
        [RequireCooldown("huntCooldown", "You cannot go hunting for {0}.")]
        [RequireItem("Sword")]
        public async Task Hunt()
        {
            await GenericTask("Sword", "hunted", "mobs",
                new { huntCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.HUNT_COOLDOWN) });
        }

        [Command("mine")]
        [Summary("Go mining.")]
        [Remarks("$mine")]
        [RequireCooldown("mineCooldown", "You cannot go mining for {0}.")]
        [RequireItem("Pickaxe")]
        public async Task Mine()
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            List<string> items = snap.GetValue<List<string>>("items");
            double cash = snap.GetValue<double>("cash");

            string item = Items.GetBestItem(items, "Pickaxe");

            int numMined = random.Next(32, 65);
            if (snap.TryGetValue("perks", out Dictionary<string, long> perks) && perks.Keys.Contains("Enchanter"))
            {
                int randNum = random.Next(100);
                if (randNum == 1 || randNum == 2)
                {
                    items.Remove(item);
                    Dictionary<string, object> newItems = new() { { "items", items } };
                    await doc.UpdateAsync(newItems);
                    await Context.User.NotifyAsync(Context.Channel, $"Your {item} broke into pieces as soon as you tried to use it. You made no money.");
                    return;
                }

                numMined = (int)(numMined * 1.1);
            }

            double cashGained = numMined * 4;
            double totalCash = cash + cashGained;

            if (item.StartsWith("Wooden", StringComparison.Ordinal))
            {
                await Context.User.NotifyAsync(Context.Channel, $"You mined {numMined} stone with your {item} and earned **{cashGained:C2}**.\nBalance: {totalCash:C2}");
            }
            else if (item.StartsWith("Stone", StringComparison.Ordinal))
            {
                cashGained *= Constants.MINE_STONE_MULTIPLIER;
                totalCash = cash + cashGained;
                await Context.User.NotifyAsync(Context.Channel, $"You mined {numMined} iron with your {item} and earned **{cashGained:C2}**.\nBalance: {totalCash:C2}");
            }
            else if (item.StartsWith("Iron", StringComparison.Ordinal))
            {
                cashGained *= Constants.MINE_IRON_MULTIPLIER;
                totalCash = cash + cashGained;
                await Context.User.NotifyAsync(Context.Channel, $"You mined {numMined} diamonds with your {item} and earned **{cashGained:C2}**.\nBalance: {totalCash:C2}");
            }
            else if (item.StartsWith("Diamond", StringComparison.Ordinal))
            {
                cashGained *= Constants.MINE_DIAMOND_MULTIPLIER;
                totalCash = cash + cashGained;
                await Context.User.NotifyAsync(Context.Channel, $"You mined {numMined} obsidian with your {item} and earned **{cashGained:C2}**.\nBalance: {totalCash:C2}");
            }

            await Context.User.AddToStatsAsync(CultureInfo.CurrentCulture, Context.Guild, new Dictionary<string, string>
            {
                { "Tasks Done", "1" },
                { "Money Gained from Tasks", cashGained.ToString("C2") }
            });
            await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, totalCash);
            await doc.SetAsync(new { mineCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.MINE_COOLDOWN) },
                SetOptions.MergeAll);
        }
    }
}
