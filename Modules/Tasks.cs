using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Google.Cloud.Firestore;
using RRBot.Preconditions;
using RRBot.Systems;

namespace RRBot.Modules
{
    public class Tasks : ModuleBase<SocketCommandContext>
    {
        private async Task GenericTask(string itemType, string activity, string thing, object cooldown)
        {
            Random random = new Random();
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            List<string> items = snap.GetValue<List<string>>("items");
            float cash = snap.GetValue<float>("cash");

            string item = CashSystem.GetBestItem(items, itemType);
            int numMined = random.Next(32, 65); // default for wooden
            if (item.StartsWith("Stone", StringComparison.Ordinal)) numMined = random.Next(65, 113);
            else if (item.StartsWith("Iron", StringComparison.Ordinal)) numMined = random.Next(113, 161);
            else if (item.StartsWith("Diamond", StringComparison.Ordinal)) numMined = random.Next(161, 209);
            float cashGained = (float)(numMined * 2.5);
            await ReplyAsync($"{Context.User.Mention}, you {activity} {numMined} {thing} with your {item} and earned **${string.Format("{0:0.00}", cashGained)}**.");
            await CashSystem.SetCash(Context.User as IGuildUser, cash + cashGained);

            await doc.SetAsync(cooldown, SetOptions.MergeAll);
        }

        [Command("chop")]
        [Summary("Go chop some wood.")]
        [Remarks("``$chop``")]
        [RequireCooldown("chopCooldown", "you cannot chop wood for {0}.")]
        [RequireItem("Axe")]
        public async Task Chop() => await GenericTask("Axe", "chopped down", "trees", new { chopCooldown = Global.UnixTime(3600) });

        [Command("dig")]
        [Summary("Go digging.")]
        [Remarks("``$dig``")]
        [RequireCooldown("digCooldown", "you cannot go digging for {0}.")]
        [RequireItem("Shovel")]
        public async Task Dig() => await GenericTask("Shovel", "mined", "dirt", new { digCooldown = Global.UnixTime(3600) });

        [Command("farm")]
        [Summary("Go farming.")]
        [Remarks("``$farm``")]
        [RequireCooldown("farmCooldown", "you cannot farm for {0}.")]
        [RequireItem("Hoe")]
        public async Task Farm() => await GenericTask("Hoe", "farmed", "crops", new { farmCooldown = Global.UnixTime(3600) });

        [Command("hunt")]
        [Summary("Go hunting.")]
        [Remarks("``$hunt``")]
        [RequireCooldown("huntCooldown", "you cannot go hunting for {0}.")]
        [RequireItem("Sword")]
        public async Task Hunt() => await GenericTask("Sword", "hunted", "mobs", new { huntCooldown = Global.UnixTime(3600) });

        [Command("mine")]
        [Summary("Go mining.")]
        [Remarks("``$mine``")]
        [RequireCooldown("mineCooldown", "you cannot go mining for {0}.")]
        [RequireItem("Pickaxe")]
        public async Task Mine()
        {
            Random random = new Random();
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            List<string> items = snap.GetValue<List<string>>("items");
            float cash = snap.GetValue<float>("cash");

            string item = CashSystem.GetBestItem(items, "Pickaxe");
            int numMined = random.Next(32, 65);
            float cashGained = numMined * 4;
            if (item.StartsWith("Wooden", StringComparison.Ordinal))
            {
                await ReplyAsync($"{Context.User.Mention}, you mined {numMined} stone with your {item} and earned **${string.Format("{0:0.00}", cashGained)}**.");
            }
            else if (item.StartsWith("Stone", StringComparison.Ordinal))
            {
                cashGained *= 1.33f;
                await ReplyAsync($"{Context.User.Mention}, you mined {numMined} iron with your {item} and earned **${string.Format("{0:0.00}", cashGained)}**.");
            }
            else if (item.StartsWith("Iron", StringComparison.Ordinal))
            {
                cashGained *= 1.66f;
                await ReplyAsync($"{Context.User.Mention}, you mined {numMined} diamonds with your {item} and earned **${string.Format("{0:0.00}", cashGained)}**.");
            }
            else if (item.StartsWith("Diamond", StringComparison.Ordinal))
            {
                cashGained *= 2;
                await ReplyAsync($"{Context.User.Mention}, you mined {numMined} obsidian with your {item} and earned **${string.Format("{0:0.00}", cashGained)}**.");
            }
            await CashSystem.SetCash(Context.User as IGuildUser, cash + cashGained);

            await doc.SetAsync(new { mineCooldown = Global.UnixTime(3600) }, SetOptions.MergeAll);
        }
    }
}
