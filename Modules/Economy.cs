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
using RRBot.Preconditions;
using RRBot.Systems;

namespace RRBot.Modules
{
    [Summary("This is the hub for checking and managing your economy stuff. Wanna know how much cash you have? Or what items you have? Or do you want to check out le shoppe? It's all here.")]
    public class Economy : ModuleBase<SocketCommandContext>
    {
        private async Task AddBackUserSettings(DocumentReference doc, double btc, double doge, double eth, double xrp, bool dmNotifsV, bool rankupNotifsV, bool replyPingsV,
            Dictionary<string, string> userStats)
        {
            if (btc > 0) await CashSystem.AddCrypto(Context.User as IGuildUser, "btc", btc);
            if (doge > 0) await CashSystem.AddCrypto(Context.User as IGuildUser, "doge", doge);
            if (eth > 0) await CashSystem.AddCrypto(Context.User as IGuildUser, "eth", eth);
            if (xrp > 0) await CashSystem.AddCrypto(Context.User as IGuildUser, "xrp", xrp);
            if (dmNotifsV) await doc.SetAsync(new { dmNotifs = dmNotifsV }, SetOptions.MergeAll);
            if (rankupNotifsV) await doc.SetAsync(new { rankupNotifs = rankupNotifsV }, SetOptions.MergeAll);
            if (!replyPingsV) await doc.SetAsync(new { replyPings = replyPingsV }, SetOptions.MergeAll);
            if (userStats.Count > 0) await doc.SetAsync(new { stats = userStats }, SetOptions.MergeAll);
        }

        [Alias("bal", "cash")]
        [Command("balance")]
        [Summary("Check your own or someone else's balance.")]
        [Remarks("$balance <user>")]
        public async Task<RuntimeResult> Balance(IGuildUser user = null)
        {
            if (user?.IsBot == true) return CommandResult.FromError("Nope.");

            ulong userId = user == null ? Context.User.Id : user.Id;
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(userId.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            double cash = snap.GetValue<double>("cash");

            if (cash > 0)
            {
                if (user == null) await Context.User.NotifyAsync(Context.Channel, $"You have **{cash:C2}**.");
                else await ReplyAsync($"**{user}** has **{cash:C2}**.");
                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError(user == null ? $"{Context.User.Mention}, you're broke!" : $"**{user}** is broke!");
        }

        [Alias("purchase")]
        [Command("buy")]
        [Summary("Buy an item or perk from the shop.")]
        [Remarks("$buy [item]")]
        public async Task<RuntimeResult> Buy([Remainder] string item)
        {
            if (!Items.items.Contains(item)) return CommandResult.FromError($"{Context.User.Mention}, **{item}** is not a valid item!");

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("usingSlots", out bool usingSlots) && usingSlots)
                return CommandResult.FromError($"{Context.User.Mention}, you appear to be currently gambling. I cannot do any transactions at the moment.");
            List<string> usrItems = snap.TryGetValue("items", out List<string> tmpItems) ? tmpItems : new List<string>();
            double cash = snap.GetValue<double>("cash");

            if (!usrItems.Contains(item))
            {
                double price = Items.ComputeItemPrice(item);
                if (price < cash)
                {
                    usrItems.Add(item);
                    await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash - price);
                    await Context.User.NotifyAsync(Context.Channel, $"You got yourself a fresh {item} for **{price:C2}**!");
                    await doc.SetAsync(new { items = usrItems }, SetOptions.MergeAll);
                    return CommandResult.FromSuccess();
                }

                return CommandResult.FromError($"{Context.User.Mention}, you do not have enough to buy a {item}!");
            }

            return CommandResult.FromError($"{Context.User.Mention}, you already have a {item}!");
        }

