using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
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
            ulong userId = user == null ? Context.User.Id : user.Id;
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(userId.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            if (snap.TryGetValue("cash", out float cash))
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
    }
}
