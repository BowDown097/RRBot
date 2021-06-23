using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Google.Cloud.Firestore;
using RRBot.Extensions;

namespace RRBot.Systems
{
    public static class CashSystem
    {
        private static readonly Dictionary<string, int> rankings = new Dictionary<string, int>
        {
            { "Wooden", 0 },
            { "Stone", 1 },
            { "Iron", 2 },
            { "Diamond", 3 }
        };

        public static readonly Dictionary<int, string> itemMap = new Dictionary<int, string>
        {
            { 0, "Wooden Pickaxe" },
            { 1, "Stone Pickaxe" },
            { 2, "Iron Pickaxe" },
            { 3, "Diamond Pickaxe" },
            { 4, "Wooden Sword" },
            { 5, "Stone Sword" },
            { 6, "Iron Sword" },
            { 7, "Diamond Sword" },
            { 8, "Wooden Shovel" },
            { 9, "Stone Shovel" },
            { 10, "Iron Shovel" },
            { 11, "Diamond Shovel" },
            { 12, "Wooden Axe" },
            { 13, "Stone Axe" },
            { 14, "Iron Axe" },
            { 15, "Diamond Axe" },
            { 16, "Wooden Hoe" },
            { 17, "Stone Hoe" },
            { 18, "Iron Hoe" },
            { 19, "Diamond Hoe" },
        };

        public static async Task<float> CashFromString(IGuildUser user, string cashStr)
        {
            float.TryParse(cashStr, out float cash);
            if (cashStr.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                DocumentReference doc = Program.database.Collection($"servers/{user.GuildId}/users").Document(user.Id.ToString());
                DocumentSnapshot snap = await doc.GetSnapshotAsync();
                cash = snap.GetValue<float>("cash");
            }

            return cash;
        }
        public static string GetBestItem(List<string> items, string type)
        {
            List<string> itemsOfType = items.Where(item => item.EndsWith(type, StringComparison.Ordinal)).ToList();
            return itemsOfType.Count > 0
                ? itemsOfType.OrderByDescending(item => rankings[item.Replace(type, string.Empty).Trim()]).First()
                : string.Empty;
        }

        public static async Task<string> RandomItem(IGuildUser user, Random random) 
        {
            Dictionary<int, string> newMap = itemMap;
            DocumentReference userDoc = Program.database.Collection($"servers/{user.GuildId}/users").Document(user.Id.ToString());
            DocumentSnapshot snap = await userDoc.GetSnapshotAsync();
            if (snap.TryGetValue("items", out List<string> items)) newMap = newMap.Where(kvp => !items.Contains(kvp.Value)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return items.Count <= newMap.Count ? newMap[random.Next(newMap.Count)] : string.Empty;
        }

        public static async Task RewardItem(IGuildUser user, string item)
        {
            if (user.IsBot) return;

            DocumentReference userDoc = Program.database.Collection($"servers/{user.GuildId}/users").Document(user.Id.ToString());
            DocumentSnapshot snap = await userDoc.GetSnapshotAsync();

            if (!snap.TryGetValue("items", out List<string> usrItems)) usrItems = new List<string>();
            if (!usrItems.Contains(item)) usrItems.Add(item);
            await userDoc.SetAsync(new { items = usrItems }, SetOptions.MergeAll);
        }

        public static async Task SetCash(IGuildUser user, float amount)
        {
            if (user.IsBot) return;
            if (amount < 0) amount = 0;

            amount = (float)Math.Round(amount, 2);
            DocumentReference userDoc = Program.database.Collection($"servers/{user.GuildId}/users").Document(user.Id.ToString());
            await userDoc.SetAsync(new { cash = amount }, SetOptions.MergeAll);

            DocumentReference ranksDoc = Program.database.Collection($"servers/{user.GuildId}/config").Document("ranks");
            DocumentSnapshot snap = await ranksDoc.GetSnapshotAsync();
            if (snap.Exists)
            {
                foreach (KeyValuePair<string, object> kvp in snap.ToDictionary().Where(kvp => kvp.Key.EndsWith("Id", StringComparison.Ordinal)))
                {
                    float neededCash = snap.GetValue<float>(kvp.Key.Replace("Id", "Cost"));
                    ulong roleId = Convert.ToUInt64(kvp.Value);
                    if (amount >= neededCash && !user.RoleIds.Contains(roleId)) await user.AddRoleAsync(roleId);
                    else if (amount <= neededCash && user.RoleIds.Contains(roleId)) await user.RemoveRoleAsync(roleId);
                }
            }
        }

        public static async Task TryMessageReward(SocketCommandContext context)
        {
            DocumentReference doc = Program.database.Collection($"servers/{context.Guild.Id}/users").Document(context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            if (snap.TryGetValue("timeTillCash", out long time) && time <= DateTimeOffset.UtcNow.ToUnixTimeSeconds()) 
                await SetCash(context.User as IGuildUser, snap.GetValue<float>("cash") + 10);

            await doc.SetAsync(new { timeTillCash = DateTimeOffset.UtcNow.ToUnixTimeSeconds(TimeSpan.FromMinutes(1).TotalSeconds) }, SetOptions.MergeAll);
        }
    }
}