        [Alias("cd")]
        [Command("cooldowns")]
        [Summary("Check your crime cooldowns.")]
        [Remarks("$cooldowns")]
        public async Task Cooldowns()
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            StringBuilder description = new();
            if (snap.TryGetValue("dealCooldown", out long dealCd) && dealCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds() > 0L)
                description.AppendLine($"**Deal**: {TimeSpan.FromSeconds(dealCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).FormatCompound()}");
            if (snap.TryGetValue("lootCooldown", out long lootCd) && lootCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds() > 0L)
                description.AppendLine($"**Loot**: {TimeSpan.FromSeconds(lootCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).FormatCompound()}");
            if (snap.TryGetValue("rapeCooldown", out long rapeCd) && rapeCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds() > 0L)
                description.AppendLine($"**Rape**: {TimeSpan.FromSeconds(rapeCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).FormatCompound()}");
            if (snap.TryGetValue("slaveryCooldown", out long slaveryCd) && slaveryCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds() > 0L)
                description.AppendLine($"**Slavery**: {TimeSpan.FromSeconds(slaveryCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).FormatCompound()}");
            if (snap.TryGetValue("whoreCooldown", out long whoreCd) && whoreCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds() > 0L)
                description.AppendLine($"**Whore**: {TimeSpan.FromSeconds(whoreCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).FormatCompound()}");
            if (snap.TryGetValue("chopCooldown", out long chopCd) && chopCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds() > 0L)
                description.AppendLine($"**Chopping Wood**: {TimeSpan.FromSeconds(chopCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).FormatCompound()}");
            if (snap.TryGetValue("digCooldown", out long digCd) && digCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds() > 0L)
                description.AppendLine($"**Digging**: {TimeSpan.FromSeconds(digCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).FormatCompound()}");
            if (snap.TryGetValue("farmCooldown", out long farmCd) && farmCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds() > 0L)
                description.AppendLine($"**Farming**: {TimeSpan.FromSeconds(farmCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).FormatCompound()}");
            if (snap.TryGetValue("huntCooldown", out long huntCd) && huntCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds() > 0L)
                description.AppendLine($"**Hunting**: {TimeSpan.FromSeconds(huntCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).FormatCompound()}");
            if (snap.TryGetValue("mineCooldown", out long mineCd) && mineCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds() > 0L)
                description.AppendLine($"**Mining**: {TimeSpan.FromSeconds(mineCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).FormatCompound()}");

            EmbedBuilder embed = new()
            {
                Title = "Crime Cooldowns",
                Color = Color.Red,
                Description = description.Length > 0 ? description.ToString() : "None"
            };

            await ReplyAsync(embed: embed.Build());
        }

