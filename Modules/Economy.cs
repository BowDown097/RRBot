using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using RRBot.Entities;
using RRBot.Extensions;
using RRBot.Preconditions;
using RRBot.Systems;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RRBot.Modules
{
    [Summary("This is the hub for checking and managing your economy stuff. Wanna know how much cash you have? Or what items you have? Or do you want to check out le shoppe? It's all here.")]
    public class Economy : ModuleBase<SocketCommandContext>
    {
        public static readonly string[] CMDS_WITH_COOLDOWN = { "Deal", "Loot", "Rape", "Rob", "Slavery", "Whore", "Bully",
            "Chop", "Dig", "Farm", "Fish", "Hunt", "Mine" };

        private static async Task AddBackUserSettings(DbUser user, double btc, double doge, double eth, double ltc, double xrp,
            bool dmNotifs, bool noReplyPings, bool rankupNotifs, Dictionary<string, string> stats, long whoreCd,
            long slaveryCd, long rapeCd, long lootCd, long dealCd, long bullyCd, long mineCd, long huntCd, long farmCd,
            long digCd, long chopCd)
        {
            user.BTC = btc;
            user.DOGE = doge;
            user.ETH = eth;
            user.LTC = ltc;
            user.XRP = xrp;
            user.DMNotifs = dmNotifs;
            user.NoReplyPings = noReplyPings;
            user.RankupNotifs = rankupNotifs;
            user.Stats = stats;
            user.WhoreCooldown = whoreCd;
            user.SlaveryCooldown = slaveryCd;
            user.RapeCooldown = rapeCd;
            user.LootCooldown = lootCd;
            user.DealCooldown = dealCd;
            user.BullyCooldown = bullyCd;
            user.MineCooldown = mineCd;
            user.HuntCooldown = huntCd;
            user.FarmCooldown = farmCd;
            user.DigCooldown = digCd;
            user.ChopCooldown = chopCd;
            await user.Write();
        }

        [Alias("bal", "cash")]
        [Command("balance")]
        [Summary("Check your own or someone else's balance.")]
        [Remarks("$balance <user>")]
        public async Task<RuntimeResult> Balance(IGuildUser user = null)
        {
            if (user?.IsBot == true)
                return CommandResult.FromError("Nope.");

            ulong userId = user == null ? Context.User.Id : user.Id;
            DbUser dbUser = await DbUser.GetById(Context.Guild.Id, userId);
            if (dbUser.Cash > 0)
            {
                if (user == null)
                    await Context.User.NotifyAsync(Context.Channel, $"You have **{dbUser.Cash:C2}**.");
                else
                    await ReplyAsync($"**{user}** has **{dbUser.Cash:C2}**.");
                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError(user == null ? "You're broke!" : $"**{user}** is broke!");
        }

        [Alias("purchase")]
        [Command("buy")]
        [Summary("Buy an item or perk from the shop.")]
        [Remarks("$buy [item]")]
        public async Task<RuntimeResult> Buy([Remainder] string item)
        {
            if (Items.items.Any(i => i == item))
                return await Items.BuyItem(item, Context.User, Context.Guild, Context.Channel);
            else if (Items.perks.Any(perk => perk.Item1 == item))
                return await Items.BuyPerk(item, Context.User, Context.Guild, Context.Channel);
            else
                return CommandResult.FromError($"**{item}** is not a valid item or perk!\n*Tip: This command is case sensitive.*");
        }

        [Alias("cd")]
        [Command("cooldowns")]
        [Summary("Check your crime cooldowns.")]
        [Remarks("$cooldowns")]
        public async Task Cooldowns()
        {
            DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
            StringBuilder description = new();
            double mult = user.Perks?.Keys.Contains("Speed Demon") == true ? 0.85 : 1;

            foreach (string cmd in CMDS_WITH_COOLDOWN)
            {
                long cooldown = (long)user[$"{cmd}Cooldown"];
                long fullCd = (long)((cooldown - DateTimeOffset.UtcNow.ToUnixTimeSeconds()) * mult);
                if (fullCd > 0L)
                    description.AppendLine($"**{cmd}**: {TimeSpan.FromSeconds(fullCd).FormatCompound()}");
            }

            EmbedBuilder embed = new()
            {
                Title = "Cooldowns",
                Color = Color.Red,
                Description = description.Length > 0 ? description.ToString() : "None"
            };
            await ReplyAsync(embed: embed.Build());
        }

        [Command("discard")]
        [Summary("Discard an item or the Pacifist perk.")]
        [Remarks("$discard [item]")]
        [RequireItem]
        public async Task<RuntimeResult> Discard([Remainder] string item)
        {
            DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
            if (user.UsingSlots)
                return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");

            if (item == "Pacifist")
            {
                if (user.Perks?.ContainsKey("Pacifist") == true)
                {
                    user.Perks.Remove("Pacifist");
                    user.PacifistCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(259200);
                    await Context.User.NotifyAsync(Context.Channel, "You discarded your Pacifist perk. If you wish to buy it again, you will have to wait 3 days.");
                }

                return CommandResult.FromError("You do not have the Pacifist perk!");
            }
            else if (user.Items?.Remove(item) == true)
            {
                double price = Items.ComputeItemPrice(item) / 1.5;
                await user.SetCash(Context.User, Context.Channel, user.Cash + price);
                await Context.User.NotifyAsync(Context.Channel, $"You sold your {item} to some dude for **{price:C2}**.");
            }

            await user.Write();
            return CommandResult.FromError($"You do not have a(n) {item}!" +
                "\n*Tip: This command is case sensitive and does not accept perks other than Pacifist.*");
        }

        [Command("items")]
        [Summary("Check your items.")]
        [Remarks("$items")]
        [RequireItem]
        public async Task GetItems()
        {
            DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
            EmbedBuilder embed = new()
            {
                Title = "Items",
                Color = Color.Red,
                Description = string.Join(", ", user.Items)
            };
            await ReplyAsync(embed: embed.Build());
        }

        [Alias("lb")]
        [Command("leaderboard")]
        [Summary("Check the leaderboard for cash or for a specific currency.")]
        [Remarks("$leaderboard <currency>")]
        public async Task<RuntimeResult> Leaderboard(string crypto = "cash")
        {
            string cryptoFormatted = crypto.Equals("cash", StringComparison.OrdinalIgnoreCase) ? "Cash" : crypto.ToUpper();
            if (cryptoFormatted != "Cash" && cryptoFormatted != "BTC" && cryptoFormatted != "DOGE"
                && cryptoFormatted != "ETH" && cryptoFormatted != "LTC" && cryptoFormatted != "XRP")
            {
                return CommandResult.FromError($"**{crypto}** is not a currently accepted currency!");
            }

            QuerySnapshot users = await Program.database.Collection($"servers/{Context.Guild.Id}/users")
                .OrderByDescending(crypto).GetSnapshotAsync();
            StringBuilder lb = new();
            int processedUsers = 0;
            foreach (DocumentSnapshot doc in users.Documents)
            {
                if (processedUsers == 10)
                    break;

                ulong userId = Convert.ToUInt64(doc.Id);
                SocketGuildUser guildUser = Context.Guild.GetUser(userId);
                if (guildUser == null)
                    continue;

                DbUser user = await DbUser.GetById(Context.Guild.Id, userId);
                if (user.Perks?.ContainsKey("Pacifist") == true)
                    continue;

                double val = (double)user[cryptoFormatted];
                if (val < Constants.INVESTMENT_MIN_AMOUNT)
                    break;

                lb.AppendLine($"{processedUsers + 1}: **{guildUser}**: {(cryptoFormatted == "Cash" ? val.ToString("C2") : val.ToString("0.####"))}");
                processedUsers++;
            }

            EmbedBuilder embed = new()
            {
                Color = Color.Red,
                Title = "Leaderboard",
                Description = lb.Length > 0 ? lb.ToString() : "Nothing to see here!"
            };
            await ReplyAsync(embed: embed.Build());
            return CommandResult.FromSuccess();
        }

        [Command("perks")]
        [Summary("View info about your currently active perks.")]
        [Remarks("$perks")]
        [RequirePerk]
        public async Task Perks()
        {
            DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
            StringBuilder perksBuilder = new();
            foreach (KeyValuePair<string, long> perk in user.Perks?.OrderBy(p => p.Key))
            {
                if (perk.Value <= DateTimeOffset.UtcNow.ToUnixTimeSeconds() && perk.Key != "Pacifist")
                    return;

                Tuple<string, string, double, long> perkT = Array.Find(Items.perks, p => p.Item1.Equals(perk.Key, StringComparison.OrdinalIgnoreCase));
                perksBuilder.AppendLine($"**{perkT.Item1}**: {perkT.Item2}" +
                    $"\nTime Left: {(perk.Key != "Pacifist" ? TimeSpan.FromSeconds(perk.Value - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).FormatCompound() : "Infinity")}");
            }

            EmbedBuilder embed = new()
            {
                Title = "Perks",
                Color = Color.Red,
                Description = perksBuilder.Length > 0 ? perksBuilder.ToString() : "None"
            };

            await ReplyAsync(embed: embed.Build());
        }

        [Alias("roles")]
        [Command("ranks")]
        [Summary("View all the ranks and their costs.")]
        [Remarks("$ranks")]
        public async Task Ranks()
        {
            DocumentReference ranksDoc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("ranks");
            DocumentSnapshot snap = await ranksDoc.GetSnapshotAsync();

            StringBuilder ranks = new();
            foreach (KeyValuePair<string, object> kvp in snap.ToDictionary().Where(kvp => kvp.Key.EndsWith("Id", StringComparison.Ordinal)).OrderBy(kvp => kvp.Key))
            {
                double neededCash = snap.GetValue<double>(kvp.Key.Replace("Id", "Cost"));
                SocketRole role = Context.Guild.GetRole(Convert.ToUInt64(kvp.Value));
                ranks.AppendLine($"**{role.Name}**: {neededCash:C2}");
            }

            EmbedBuilder embed = new()
            {
                Color = Color.Red,
                Title = "Available Ranks",
                Description = ranks.Length > 0 ? ranks.ToString() : "None"
            };

            await ReplyAsync(embed: embed.Build());
        }

        [Alias("give", "transfer")]
        [Command("sauce")]
        [Summary("Sauce someone some cash.")]
        [Remarks("$sauce [user] [amount]")]
        public async Task<RuntimeResult> Sauce(IGuildUser user, double amount)
        {
            if (amount < 0.01 || double.IsNaN(amount))
                return CommandResult.FromError("You can't sauce negative or no money!");
            if (Context.User == user)
                return CommandResult.FromError("You can't sauce yourself money. Don't even know how you would.");
            if (user.IsBot)
                return CommandResult.FromError("Nope.");

            DbUser author = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
            DbUser target = await DbUser.GetById(Context.Guild.Id, user.Id);
            if (author.UsingSlots)
                return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");
            if (author.Cash < amount)
                return CommandResult.FromError("You do not have that much money!");

            await author.SetCash(Context.User, Context.Channel, author.Cash - amount);
            await target.SetCash(user as SocketUser, Context.Channel, target.Cash + amount);

            await Context.User.NotifyAsync(Context.Channel, $"You have sauced **{user}** {amount:C2}.");
            await author.Write();
            await target.Write();
            return CommandResult.FromSuccess();
        }

        [Command("shop")]
        [Summary("Check out what's available for purchase in the shop.")]
        [Remarks("$shop <items|perks>")]
        public async Task Shop(string category = "")
        {
            StringBuilder items = new();
            StringBuilder perks = new();

            foreach (string item in Items.items)
            {
                double price = Items.ComputeItemPrice(item);
                items.AppendLine($"**{item}**: {price:C2}");
            }

            foreach (Tuple<string, string, double, long> perk in Items.perks)
                perks.AppendLine($"**{perk.Item1}**: {perk.Item2}\nDuration: {TimeSpan.FromSeconds(perk.Item4).FormatCompound()}\nPrice: {perk.Item3:C2}");

            EmbedBuilder itemsEmbed = new()
            {
                Color = Color.Red,
                Title = "⛏️Items⛏️️",
                Description = items.ToString()
            };

            EmbedBuilder perksEmbed = new()
            {
                Color = Color.Red,
                Title = "️️🧪Perks🧪",
                Description = perks.ToString()
            };

            if (string.IsNullOrWhiteSpace(category))
            {
                await ReplyAsync("Welcome to the shop! Here's what I've got: ", embed: itemsEmbed.Build());
                await ReplyAsync(embed: perksEmbed.Build());
            }
            else if (category.Equals("items", StringComparison.OrdinalIgnoreCase))
            {
                await ReplyAsync("Welcome to the shop! Here's what I've got: ", embed: itemsEmbed.Build());
            }
            else if (category.Equals("perks", StringComparison.OrdinalIgnoreCase))
            {
                await ReplyAsync("Welcome to the shop! Here's what I've got: ", embed: perksEmbed.Build());
            }
            else
            {
                await ReplyAsync($"{Context.User.Mention}, **{category}** is not a valid category!");
            }
        }

        [Alias("kms", "selfend")]
        [Command("suicide")]
        [Summary("Kill yourself.")]
        [Remarks("$suicide")]
        public async Task<RuntimeResult> Suicide()
        {
            DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
            DbUser temp = user;
            if (user.UsingSlots)
                return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");

            switch (RandomUtil.Next(4))
            {
                case 0:
                    await Context.User.NotifyAsync(Context.Channel, "You attempted to hang yourself, but the rope snapped. You did not die.");
                    break;
                case 1:
                    await Context.User.NotifyAsync(Context.Channel, "You shot yourself, but somehow the bullet didn't kill you. Lucky or unlucky?");
                    break;
                case 2:
                    await Context.User.NotifyAsync(Context.Channel, "​DAMN that shotgun made a fucking mess out of you! You're DEAD DEAD, and lost everything.");
                    await user.Reference.DeleteAsync();
                    await user.SetCash(Context.User, Context.Channel, 0);
                    await AddBackUserSettings(user, temp.BTC, temp.DOGE, temp.ETH, temp.LTC, temp.XRP, temp.DMNotifs,
                        temp.NoReplyPings, temp.RankupNotifs, temp.Stats, temp.WhoreCooldown, temp.SlaveryCooldown,
                        temp.RapeCooldown, temp.LootCooldown, temp.DealCooldown, temp.BullyCooldown, temp.MineCooldown,
                        temp.HuntCooldown, temp.FarmCooldown, temp.DigCooldown, temp.ChopCooldown);
                    break;
                case 3:
                    await Context.User.NotifyAsync(Context.Channel, "It was quite a struggle, but the noose put you out of your misery. You lost everything.");
                    await user.Reference.DeleteAsync();
                    await user.SetCash(Context.User, Context.Channel, 0);
                    await AddBackUserSettings(user, temp.BTC, temp.DOGE, temp.ETH, temp.LTC, temp.XRP, temp.DMNotifs,
                        temp.NoReplyPings, temp.RankupNotifs, temp.Stats, temp.WhoreCooldown, temp.SlaveryCooldown,
                        temp.RapeCooldown, temp.LootCooldown, temp.DealCooldown, temp.BullyCooldown, temp.MineCooldown,
                        temp.HuntCooldown, temp.FarmCooldown, temp.DigCooldown, temp.ChopCooldown);
                    break;
            }

            return CommandResult.FromSuccess();
        }
    }
}
