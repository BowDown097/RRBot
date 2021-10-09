using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using RRBot.Entities;

namespace RRBot.Interactions
{
    public static class LeaderboardInteractions
    {
        public static async Task GetNext(SocketMessageComponent component, ulong executorId, string currency, int start, int end)
        {
            Embed embed = component.Message.Embeds.FirstOrDefault();
            SocketGuild guild = (component.User as SocketGuildUser)?.Guild;

            QuerySnapshot users = await Program.database.Collection($"servers/{guild.Id}/users")
                .OrderByDescending(currency.ToLower()).GetSnapshotAsync();
            StringBuilder lb = new();
            int processedUsers = 0;
            foreach (DocumentSnapshot doc in users.Documents.Skip(start - 1))
            {
                if (processedUsers == 10)
                    break;

                SocketGuildUser guildUser = guild.GetUser(Convert.ToUInt64(doc.Id));
                if (guildUser == null)
                    continue;

                DbUser dbUser = await DbUser.GetById(guild.Id, guildUser.Id);
                if (dbUser.Perks.ContainsKey("Pacifist"))
                    continue;

                double val = (double)dbUser[currency];
                if (val < Constants.INVESTMENT_MIN_AMOUNT)
                    break;

                lb.AppendLine($"{start + processedUsers}: **{guildUser}**: {(currency == "Cash" ? val.ToString("C2") : val.ToString("0.####"))}");
                processedUsers++;
            }

            EmbedBuilder embedBuilder = embed.ToEmbedBuilder()
                .WithDescription(lb.Length > 0 ? lb.ToString() : "Nothing to see here!");
            ComponentBuilder componentBuilder = new ComponentBuilder()
                .WithButton("Back", $"lbnext-{executorId}-{currency}-{start-10}-{end-10}", disabled: end <= 10)
                .WithButton("Next", $"lbnext-{executorId}-{currency}-{end+1}-{end+10}", disabled: processedUsers != 10 || users.Documents.Count < end + 1);
            await component.UpdateAsync(resp => {
                resp.Embed = embedBuilder.Build();
                resp.Components = componentBuilder.Build();
            });
        }
    }
}