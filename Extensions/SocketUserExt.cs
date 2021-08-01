using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using RRBot.Modules;

namespace RRBot.Extensions
{
    public static class SocketUserExt
    {
        public static async Task AddToStatsAsync(this SocketUser user, CultureInfo culture, SocketGuild guild, Dictionary<string, string> statsToAddTo)
        {
            DocumentReference doc = Program.database.Collection($"servers/{guild.Id}/users").Document(user.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (!snap.TryGetValue("stats", out Dictionary<string, string> userStats)) userStats = new Dictionary<string, string>();

            foreach (KeyValuePair<string, string> kvp in statsToAddTo)
            {
                if (userStats.ContainsKey(kvp.Key))
                {
                    if (kvp.Value[0] == '$')
                    {
                        double oldValue = double.Parse(userStats[kvp.Key][1..]);
                        double toAdd = double.Parse(kvp.Value[1..]);
                        userStats[kvp.Key] = (oldValue + toAdd).ToString("C2", culture);
                    }
                    else
                    {
                        double oldValue = double.Parse(userStats[kvp.Key]);
                        double toAdd = double.Parse(kvp.Value);
                        userStats[kvp.Key] = (oldValue + toAdd).ToString();
                    }
                }
                else
                {
                    userStats.Add(kvp.Key, kvp.Value);
                }
            }

            await doc.SetAsync(new { stats = userStats }, SetOptions.MergeAll);
        }

        public static async Task<IUserMessage> NotifyAsync(this SocketUser user, ISocketMessageChannel channel, string message, string altMsg = "", bool doDM = false)
        {
            if (doDM)
            {
                bool dmNotify = await UserSettingsGetters.GetDMNotifications(user as IGuildUser);
                string reply = string.IsNullOrEmpty(altMsg) ? message : altMsg;
                if (dmNotify) return await user.SendMessageAsync(reply);
            }

            bool replyPings = await UserSettingsGetters.GetReplyPings(user as IGuildUser);
            if (!string.IsNullOrEmpty(altMsg))
                message = replyPings ? altMsg : message;
            else
                message = replyPings ? $"{user.Mention}, {char.ToLowerInvariant(message[0]) + message[1..]}" : message;

            return await channel.SendMessageAsync(message);
        }
    }
}
