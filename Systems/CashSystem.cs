using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RRBot.Entities;
using RRBot.Extensions;
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

        public static async Task<double> QueryCryptoValue(string crypto)
        {
            string current = DateTime.Now.ToString("yyyy-MM-ddTHH:mm");
            string today = DateTime.Now.ToString("yyyy-MM-dd") + "T00:00";
            string data = await client.DownloadStringTaskAsync($"https://production.api.coindesk.com/v2/price/values/{crypto}?start_date={today}&end_date={current}");
            dynamic obj = JsonConvert.DeserializeObject(data);
            JToken latestEntry = JArray.FromObject(obj.data.entries).Last;
            return Math.Round(latestEntry[1].Value<double>(), 2);
        }

        public static async Task TryMessageReward(SocketCommandContext context)
        {
            DbUser user = await DbUser.GetById(context.Guild.Id, context.User.Id);

            if (user.TimeTillCash == 0)
            {
                user.TimeTillCash = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.MESSAGE_CASH_COOLDOWN);
            }
            else if (user.TimeTillCash <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                await user.SetCash(context.User, context.Channel, user.Cash + Constants.MESSAGE_CASH);
                user.TimeTillCash = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.MESSAGE_CASH_COOLDOWN);
            }

            await user.Write();
        }
    }
}
