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
                cash = (float)Math.Round(cash, 2);
                await ReplyAsync(user == null ? $"{Context.User.Mention}, you have **${cash}**." : $"**{user.ToString()}** has **${cash}**.");
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

            string description = string.Empty;
            if (snap.TryGetValue("rapeCooldown", out long rapeCd) && rapeCd - Global.UnixTime() > 0L) description += $"**Rape**: {Global.FormatTime(rapeCd - Global.UnixTime())}\n";
            if (snap.TryGetValue("whoreCooldown", out long whoreCd) && whoreCd - Global.UnixTime() > 0L) description += $"**Whore**: {Global.FormatTime(whoreCd - Global.UnixTime())}\n";
            if (snap.TryGetValue("lootCooldown", out long lootCd) && lootCd - Global.UnixTime() > 0L) description += $"**Loot**: {Global.FormatTime(lootCd - Global.UnixTime())}\n";

            EmbedBuilder embed = new EmbedBuilder
            {
                Title = "Crime Cooldowns",
                Color = Color.Blue,
                Description = description ?? "None"
            };

            await ReplyAsync(embed: embed.Build());
        }

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
    }
}
