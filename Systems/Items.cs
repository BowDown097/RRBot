using Discord.Commands;
using Discord.WebSocket;
using RRBot.Entities;
using RRBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            "Wooden Hoe", "Stone Hoe", "Iron Hoe", "Diamond Hoe",
            "Fishing Rod"
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
            DbUser dbUser = await DbUser.GetById(guild.Id, user.Id);
            if (dbUser.UsingSlots)
                return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");

            if (dbUser.Items?.Contains(item) == false)
            {
                double price = ComputeItemPrice(item);
                if (price <= dbUser.Cash)
                {
                    dbUser.Items.Add(item);
                    await dbUser.SetCash(user, channel, dbUser.Cash - price);
                    await user.NotifyAsync(channel, $"You got yourself a fresh {item} for **{price:C2}**!");
                    await dbUser.Write();
                    return CommandResult.FromSuccess();
                }

                return CommandResult.FromError($"You do not have enough to buy a {item}!");
            }

            return CommandResult.FromError($"You already have a {item}!");
        }

        public static async Task<RuntimeResult> BuyPerk(string perk, SocketUser user, SocketGuild guild, ISocketMessageChannel channel)
        {
            DbUser dbUser = await DbUser.GetById(guild.Id, user.Id);
            if (dbUser.UsingSlots)
                return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");

            if (dbUser.Perks?.ContainsKey("Pacifist") == true)
                return CommandResult.FromError("You have the Pacifist perk and cannot buy another.");
            if (dbUser.Perks?.ContainsKey("Multiperk") == true && dbUser.Perks?.Count == 1 && perk != "Pacifist" && perk != "Multiperk")
                return CommandResult.FromError("You already have a perk.");
            if (dbUser.Perks?.ContainsKey("Multiperk") == true && dbUser.Perks?.Count == 3 && perk != "Pacifist")
                return CommandResult.FromError("You already have 2 perks.");

            if (dbUser.Perks?.ContainsKey(perk) == false)
            {
                if (perk == "Pacifist")
                {
                    if (dbUser.PacifistCooldown != 0)
                    {
                        if (dbUser.PacifistCooldown > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                        {
                            return CommandResult.FromError("You bought the Pacifist perk later than 3 days ago." +
                                $" You still have to wait {TimeSpan.FromSeconds(dbUser.PacifistCooldown - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).FormatCompound()}.");
                        }
                        dbUser.PacifistCooldown = 0;
                    }

                    foreach (string perkName in dbUser.Perks.Keys)
                    {
                        Tuple<string, string, double, long> funnyTuple = Array.Find(perks, p => p.Item1 == perkName);
                        dbUser.Cash += funnyTuple.Item3;
                        dbUser.Perks.Remove(perkName);
                    }
                }

                Tuple<string, string, double, long> perkTuple = Array.Find(perks, p => p.Item1 == perk);
                double price = perkTuple.Item3;
                long duration = perkTuple.Item4;
                if (price <= dbUser.Cash)
                {
                    dbUser.Perks.Add(perk, DateTimeOffset.UtcNow.ToUnixTimeSeconds(duration));
                    await dbUser.SetCash(user, channel, dbUser.Cash - price);

                    StringBuilder notification = new($"You got yourself the {perk} perk for **{price:C2}**!");
                    if (perk == "Pacifist")
                        notification.Append(" Additionally, as you bought the Pacifist perk, any perks you previously had have been refunded.");

                    await user.NotifyAsync(channel, notification.ToString());
                    await dbUser.Write();
                    return CommandResult.FromSuccess();
                }

                return CommandResult.FromError($"You do not have enough to buy {perk}!");
            }

            return CommandResult.FromError($"You already have {perk}!");
        }

        public static double ComputeItemPrice(string item)
        {
            if (item.StartsWith("Wooden", StringComparison.Ordinal)) return 4500;
            else if (item.StartsWith("Stone", StringComparison.Ordinal)) return 6000;
            else if (item.StartsWith("Iron", StringComparison.Ordinal)) return 7500;
            else if (item.StartsWith("Diamond", StringComparison.Ordinal)) return 9000;

            return 7500; // misc item price
        }

        public static string GetBestItem(List<string> itemsList, string type)
        {
            List<string> itemsOfType = itemsList.Where(item => item.EndsWith(type, StringComparison.Ordinal)).ToList();
            return itemsOfType.Count > 0
                ? itemsOfType.OrderByDescending(item => rankings[item.Replace(type, "").Trim()]).First()
                : "";
        }
    }
}
