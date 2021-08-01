using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace RRBot.Systems
{
    public static class Filters
    {
        public static readonly Regex FUNNY_REGEX = new("[nɴⁿₙñńņňÑŃŅŇ][i1!¡ɪᶦᵢ¹₁jįīïîíìl|;:][g9ɢᵍ𝓰𝓰qģğĢĞ][g9ɢᵍ𝓰𝓰qģğĢĞ][e3€ᴇᵉₑ³₃ĖĘĚĔėęěĕəèéêëē][rʀʳᵣŔŘŕř]");

        public static async Task DoScamCheckAsync(SocketCommandContext context)
        {
            foreach (Embed epicEmbed in context.Message.Embeds)
            {
                if ((context.Message.Content.Contains("skins") && context.Message.Content.Contains("imgur"))
                    || (epicEmbed.Title.StartsWith("Trade offer", StringComparison.Ordinal) && !epicEmbed.Url.Contains("steamcommunity"))
                    || (epicEmbed.Title.StartsWith("Steam Community", StringComparison.Ordinal) && epicEmbed.Url.Contains("y.ru")))
                {
                    await context.Message.DeleteAsync();
                    break;
                }
            }
        }

        public static async Task DoNWordCheckAsync(SocketCommandContext context)
        {
            if (context.Channel.Name != "extremely-funny" && FUNNY_REGEX.Matches(new string(context.Message.Content.Where(char.IsLetter).ToArray()).ToLower()).Count != 0)
            {
                await Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(10));
                    await context.Message.DeleteAsync();
                });
            }
        }
    }
}
