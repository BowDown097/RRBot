using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Google.Cloud.Firestore;

namespace RRBot.Systems
{
    public static class CashSystem
    {
        public static async Task SetCash(IGuildUser user, float amount)
        {
            if (user.IsBot) return;
            if (amount < 0) amount = 0;

            amount = (float)Math.Round(amount, 2);
            DocumentReference userDoc = Program.database.Collection($"servers/{user.GuildId}/users").Document(user.Id.ToString());
            await userDoc.SetAsync(new { cash = amount }, SetOptions.MergeAll);

            DocumentReference ranksDoc = Program.database.Collection($"servers/{user.GuildId}/config").Document("ranks");
            DocumentSnapshot snap = await ranksDoc.GetSnapshotAsync();
            if (snap.Exists)
            {
                foreach (KeyValuePair<string, object> kvp in snap.ToDictionary().Where(kvp => kvp.Key.EndsWith("Id", StringComparison.Ordinal)))
                {
                    float neededCash = snap.GetValue<float>(kvp.Key.Replace("Id", "Cost"));
                    ulong roleId = Convert.ToUInt64(kvp.Value);
                    if (amount >= neededCash && !user.RoleIds.Contains(roleId)) await user.AddRoleAsync(roleId);
                    else if (amount <= neededCash && user.RoleIds.Contains(roleId)) await user.RemoveRoleAsync(roleId);
                }
            }
        }

        public static async Task TryMessageReward(SocketCommandContext context)
        {
            DocumentReference doc = Program.database.Collection($"servers/{context.Guild.Id}/users").Document(context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            if (snap.TryGetValue("timeTillCash", out long time) && time <= Global.UnixTime()) await SetCash(context.User as IGuildUser, snap.GetValue<float>("cash") + 10);

            await doc.SetAsync(new { timeTillCash = Global.UnixTime(TimeSpan.FromMinutes(1).TotalSeconds) }, SetOptions.MergeAll);
        }
    }
}
