using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Google.Cloud.Firestore;
using RRBot.Extensions;

namespace RRBot.Modules
{
    public static class UserSettingsGetters
    {
        private static async Task<bool> GenericGet(IGuildUser user, string path, bool @default)
        {
            DocumentReference doc = Program.database.Collection($"servers/{user.GuildId}/users").Document(user.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue(path, out bool status)) return status;

            return @default;
        }

        public static async Task<bool> GetDMNotifications(IGuildUser user) => await GenericGet(user, "dmNotifs", false);
        public static async Task<bool> GetRankupNotifications(IGuildUser user) => await GenericGet(user, "rankupNotifs", false);
        public static async Task<bool> GetReplyPings(IGuildUser user) => await GenericGet(user, "replyPings", true);
    }

    public class UserSettings : ModuleBase<SocketCommandContext>
    {
        private async Task GenericSet(object documentData)
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            await doc.SetAsync(documentData, SetOptions.MergeAll);
        }

        [Command("mysettings")]
        [Summary("List your user settings.")]
        [Remarks("``$mysettings``")]
        public async Task MySettings()
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            StringBuilder description = new StringBuilder();
            description.AppendLine($"**DM Notifications**: {snap.TryGetValue("dmNotifs", out bool dmNotifs) && dmNotifs}");
            description.AppendLine($"**Rankup Notifications**: {snap.TryGetValue("rankupNotifs", out bool rankupNotifs) && rankupNotifs}");
            description.AppendLine($"**Reply Pings**: {!snap.TryGetValue("replyPings", out bool replyPings) || replyPings}");

            EmbedBuilder embed = new EmbedBuilder
            {
                Title = "Your Settings",
                Color = Color.Red,
                Description = description.ToString()
            };

            await ReplyAsync(embed: embed.Build());
        }

        [Alias("setdmnotifs")]
        [Command("setdmnotifications")]
        [Summary("Set whether or not you will be DM'd by commands/general notifications that support it. *(default: false)*")]
        [Remarks("``$setdmnotifications [true/false]``")]
        public async Task SetDMNotifications(bool status) 
        {
            await GenericSet(new { dmNotifs = status });
            await Context.User.NotifyAsync(Context.Channel, $"You will {(status ? "now see" : "no longer see")} DM notifications.");
        }

        [Alias("setrankupnotifs")]
        [Command("setrankupnotifications")]
        [Summary("Set whether or not you will be notified of rank-ups/deranks. *(default: false)*")]
        [Remarks("``$setrankupnotifications [true/false]``")]
        public async Task SetRankupNotifications(bool status)
        {
            await GenericSet(new { rankupNotifs = status });
            await Context.User.NotifyAsync(Context.Channel, $"You will {(status ? "now see" : "no longer see")} rankup notifications.");
        }

        [Command("setreplypings")]
        [Summary("Set whether or not you will be pinged in command responses (that are not errors). *(default: true)*")]
        [Remarks("``$setreplypings [true/false]``")]
        public async Task SetReplyPings(bool status) 
        {
            await GenericSet(new { replyPings = status });
            await Context.User.NotifyAsync(Context.Channel, $"You will {(status ? "now be" : "no longer be")} pinged in command responses (that are not errors).");
        }
    }
}
