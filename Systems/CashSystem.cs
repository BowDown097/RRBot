using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RRBot.Extensions;
using RRBot.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
#pragma warning disable IDE0018 // Inline variable declaration

namespace RRBot.Systems
{
    public static class CashSystem
    {
        private static readonly WebClient client = new();

        public static async Task AddCrypto(SocketUser user, string crypto, double amount)
        {
            amount = Math.Round(amount, 4);
            IGuildUser guildUser = user as IGuildUser;
            DocumentReference doc = Program.database.Collection($"servers/{guildUser.GuildId}/users").Document(user.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            snap.TryGetValue(crypto, out double currentAmount);
            await doc.SetAsync(new Dictionary<string, double> { { crypto, currentAmount + amount } }, SetOptions.MergeAll);
        }

        public static async Task<double> QueryCryptoValue(string crypto)
        {
            string current = DateTime.Now.ToString("yyyy-MM-ddTHH:mm");
            string today = DateTime.Now.ToString("yyyy-MM-dd") + "T00:00";
            string data = await client.DownloadStringTaskAsync($"https://production.api.coindesk.com/v2/price/values/{crypto}?start_date={today}&end_date={current}");
            dynamic obj = JsonConvert.DeserializeObject(data);
            JToken latestEntry = JArray.FromObject(obj.data.entries).Last;
            return Math.Round(latestEntry[1].Value<double>(), 2);
        }

        public static async Task SetCash(SocketUser user, ISocketMessageChannel channel, double amount)
        {
            if (user.IsBot)
                return;
            if (amount < 0)
                amount = 0;

            IGuildUser guildUser = user as IGuildUser;
            amount = Math.Round(amount, 2) * Constants.CASH_MULTIPLIER;
            DocumentReference userDoc = Program.database.Collection($"servers/{guildUser.GuildId}/users").Document(user.Id.ToString());
            await userDoc.SetAsync(new { cash = amount }, SetOptions.MergeAll);

            DocumentReference ranksDoc = Program.database.Collection($"servers/{guildUser.GuildId}/config").Document("ranks");
            DocumentSnapshot snap = await ranksDoc.GetSnapshotAsync();
            if (snap.Exists)
            {
                foreach (KeyValuePair<string, object> kvp in snap.ToDictionary().Where(kvp => kvp.Key.EndsWith("Id", StringComparison.Ordinal)))
                {
                    double neededCash = snap.GetValue<double>(kvp.Key.Replace("Id", "Cost"));
                    ulong roleId = Convert.ToUInt64(kvp.Value);
                    IRole role = guildUser.Guild.GetRole(roleId);
                    bool rankupNotify = await UserSettingsGetters.GetRankupNotifications(guildUser);

                    if (amount >= neededCash && !guildUser.RoleIds.Contains(roleId))
                    {
                        if (rankupNotify)
                            await user.NotifyAsync(channel, $"**{user}** ranked up to {role.Name}!", $"You have ranked up to {role.Name}!", true);
                        await guildUser.AddRoleAsync(roleId);
                    }
                    else if (amount <= neededCash && guildUser.RoleIds.Contains(roleId))
                    {
                        if (rankupNotify)
                            await user.NotifyAsync(channel, $"**{user}** has lost {role.Name}!", $"You lost {role.Name}!", true);
                        await guildUser.RemoveRoleAsync(roleId);
                    }
                }
            }
        }

        public static async Task TryMessageReward(SocketCommandContext context)
        {
            DocumentReference doc = Program.database.Collection($"servers/{context.Guild.Id}/users").Document(context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            if (snap.TryGetValue("timeTillCash", out long time) && time <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                await SetCash(context.User, context.Channel, snap.GetValue<double>("cash") + Constants.MESSAGE_CASH);

            await doc.SetAsync(new { timeTillCash = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.MESSAGE_CASH_COOLDOWN) },
                SetOptions.MergeAll);
        }
    }
}