        [Command("discard")]
        [Summary("Discard an item.")]
        [Remarks("$discard [item]")]
        [RequireItem]
        public async Task<RuntimeResult> Discard([Remainder] string item)
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("usingSlots", out bool usingSlots) && usingSlots)
                return CommandResult.FromError($"{Context.User.Mention}, you appear to be currently gambling. I cannot do any transactions at the moment.");
            List<string> usrItems = snap.GetValue<List<string>>("items");
            double cash = snap.GetValue<double>("cash");

            if (usrItems.Remove(item))
            {
                double price = Items.ComputeItemPrice(item) / 1.5;
                await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash + price);
                await Context.User.NotifyAsync(Context.Channel, $"You sold your {item} to some dude for **{price:C2}**.");
                await doc.SetAsync(new { items = usrItems }, SetOptions.MergeAll);
                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError($"{Context.User.Mention}, you do not have a {item}!");
        }

        [Command("items")]
        [Summary("Check your items.")]
        [Remarks("$items")]
        [RequireItem]
        public async Task GetItems()
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            List<string> items = snap.GetValue<List<string>>("items");

            EmbedBuilder embed = new()
            {
                Title = "Items",
                Color = Color.Red,
                Description = string.Join(", ", items)
            };

            await ReplyAsync(embed: embed.Build());
        }

        [Alias("lb")]
        [Command("leaderboard")]
        [Summary("Check the leaderboard.")]
        [Remarks("$leaderboard")]
        public async Task Leaderboard()
        {
            CollectionReference users = Program.database.Collection($"servers/{Context.Guild.Id}/users");
            Query ordered = users.OrderByDescending("cash").Limit(10);
            QuerySnapshot snap = await ordered.GetSnapshotAsync();

            StringBuilder builder = new();
            for (int i = 0; i < snap.Documents.Count; i++)
            {
                DocumentSnapshot doc = snap.Documents[i];
                SocketGuildUser user = Context.Guild.GetUser(Convert.ToUInt64(doc.Id));
                if (user == null) continue;
                double cash = doc.GetValue<double>("cash");
                builder.AppendLine($"{i + 1}: **{user}**: {cash:C2}");
            }

            EmbedBuilder embed = new()
            {
                Color = Color.Red,
                Title = "Leaderboard",
                Description = builder.ToString()
            };

            await ReplyAsync(embed: embed.Build());
        }

        [Alias("roles")]
        [Command("ranks")]
        [Summary("View all the ranks and their costs.")]
        [Remarks("$ranks")]
        public async Task Ranks()
        {
            StringBuilder ranks = new();

            DocumentReference ranksDoc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("ranks");
            DocumentSnapshot snap = await ranksDoc.GetSnapshotAsync();

            IEnumerable<KeyValuePair<string, object>> kvps = snap.ToDictionary().Where(kvp => kvp.Key.EndsWith("Id", StringComparison.Ordinal)).OrderBy(kvp => kvp.Key);
            foreach (KeyValuePair<string, object> kvp in kvps)
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
            if (amount <= 0) return CommandResult.FromError($"{Context.User.Mention}, you can't sauce negative or no money!");
            if (Context.User == user) return CommandResult.FromError($"{Context.User.Mention}, you can't sauce yourself money. Don't even know how you would.");
            if (user.IsBot) return CommandResult.FromError("Nope.");

            CollectionReference users = Program.database.Collection($"servers/{Context.Guild.Id}/users");
            DocumentSnapshot aSnap = await users.Document(Context.User.Id.ToString()).GetSnapshotAsync();
            if (aSnap.TryGetValue("usingSlots", out bool usingSlots) && usingSlots)
                return CommandResult.FromError($"{Context.User.Mention}, you appear to be currently gambling. I cannot do any transactions at the moment.");
            double aCash = aSnap.GetValue<double>("cash");

            DocumentSnapshot tSnap = await users.Document(user.Id.ToString()).GetSnapshotAsync();
            double tCash = tSnap.GetValue<double>("cash");

            if (amount > aCash) return CommandResult.FromError($"{Context.User.Mention}, you do not have that much money!");

            await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, aCash - amount);
            await CashSystem.SetCash(user, Context.Channel, tCash + amount);

            await Context.User.NotifyAsync(Context.Channel, $"You have sauced **{user}** {amount:C2}.");
            return CommandResult.FromSuccess();
        }

        [Command("shop")]
        [Summary("Check out what's available for purchase in the shop.")]
        [Remarks("$shop")]
        public async Task Shop()
        {
            StringBuilder items = new();
            StringBuilder perks = new();

            foreach (string item in Items.items)
            {
                double price = Items.ComputeItemPrice(item);
                items.AppendLine($"**{item}**: {price:C2}");
            }

            foreach (Tuple<string, string, double> perk in Items.perks)
                perks.AppendLine($"**{perk.Item1}**: {perk.Item2}. Price: {perk.Item3:C2}");

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

            await ReplyAsync("Welcome to the shop! Here's what I've got: ", embed: itemsEmbed.Build());
            await ReplyAsync(embed: perksEmbed.Build());
        }

        [Alias("kms", "selfend")]
        [Command("suicide")]
        [Summary("Kill yourself.")]
        [Remarks("$suicide")]
        public async Task<RuntimeResult> Suicide()
        {
            Random random = new();
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("usingSlots", out bool usingSlots) && usingSlots)
                return CommandResult.FromError($"{Context.User.Mention}, you appear to be currently gambling. I cannot do any transactions at the moment.");

            snap.TryGetValue("btc", out double btc);
            snap.TryGetValue("doge", out double doge);
            snap.TryGetValue("eth", out double eth);
            snap.TryGetValue("xrp", out double xrp);
            snap.TryGetValue("dmNotifs", out bool dmNotifsV);
            snap.TryGetValue("rankupNotifs", out bool rankupNotifsV);
            snap.TryGetValue("replyPings", out bool replyPingsV);
            snap.TryGetValue("stats", out Dictionary<string, string> userStats);
            switch (random.Next(4))
            {
                case 0:
                    await Context.User.NotifyAsync(Context.Channel, "You attempted to hang yourself, but the rope snapped. You did not die.");
                    break;
                case 1:
                    await Context.User.NotifyAsync(Context.Channel, "You shot yourself, but somehow the bullet didn't kill you. Lucky or unlucky?");
                    break;
                case 2:
                    await Context.User.NotifyAsync(Context.Channel, "DAMN that shotgun made a fucking mess out of you! You're DEAD DEAD, and lost everything.");
                    await doc.DeleteAsync();
                    await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, 0);
                    await AddBackUserSettings(doc, btc, doge, eth, xrp, dmNotifsV, rankupNotifsV, replyPingsV, userStats);
                    break;
                case 3:
                    await Context.User.NotifyAsync(Context.Channel, "It was quite a struggle, but the noose put you out of your misery. You lost everything.");
                    await doc.DeleteAsync();
                    await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, 0);
                    await AddBackUserSettings(doc, btc, doge, eth, xrp, dmNotifsV, rankupNotifsV, replyPingsV, userStats);
                    break;
            }

            return CommandResult.FromSuccess();
        }
    }
}
