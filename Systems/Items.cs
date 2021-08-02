using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using RRBot.Extensions;

namespace RRBot.Systems
{
    public static class Items
    {
        private static readonly Dictionary<string, int> rankings = new()
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

        // name, description, price, duration (secs)
        public static readonly Tuple<string, string, double, long>[] perks =
        {
            new("Enchanter", "Tasks are 10% more effective, but your items have a 2% chance of breaking after use.", 5000, 172800),
            new("Speed Demon", "Cooldowns are 15% shorter, but you have a 5% higher chance of failing any command that can fail.", 5000, 172800),
            new("Multiperk", "Grants the ability to equip 2 perks, not including this one.", 25000, 604800),
            new("Pacifist", "You are immune to all crimes, but you cannot use any crime commands and you also cannot appear on the leaderboard. Cannot be stacked with other perks, even if you have the Multiperk. Can be discarded, but cannot be used again for 3 days.", 0, -1)
        };

        public static async Task<RuntimeResult> BuyItem(string item, SocketUser user, SocketGuild guild, ISocketMessageChannel channel)
        {
            DocumentReference doc = Program.database.Collection($"servers/{guild.Id}/users").Document(user.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("usingSlots", out bool usingSlots) && usingSlots)
                return CommandResult.FromError($"{user.Mention}, you appear to be currently gambling. I cannot do any transactions at the moment.");
            List<string> usrItems = snap.TryGetValue("items", out List<string> tmpItems) ? tmpItems : new List<string>();
            double cash = snap.GetValue<double>("cash");

            if (!usrItems.Contains(item))
            {
                double price = ComputeItemPrice(item);
                if (price < cash)
                {
                    usrItems.Add(item);
                    await CashSystem.SetCash(user as IGuildUser, channel, cash - price);
                    await user.NotifyAsync(channel, $"You got yourself a fresh {item} for **{price:C2}**!");
                    await doc.SetAsync(new { items = usrItems }, SetOptions.MergeAll);
                    return CommandResult.FromSuccess();
                }

                return CommandResult.FromError($"{user.Mention}, you do not have enough to buy a {item}!");
            }

            return CommandResult.FromError($"{user.Mention}, you already have a {item}!");
        }

        public static async Task<RuntimeResult> BuyPerk(string perk, SocketUser user, SocketGuild guild, ISocketMessageChannel channel)
        {
            DocumentReference doc = Program.database.Collection($"servers/{guild.Id}/users").Document(user.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("usingSlots", out bool usingSlots) && usingSlots)
                return CommandResult.FromError($"{user.Mention}, you appear to be currently gambling. I cannot do any transactions at the moment.");

            Dictionary<string, long> usrPerks = snap.TryGetValue("perks", out Dictionary<string, long> tmpPerks) ? tmpPerks : new();
            if (usrPerks.Keys.Contains("Pacifist"))
                return CommandResult.FromError($"{user.Mention}, you have the Pacifist perk and cannot buy another.");
            if (!usrPerks.Keys.Contains("Multiperk") && usrPerks.Count == 1 && perk != "Pacifist" && perk != "Multiperk")
                return CommandResult.FromError($"{user.Mention}, you already have a perk!");
            if (usrPerks.Keys.Contains("Multiperk") && usrPerks.Count == 3 && perk != "Pacifist")
                return CommandResult.FromError($"{user.Mention}, you already have 2 perks!");

            double cash = snap.GetValue<double>("cash");

            if (!usrPerks.Keys.Contains(perk))
            {
                if (perk == "Pacifist")
                {
                    if (snap.TryGetValue("pacifistCooldown", out long pacifistCooldown))
                    {
                        if (pacifistCooldown > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                        {
                            return CommandResult.FromError($"{user.Mention}, you bought the Pacifist perk later than 3 days ago." +
                                $" You still have to wait {TimeSpan.FromSeconds(pacifistCooldown - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).FormatCompound()}.");
                        }

                        await doc.SetAsync(new { pacifistCooldown = FieldValue.Delete }, SetOptions.MergeAll);
                    }

                    foreach (string perkName in usrPerks.Keys)
                    {
                        Tuple<string, string, double, long> funnyTuple = Array.Find(perks, p => p.Item1 == perkName);
                        cash += funnyTuple.Item3;
                        usrPerks.Remove(perkName);
                    }

                    Dictionary<string, object> newPerks = new() { { "perks", usrPerks } };
                    await doc.UpdateAsync(newPerks);
                }

                Tuple<string, string, double, long> perkTuple = Array.Find(perks, p => p.Item1 == perk);
                double price = perkTuple.Item3;
                long duration = perkTuple.Item4;
                if (price < cash)
                {
                    usrPerks.Add(perk, DateTimeOffset.UtcNow.ToUnixTimeSeconds(duration));
                    await CashSystem.SetCash(user as IGuildUser, channel, cash - price);

                    StringBuilder notification = new($"You got yourself the {perk} perk for **{price:C2}**!");
                    if (perk == "Pacifist") notification.Append(" Additionally, as you bought the Pacifist perk, any perks you previously had have been refunded.");

                    await user.NotifyAsync(channel, notification.ToString());
                    await doc.SetAsync(new { perks = usrPerks }, SetOptions.MergeAll);
                    return CommandResult.FromSuccess();
                }

                return CommandResult.FromError($"{user.Mention}, you do not have enough to buy {perk}!");
            }

            return CommandResult.FromError($"{user.Mention}, you already have {perk}!");
        }

        public static double ComputeItemPrice(string item)
        {
            if (item.StartsWith("Stone", StringComparison.Ordinal)) return 6000;
            else if (item.StartsWith("Iron", StringComparison.Ordinal)) return 7500;
            else if (item.StartsWith("Diamond", StringComparison.Ordinal)) return 9000;

            return 4500; // wood price
        }

        public static string GetBestItem(List<string> itemsList, string type)
        {
            List<string> itemsOfType = itemsList.Where(item => item.EndsWith(type, StringComparison.Ordinal)).ToList();
            return itemsOfType.Count > 0 ? itemsOfType.OrderByDescending(item => rankings[item.Replace(type, "").Trim()]).First() : "";
        }

        public static async Task<string> RandomItem(IGuildUser user, Random random)
        {
            string[] newItems = items;
            DocumentReference userDoc = Program.database.Collection($"servers/{user.GuildId}/users").Document(user.Id.ToString());
            DocumentSnapshot snap = await userDoc.GetSnapshotAsync();
            if (snap.TryGetValue("items", out List<string> itemsList)) newItems = newItems.Where(item => !itemsList.Contains(item)).ToArray();

            return items.Length <= newItems.Length ? newItems[random.Next(newItems.Length)] : "";
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
