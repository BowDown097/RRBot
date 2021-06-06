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
            if (amount < 0) amount = 0;

            DocumentReference userDoc = Program.database.Collection($"servers/{user.GuildId}/users").Document(user.Id.ToString());
            await userDoc.SetAsync(new { cash = amount }, SetOptions.MergeAll);

            DocumentReference ranksDoc = Program.database.Collection($"servers/{user.GuildId}/config").Document("ranks");
            DocumentSnapshot snap = await ranksDoc.GetSnapshotAsync();
            if (snap.Exists)
            {
                foreach (KeyValuePair<string, object> kvp in snap.ToDictionary())
                {
                    float neededCash = Convert.ToSingle(kvp.Value);
                    ulong roleId = ulong.Parse(kvp.Key);
                    if (amount >= neededCash && !user.RoleIds.Contains(roleId)) await user.AddRoleAsync(user.Guild.GetRole(roleId));
                }
            }
        }

        public static async Task TryMessageReward(SocketCommandContext context)
        {
            DocumentReference doc = Program.database.Collection($"servers/{context.Guild.Id}/users").Document(context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            if (snap.TryGetValue("cash", out float cash) && snap.TryGetValue("timeTillCash", out long time))
            {
                if (time <= Global.UnixTime())
                {
                    await SetCash(context.User as IGuildUser, cash + 10);
                }
            }
            else
            {
                await SetCash(context.User as IGuildUser, 10);
            }

            await doc.SetAsync(new { timeTillCash = Global.UnixTime(TimeSpan.FromMinutes(1).TotalSeconds) }, SetOptions.MergeAll);
        }
    }
}
