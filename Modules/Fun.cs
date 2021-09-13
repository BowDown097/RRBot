using Discord;
using Discord.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RRBot.Modules
{
    [Summary("Commands that don't do anything related to the bot's systems: they just exist for fun (hence the name).")]
    public class Fun : ModuleBase<SocketCommandContext>
    {
        [Command("waifu")]
        [Summary("Get yourself a random waifu from our vast and sexy collection of scrumptious waifus.")]
        [Remarks("$waifu")]
        public async Task Waifu()
        {
            List<string> keys = Constants.WAIFUS.Keys.ToList();
            string waifu = keys[RandomUtil.Next(Constants.WAIFUS.Count)];

            EmbedBuilder waifuEmbed = new()
            {
                Color = Color.Red,
                Title = "Say hello to your new waifu!",
                Description = $"Your waifu is **{waifu}**.",
                ImageUrl = Constants.WAIFUS[waifu]
            };

            await ReplyAsync(embed: waifuEmbed.Build());
        }
    }
}
