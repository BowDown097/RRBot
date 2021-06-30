using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Google.Cloud.Firestore;

namespace RRBot.Systems
{
    public static class Items
    {
        private static readonly Dictionary<string, int> rankings = new Dictionary<string, int>
        {
            { "Wooden", 0 },
            { "Stone", 1 },
            { "Iron", 2 },
            { "Diamond", 3 }
        };

        public static readonly string[] items = 
        {
            "Wooden Pickaxe", "Stone Pickaxe", "Iron Pickaxe", "Diamond Pickaxe",
            "Wooden Sword", "Stone Sword", "Iron Sword", "Diamond Sword",
            "Wooden Shovel", "Stone Shovel", "Iron Shovel", "Diamond Shovel",
            "Wooden Axe", "Stone Axe", "Iron Axe", "Diamond Axe",
            "Wooden Hoe", "Stone Hoe", "Iron Hoe", "Diamond Hoe"
        };

        public static readonly Tuple<string, string, float>[] perks = 
        { 
            new Tuple<string, string, float>("test perk", "Test Perk (cannot purchase)", 69696969f)
        };

        public static string GetBestItem(List<string> itemsList, string type)
        {
            List<string> itemsOfType = itemsList.Where(item => item.EndsWith(type, StringComparison.Ordinal)).ToList();
            return itemsOfType.Count > 0
                ? itemsOfType.OrderByDescending(item => rankings[item.Replace(type, string.Empty).Trim()]).First()
                : string.Empty;
        }

        public static float ComputeItemPrice(string item)
        {
            float price = 4500f; // wood price

            if (item.StartsWith("Stone", StringComparison.Ordinal)) price = 6000f;
            else if (item.StartsWith("Iron", StringComparison.Ordinal)) price = 7500f;
            else if (item.StartsWith("Diamond", StringComparison.Ordinal)) price = 9000f;

            return price;
        }

        public static async Task<string> RandomItem(IGuildUser user, Random random)
        {
            string[] newItems = items;
            DocumentReference userDoc = Program.database.Collection($"servers/{user.GuildId}/users").Document(user.Id.ToString());
            DocumentSnapshot snap = await userDoc.GetSnapshotAsync();
            if (snap.TryGetValue("items", out List<string> itemsList)) newItems = newItems.Where(item => !itemsList.Contains(item)).ToArray();

            return items.Length <= newItems.Length ? newItems[random.Next(newItems.Length)] : string.Empty;
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
    }
}
