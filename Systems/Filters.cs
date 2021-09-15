using Discord;
using Discord.Commands;
using Discord.Rest;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RRBot.Systems
{
    public static class Filters
    {
        public static readonly Regex INVITE_REGEX = new(@"discord(?:app.com\/invite|.gg|.me|.io)(?:[\\]+)?\/([a-zA-Z0-9\-]+)");
        public static readonly Regex NWORD_REGEX = new("[nɴⁿₙñńņňÑŃŅŇ][i1!¡ɪᶦᵢ¹₁jįīïîíìl|;:¡][g9ɢᵍ𝓰𝓰qģğĢĞ][g9ɢᵍ𝓰𝓰qģğĢĞ][e3€ᴇᵉₑ³₃ĖĘĚĔėęěĕəèéêëē][rʀʳᵣŔŘŕř]");

        public static async Task DoInviteCheckAsync(SocketCommandContext context)
        {
            foreach (Match match in INVITE_REGEX.Matches(context.Message.Content))
            {
                string inviteCode = match.Groups[1].Value;
                RestInviteMetadata invite = await context.Client.GetInviteAsync(inviteCode);
                if (invite != null) await context.Message.DeleteAsync();
            }
        }

        public static async Task DoNWordCheckAsync(SocketCommandContext context)
        {
            if (context.Channel.Name != "extremely-funny" && NWORD_REGEX.Matches(new string(context.Message.Content.Where(char.IsLetter).ToArray()).ToLower()).Count != 0)
            {
                await Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(10));
                    await context.Message.DeleteAsync();
                });
            }
        }

        public static async Task DoScamCheckAsync(SocketCommandContext context)
        {
            foreach (Embed epicEmbed in context.Message.Embeds)
            {
                if ((context.Message.Content.Contains("skins") && context.Message.Content.Contains("imgur"))
                    || (epicEmbed.Title.StartsWith("Trade offer", StringComparison.Ordinal) && !epicEmbed.Url.Contains("steamcommunity.com"))
                    || (epicEmbed.Title.StartsWith("Steam Community", StringComparison.Ordinal) && !epicEmbed.Url.Contains("steamcommunity.com"))
                    || epicEmbed.Title.StartsWith("Free Discord Nitro"))
                {
                    await context.Message.DeleteAsync();
                    break;
                }
            }
        }
    }
}
