using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using RRBot.Entities;

namespace RRBot.Systems
{
    public static class Achievements
    {
        public static async Task UnlockAchievement(string name, string desc, SocketUser user, SocketGuild guild, ISocketMessageChannel channel, double reward = 0)
        {
            DbUser dbUser = await DbUser.GetById(guild.Id, user.Id);
            if (dbUser.Achievements.ContainsKey(name))
                return;

            dbUser.Achievements.Add(name, desc);
            string description = $"GG {user}, you unlocked an achievement.\n**{name}**: {desc}";
            if (reward != 0)
            {
                dbUser.Cash += reward;
                description += $"\nReward: {reward:C2}";
            }

            EmbedBuilder embed = new()
            {
                Color = Color.Red,
                Title = "Achievement Get!",
                Description = description
            };

            await channel.SendMessageAsync(embed: embed.Build());
            await dbUser.Write();
        }
    }
}
