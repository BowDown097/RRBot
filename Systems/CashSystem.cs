using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using RRBot.Extensions;
using RRBot.Modules;

namespace RRBot.Systems
{
    public static class CashSystem
    {
        public static async Task<float> CashFromString(IGuildUser user, string cashStr)
        {
            float.TryParse(cashStr, out float cash);
            if (cashStr.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                DocumentReference doc = Program.database.Collection($"servers/{user.GuildId}/users").Document(user.Id.ToString());
                DocumentSnapshot snap = await doc.GetSnapshotAsync();
                cash = snap.GetValue<float>("cash");
            }

            return cash;
        }

        public static async Task SetCash(IGuildUser user, ISocketMessageChannel channel, float amount)
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

                    if (amount >= neededCash && !user.RoleIds.Contains(roleId))
                    {
                        IRole role = user.Guild.GetRole(roleId);
                        bool rankupNotify = await UserSettingsGetters.GetRankupNotifications(user);
                        if (rankupNotify)
                            await (user as SocketUser).NotifyAsync(channel, $"**{user.ToString()}** has ranked up to {role.Name}!",
                                 $"{user.Mention}, you have ranked up to {role.Name}!", true);

                        await user.AddRoleAsync(roleId);
                    }
                    else if (amount <= neededCash && user.RoleIds.Contains(roleId))
                    {
                        IRole role = user.Guild.GetRole(roleId);
                        bool rankupNotify = await UserSettingsGetters.GetRankupNotifications(user);
                        if (rankupNotify)
                            await (user as SocketUser).NotifyAsync(channel, $"**{user.ToString()}** has lost {role.Name}!", $"{user.Mention}, you lost {role.Name}!", true);

                        await user.RemoveRoleAsync(roleId);
                    }
                }
            }
        }

        public static async Task TryMessageReward(SocketCommandContext context)
        {
            DocumentReference doc = Program.database.Collection($"servers/{context.Guild.Id}/users").Document(context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            if (snap.TryGetValue("timeTillCash", out long time) && time <= DateTimeOffset.UtcNow.ToUnixTimeSeconds()) 
                await SetCash(context.User as IGuildUser, context.Channel, snap.GetValue<float>("cash") + 10);

            await doc.SetAsync(new { timeTillCash = DateTimeOffset.UtcNow.ToUnixTimeSeconds(TimeSpan.FromMinutes(1).TotalSeconds) }, SetOptions.MergeAll);
        }
    }
}
