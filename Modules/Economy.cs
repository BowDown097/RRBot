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
    public class Economy : ModuleBase<SocketCommandContext>
    {
        [Alias("bal", "cash")]
        [Command("balance")]
        [Summary("Check your own or someone else's balance.")]
        [Remarks("``$balance <user>``")]
        public async Task<RuntimeResult> Balance(IGuildUser user = null)
        {
            if (user != null && user.IsBot) return CommandResult.FromError("Nope.");

            ulong userId = user == null ? Context.User.Id : user.Id;
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(userId.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            float cash = snap.GetValue<float>("cash");
            if (cash > 0)
            {
                await ReplyAsync(user == null ? $"{Context.User.Mention}, you have **{cash.ToString("C2")}**." : $"**{user.ToString()}** has **{cash.ToString("C2")}**.");
                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError(user == null ? $"{Context.User.Mention}, you're broke!" : $"{user.Mention} is broke!");
        }

        [Alias("cd")]
        [Command("cooldowns")]
        [Summary("Check your crime cooldowns.")]
        [Remarks("``$cooldowns``")]
        public async Task Cooldowns()
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            StringBuilder description = new StringBuilder();
            if (snap.TryGetValue("rapeCooldown", out long rapeCd) && rapeCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds() > 0L)
                description.AppendLine($"**Rape**: {TimeSpan.FromSeconds(rapeCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).FormatCompound()}");
            if (snap.TryGetValue("whoreCooldown", out long whoreCd) && whoreCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds() > 0L)
                description.AppendLine($"**Whore**: {TimeSpan.FromSeconds(whoreCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).FormatCompound()}");
            if (snap.TryGetValue("lootCooldown", out long lootCd) && lootCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds() > 0L)
                description.AppendLine($"**Loot**: {TimeSpan.FromSeconds(lootCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).FormatCompound()}");
            if (snap.TryGetValue("slaveryCooldown", out long slaveryCd) && slaveryCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds() > 0L)
                description.AppendLine($"**Slavery**: {TimeSpan.FromSeconds(slaveryCd - DateTimeOffset.UtcNow.ToUnixTimeSeconds()).FormatCompound()}");
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

            EmbedBuilder embed = new EmbedBuilder
            {
                Title = "Crime Cooldowns",
                Color = Color.Red,
                Description = description.Length > 0 ? description.ToString() : "None"
            };

            await ReplyAsync(embed: embed.Build());
        }

        [Command("discard")]
        [Summary("Discard an item.")]
        [Remarks("``$discard [item]``")]
        [RequireItem]
        public async Task<RuntimeResult> DiscardItem([Remainder] string item)
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("usingSlots", out bool usingSlots) && usingSlots)
                return CommandResult.FromError($"{Context.User.Mention}, you appear to be currently gambling. I cannot do any transactions at the moment.");

            List<string> usrItems = snap.GetValue<List<string>>("items");
            float cash = snap.GetValue<float>("cash");

            if (usrItems.Remove(item))
            {
                if (item.StartsWith("Wooden", StringComparison.Ordinal))
                {
                    await CashSystem.SetCash(Context.User as IGuildUser, cash + 3000f);
                    await ReplyAsync($"{Context.User.Mention}, you sold your {item} to some dude for **$3000.00**.");
                }
                else if (item.StartsWith("Stone", StringComparison.Ordinal))
                {
                    await CashSystem.SetCash(Context.User as IGuildUser, cash + 4000f);
                    await ReplyAsync($"{Context.User.Mention}, you sold your {item} to some dude for **$4000.00**.");
                }
                else if (item.StartsWith("Iron", StringComparison.Ordinal))
                {
                    await CashSystem.SetCash(Context.User as IGuildUser, cash + 5000f);
                    await ReplyAsync($"{Context.User.Mention}, you sold your {item} to some dude for **$5000.00**.");
                }
                else if (item.StartsWith("Diamond", StringComparison.Ordinal))
                {
                    await CashSystem.SetCash(Context.User as IGuildUser, cash + 6000f);
                    await ReplyAsync($"{Context.User.Mention}, you sold your {item} to some dude for **$6000.00**.");
                }

                await doc.SetAsync(new { items = usrItems }, SetOptions.MergeAll);
                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError($"{Context.User.Mention}, you do not have a {item}!");
        }

        [Command("items")]
        [Summary("Check your items.")]
        [Remarks("``$items``")]
        [RequireItem]
        public async Task Items()
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            List<string> items = snap.GetValue<List<string>>("items");

            EmbedBuilder embed = new EmbedBuilder
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
        [Remarks("``$leaderboard``")]
        public async Task Leaderboard()
        {
            CollectionReference users = Program.database.Collection($"servers/{Context.Guild.Id}/users");
            Query ordered = users.OrderByDescending("cash").Limit(10);
            QuerySnapshot snap = await ordered.GetSnapshotAsync();

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < snap.Documents.Count; i++)
            {
                DocumentSnapshot doc = snap.Documents[i];
                SocketGuildUser user = Context.Guild.GetUser(Convert.ToUInt64(doc.Id));
                if (user == null) continue;
                float cash = doc.GetValue<float>("cash");
                builder.AppendLine($"{i + 1}: **{user.ToString()}**: {cash.ToString("C2")}");
            }

            EmbedBuilder embed = new EmbedBuilder
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
        [Remarks("``$ranks``")]
        public async Task Ranks()
        {
            StringBuilder ranks = new StringBuilder();

            DocumentReference ranksDoc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("ranks");
            DocumentSnapshot snap = await ranksDoc.GetSnapshotAsync();

            IEnumerable<KeyValuePair<string, object>> kvps = snap.ToDictionary().Where(kvp => kvp.Key.EndsWith("Id", StringComparison.Ordinal)).OrderBy(kvp => kvp.Key);
            if (kvps.Any())
            {
                foreach (KeyValuePair<string, object> kvp in kvps)
                {
                    float neededCash = snap.GetValue<float>(kvp.Key.Replace("Id", "Cost"));
                    SocketRole role = Context.Guild.GetRole(Convert.ToUInt64(kvp.Value));
                    ranks.AppendLine($"**{role.Name}**: {neededCash.ToString("C2")}");
                }
            }

            EmbedBuilder embed = new EmbedBuilder
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
        [Remarks("``$sauce [user] [amount]")]
        public async Task<RuntimeResult> Sauce(IGuildUser user, float amount)
        {
            if (user.IsBot) return CommandResult.FromError("Nope.");
            if (Context.User == user) return CommandResult.FromError($"{Context.User.Mention}, you can't sauce yourself money. Don't even know how you would.");
            if (amount <= 0 || float.IsNaN(amount)) return CommandResult.FromError($"{Context.User.Mention}, you can't sauce negative or no money!");

            CollectionReference users = Program.database.Collection($"servers/{Context.Guild.Id}/users");
            DocumentSnapshot aSnap = await users.Document(Context.User.Id.ToString()).GetSnapshotAsync();
            if (aSnap.TryGetValue("usingSlots", out bool usingSlots) && usingSlots)
                return CommandResult.FromError($"{Context.User.Mention}, you appear to be currently gambling. I cannot do any transactions at the moment.");
            float aCash = aSnap.GetValue<float>("cash");

            DocumentSnapshot tSnap = await users.Document(user.Id.ToString()).GetSnapshotAsync();
            float tCash = tSnap.GetValue<float>("cash");

            if (amount > aCash) return CommandResult.FromError($"{Context.User.Mention}, you do not have that much money!");

            await CashSystem.SetCash(Context.User as IGuildUser, aCash - amount);
            await CashSystem.SetCash(user, tCash + amount);

            await ReplyAsync($"{Context.User.Mention}, you have sauced **{user.ToString()}** {amount.ToString("C2")}.");
            return CommandResult.FromSuccess();
        }

        [Alias("kms", "selfend")]
        [Command("suicide")]
        [Summary("Kill yourself.")]
        [Remarks("``$suicide``")]
        public async Task<RuntimeResult> Suicide()
        {
            Random random = new Random();
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("usingSlots", out bool usingSlots) && usingSlots)
                return CommandResult.FromError($"{Context.User.Mention}, you appear to be currently gambling. I cannot do any transactions at the moment.");

            switch (random.Next(4))
            {
                case 0:
                    await ReplyAsync($"{Context.User.Mention}, you attempted to hang yourself, but the rope snapped. Your parents found out and you got an ass whooping, but you did not die.");
                    break;
                case 1:
                    await ReplyAsync($"{Context.User.Mention}, you shot yourself, but somehow the bullet didn't kill you. Lucky or unlucky?");
                    break;
                case 2:
                    await ReplyAsync($"{Context.User.Mention}, DAMN that shotgun made a fucking mess out of you! You're DEAD DEAD, and lost everything.");
                    await doc.DeleteAsync();
                    await CashSystem.SetCash(Context.User as IGuildUser, 10);
                    break;
                case 3:
                    await ReplyAsync($"{Context.User.Mention}, it was quite a struggle, but the noose put you out of your misery. You lost everything.");
                    await doc.DeleteAsync();
                    await CashSystem.SetCash(Context.User as IGuildUser, 10);
                    break;
            }

            return CommandResult.FromSuccess();
        }
    }
}
