using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;

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
                await ReplyAsync(user == null ? $"{Context.User.Mention}, you have **${string.Format("{0:0.00}", cash)}**." 
                : $"**{user.ToString()}** has **${string.Format("{0:0.00}", cash)}**.");
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
            if (snap.TryGetValue("rapeCooldown", out long rapeCd) && rapeCd - Global.UnixTime() > 0L) 
                description.AppendLine($"**Rape**: {Global.FormatTime(rapeCd - Global.UnixTime())}");
            if (snap.TryGetValue("whoreCooldown", out long whoreCd) && whoreCd - Global.UnixTime() > 0L) 
                description.AppendLine($"**Whore**: {Global.FormatTime(whoreCd - Global.UnixTime())}");
            if (snap.TryGetValue("lootCooldown", out long lootCd) && lootCd - Global.UnixTime() > 0L) 
                description.AppendLine($"**Loot**: {Global.FormatTime(lootCd - Global.UnixTime())}");
            if (snap.TryGetValue("slaveryCooldown", out long slaveryCd) && slaveryCd - Global.UnixTime() > 0L)
                description.AppendLine($"**Slavery**: {Global.FormatTime(slaveryCd - Global.UnixTime())}");

            EmbedBuilder embed = new EmbedBuilder
            {
                Title = "Crime Cooldowns",
                Color = Color.Red,
                Description = description.Length > 0 ? description.ToString() : "None"
            };

            await ReplyAsync(embed: embed.Build());
        }

        /*
        [Alias("lb")]
        [Command("leaderboard")]
        [Summary("Check the leaderboard.")]
        [Remarks("``$leaderboard``")]
        public async Task Leaderboard()
        {
            await ReplyAsync("Fetching leaderboard.. (this may take a while, db requests aren't very fast)");
            // slow currently, i don't think i can make it faster. since it's slow, gotta run in background so bot actually works
            Global.RunInBackground(() =>
            {
                IAsyncEnumerable<DocumentReference> users = Program.database.Collection($"servers/{Context.Guild.Id}/users").ListDocumentsAsync();
                IAsyncEnumerable<DocumentReference> topTenUsers = users.OrderByDescendingAwait(async user => (await user.GetSnapshotAsync()).GetValue<float>("cash")).Take(10);

                IEnumerable<DocumentReference> tTUEnum = topTenUsers.ToEnumerable();
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < tTUEnum.Count(); i++)
                {
                    DocumentReference doc = tTUEnum.ElementAt(i);
                    DocumentSnapshot snap = doc.GetSnapshotAsync().Result;
                    SocketGuildUser user = Context.Guild.GetUser(Convert.ToUInt64(doc.Id));
                    if (user == null) continue;

                    float cash = snap.GetValue<float>("cash");
                    builder.AppendLine($"{i + 1}: **{user.ToString()}**: ${Math.Round(cash, 2)}");
                }

                EmbedBuilder embed = new EmbedBuilder
                {
                    Color = Color.Red,
                    Title = "Leaderboard",
                    Description = builder.ToString()
                };
                ReplyAsync(embed: embed.Build());
            });
        }
        */

        [Alias("roles")]
        [Command("ranks")]
        [Summary("View all the ranks and their costs.")]
        [Remarks("``$ranks``")]
        public async Task ViewRanks()
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
                    ranks.AppendLine($"**{role.Name}**: ${string.Format("{0:0.00}", neededCash)}");
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
    }
}
