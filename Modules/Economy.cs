using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using RRBot.Extensions;
using RRBot.Preconditions;
using RRBot.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RRBot.Modules
{
    [Summary("This is the hub for checking and managing your economy stuff. Wanna know how much cash you have? Or what items you have? Or do you want to check out le shoppe? It's all here.")]
    public class Economy : ModuleBase<SocketCommandContext>
    {
        private static readonly string[] CMDS_WITH_COOLDOWN = { "Deal", "Loot", "Rape", "Rob", "Slavery", "Whore", "Chop",
            "Dig", "Farm", "Fish", "Hunt", "Mine" };

        private async Task AddBackUserSettings(DocumentReference doc, double btc, double doge, double eth, double ltc, double xrp,
            bool dmNotifsV, bool noReplyPingsV, bool rankupNotifsV, Dictionary<string, string> userStats, long whoreCd,
            long slaveryCd, long rapeCd, long lootCd, long dealCd, long bullyCd, long mineCd, long huntCd, long farmCd,
            long digCd, long chopCd)
        {
            if (btc >= Constants.INVESTMENT_MIN_AMOUNT)
                await CashSystem.AddCrypto(Context.User, "btc", btc);
            if (doge >= Constants.INVESTMENT_MIN_AMOUNT)
                await CashSystem.AddCrypto(Context.User, "doge", doge);
            if (eth >= Constants.INVESTMENT_MIN_AMOUNT)
                await CashSystem.AddCrypto(Context.User, "eth", eth);
            if (ltc >= Constants.INVESTMENT_MIN_AMOUNT)
                await CashSystem.AddCrypto(Context.User, "ltc", ltc);
            if (xrp >= Constants.INVESTMENT_MIN_AMOUNT)
                await CashSystem.AddCrypto(Context.User, "xrp", xrp);

            await doc.SetAsync(new
            {
                dmNotifs = dmNotifsV,
                noReplyPings = noReplyPingsV,
                rankupNotifs = rankupNotifsV,
                stats = userStats ?? new Dictionary<string, string>(),
                whoreCooldown = whoreCd,
                slaveryCooldown = slaveryCd,
                rapeCooldown = rapeCd,
                lootCooldown = lootCd,
                dealCooldown = dealCd,
                bullyCooldown = bullyCd,
                mineCooldown = mineCd,
                huntCooldown = huntCd,
                farmCooldown = farmCd,
                digCooldown = digCd,
                chopCooldown = chopCd
            }, SetOptions.MergeAll);
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
                if (user == null)
                    await Context.User.NotifyAsync(Context.Channel, $"You have **{cash:C2}**.");
                else
                    await ReplyAsync($"**{user}** has **{cash:C2}**.");

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
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            StringBuilder description = new();
            double mult = snap.TryGetValue("perks", out Dictionary<string, long> perks) && perks.Keys.Contains("Speed Demon")
                ? 0.85 : 1;

            foreach (string cmd in CMDS_WITH_COOLDOWN)
            {
                if (snap.TryGetValue(cmd.ToLower() + "Cooldown", out long cooldown))
                {
                    long fullCd = (long)((cooldown - DateTimeOffset.UtcNow.ToUnixTimeSeconds()) * mult);
                    if (fullCd > 0L)
                        description.AppendLine($"**{cmd}**: {TimeSpan.FromSeconds(fullCd).FormatCompound()}");
                }
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
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.ContainsField("usingSlots"))
                return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");

            if (item == "Pacifist")
            {
                if (snap.TryGetValue("perks", out Dictionary<string, long> usrPerks) && usrPerks.Keys.Contains("Pacifist"))
                {
                    usrPerks.Remove("Pacifist");
                    Dictionary<string, object> newPerks = new() { { "perks", usrPerks } };
                    await doc.SetAsync(new { pacifistCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(259200) }, SetOptions.MergeAll);
                    await doc.UpdateAsync(newPerks);
                    await Context.User.NotifyAsync(Context.Channel, "You discarded your Pacifist perk. If you wish to buy it again, you will have to wait 3 days.");
                    return CommandResult.FromSuccess();
                }

                return CommandResult.FromError("You do not have the Pacifist perk!");
            }

            List<string> usrItems = snap.GetValue<List<string>>("items");
            double cash = snap.GetValue<double>("cash");

            if (usrItems.Remove(item))
            {
                double price = Items.ComputeItemPrice(item) / 1.5;
                await CashSystem.SetCash(Context.User, Context.Channel, cash + price);
                await Context.User.NotifyAsync(Context.Channel, $"You sold your {item} to some dude for **{price:C2}**.");
                await doc.SetAsync(new { items = usrItems }, SetOptions.MergeAll);
                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError($"You do not have a(n) {item}!" +
                "\n*Tip: This command is case sensitive and does not accept perks other than Pacifist.*");
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
        [Summary("Check the leaderboard for cash or for a specific currency.")]
        [Remarks("$leaderboard <currency>")]
        public async Task<RuntimeResult> Leaderboard(string crypto = "cash")
        {
            string cryptoLower = crypto.ToLower();
            if (cryptoLower != "cash" && cryptoLower != "btc" && cryptoLower != "doge"
                && cryptoLower != "eth" && cryptoLower != "ltc" && cryptoLower != "xrp")
            {
                return CommandResult.FromError($"**{crypto}** is not a currently accepted currency!");
            }

            CollectionReference users = Program.database.Collection($"servers/{Context.Guild.Id}/users");
            Query ordered = users.OrderByDescending(crypto);
            QuerySnapshot snap = await ordered.GetSnapshotAsync();

            StringBuilder lb = new();
            int processedUsers = 0;
            foreach (DocumentSnapshot doc in snap.Documents)
            {
                if (processedUsers == 10)
                    break;

                SocketGuildUser user = Context.Guild.GetUser(Convert.ToUInt64(doc.Id));
                if (user == null || (doc.TryGetValue("perks", out Dictionary<string, long> perks) && perks.Keys.Contains("Pacifist")))
                    continue;

                double val = doc.GetValue<double>(crypto);
                if (val < Constants.INVESTMENT_MIN_AMOUNT && cryptoLower != "cash")
                    break;

                lb.AppendLine($"{processedUsers + 1}: **{user}**: {(cryptoLower == "cash" ? val.ToString("C2") : val.ToString("0.####"))}");
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
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            StringBuilder perksBuilder = new();
            Dictionary<string, long> usrPerks = snap.GetValue<Dictionary<string, long>>("perks");
            foreach (KeyValuePair<string, long> perk in usrPerks.OrderBy(p => p.Key))
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

            CollectionReference users = Program.database.Collection($"servers/{Context.Guild.Id}/users");
            DocumentSnapshot aSnap = await users.Document(Context.User.Id.ToString()).GetSnapshotAsync();
            if (aSnap.ContainsField("usingSlots"))
                return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");
            double aCash = aSnap.GetValue<double>("cash");

            DocumentSnapshot tSnap = await users.Document(user.Id.ToString()).GetSnapshotAsync();
            double tCash = tSnap.GetValue<double>("cash");

            if (amount > aCash)
                return CommandResult.FromError("You do not have that much money!");

            await CashSystem.SetCash(Context.User, Context.Channel, aCash - amount);
            await CashSystem.SetCash(user as SocketUser, Context.Channel, tCash + amount);

            await Context.User.NotifyAsync(Context.Channel, $"You have sauced **{user}** {amount:C2}.");
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
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.ContainsField("usingSlots"))
                return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");

            snap.TryGetValue("btc", out double btc);
            snap.TryGetValue("doge", out double doge);
            snap.TryGetValue("eth", out double eth);
            snap.TryGetValue("ltc", out double ltc);
            snap.TryGetValue("xrp", out double xrp);
            snap.TryGetValue("dmNotifs", out bool dmNotifsV);
            snap.TryGetValue("noReplyPings", out bool noReplyPingsV);
            snap.TryGetValue("rankupNotifs", out bool rankupNotifsV);
            snap.TryGetValue("stats", out Dictionary<string, string> userStats);
            snap.TryGetValue("whoreCooldown", out long whoreCd);
            snap.TryGetValue("slaveryCooldown", out long slaveryCd);
            snap.TryGetValue("rapeCooldown", out long rapeCd);
            snap.TryGetValue("lootCooldown", out long lootCd);
            snap.TryGetValue("dealCooldown", out long dealCd);
            snap.TryGetValue("bullyCooldown", out long bullyCd);
            snap.TryGetValue("mineCooldown", out long mineCd);
            snap.TryGetValue("huntCooldown", out long huntCd);
            snap.TryGetValue("farmCooldown", out long farmCd);
            snap.TryGetValue("digCooldown", out long digCd);
            snap.TryGetValue("chopCooldown", out long chopCd);
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
                    await doc.DeleteAsync();
                    await CashSystem.SetCash(Context.User, Context.Channel, 0);
                    await AddBackUserSettings(doc, btc, doge, eth, ltc, xrp, dmNotifsV, noReplyPingsV, rankupNotifsV, userStats,
                        whoreCd, slaveryCd, rapeCd, lootCd, dealCd, bullyCd, mineCd, huntCd, farmCd, digCd, chopCd);
                    break;
                case 3:
                    await Context.User.NotifyAsync(Context.Channel, "It was quite a struggle, but the noose put you out of your misery. You lost everything.");
                    await doc.DeleteAsync();
                    await CashSystem.SetCash(Context.User, Context.Channel, 0);
                    await AddBackUserSettings(doc, btc, doge, eth, ltc, xrp, dmNotifsV, noReplyPingsV, rankupNotifsV, userStats,
                        whoreCd, slaveryCd, rapeCd, lootCd, dealCd, bullyCd, mineCd, huntCd, farmCd, digCd, chopCd);
                    break;
            }

            return CommandResult.FromSuccess();
        }
    }
}
