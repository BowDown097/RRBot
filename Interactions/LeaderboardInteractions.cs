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
        public static async Task GetNext(SocketMessageComponent component, ulong executorId)
        {
            Embed embed = component.Message.Embeds.FirstOrDefault();
            string currency = embed.Title[..embed.Title.IndexOf(' ')];
            string lastEntry = embed.Description.Split('\n').Last();
            int userIndex = int.Parse(lastEntry[..lastEntry.IndexOf(':')]) + 1;
            int startingUser = userIndex;

            SocketGuild guild = (component.User as SocketGuildUser)?.Guild;
            QuerySnapshot users = await Program.database.Collection($"servers/{guild.Id}/users")
                .OrderByDescending(currency.ToLower()).GetSnapshotAsync();
            StringBuilder lb = new();
            foreach (DocumentSnapshot doc in users.Documents.Skip(startingUser))
            {
                if (userIndex == startingUser + 10)
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

                Console.WriteLine(userIndex);
                lb.AppendLine($"{userIndex}: **{guildUser}**: {(currency == "Cash" ? val.ToString("C2") : val.ToString("0.####"))}");
                userIndex++;
            }

            EmbedBuilder embedBuilder = embed.ToEmbedBuilder()
                .WithDescription(lb.Length > 0 ? lb.ToString() : "Nothing to see here!");
            ComponentBuilder componentBuilder = new ComponentBuilder()
                .WithButton("Next", $"lbnext-{executorId}", disabled: users.Documents.Count < startingUser + 10);
            await component.UpdateAsync(resp => {
                resp.Embed = embedBuilder.Build();
                resp.Components = componentBuilder.Build();
            });
        }
    }
}