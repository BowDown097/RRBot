using Discord;
using Discord.Commands;
using Discord.Rest;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RRBot.Systems
{
    public static class Filters
    {
        public static readonly Regex INVITE_REGEX = new(@"discord(?:app.com\/invite|.gg|.me|.io)(?:[\\]+)?\/([a-zA-Z0-9\-]+)");
        public static readonly Regex NWORD_REGEX = new("[nɴⁿₙñńņňÑŃŅŇ][i1!¡ɪᶦᵢ¹₁jįīïîíìl|;:][g9ɢᵍ𝓰𝓰qģğĢĞ][g9ɢᵍ𝓰𝓰qģğĢĞ][e3€ᴇᵉₑ³₃ĖĘĚĔėęěĕəèéêëē][rʀʳᵣŔŘŕř]");

        public static async Task DoInviteCheckAsync(SocketCommandContext context)
        {
            foreach (Match match in INVITE_REGEX.Matches(context.Message.Content))
            {
                string inviteCode = match.Groups[1].Value;
                RestInviteMetadata invite = await context.Client.GetInviteAsync(inviteCode);
                if (invite != null)
                    await context.Message.DeleteAsync();
            }
        }

        public static async Task DoNWordCheckAsync(SocketCommandContext context)
        {
            char[] cleaned = context.Message.Content.Where(c => char.IsLetterOrDigit(c) || char.IsSymbol(c) || char.IsPunctuation(c)).ToArray();
            if (context.Channel.Name != "extremely-funny" && NWORD_REGEX.Matches(new string(cleaned).ToLower()).Count != 0)
                await context.Message.DeleteAsync();
        }

        public static async Task DoScamCheckAsync(SocketCommandContext context)
        {
            string content = context.Message.Content.ToLower();
            if ((content.Contains("skins") && content.Contains("imgur"))
                || (content.Contains("nitro") && content.Contains("free") && content.Contains("http")))
            {
                await context.Message.DeleteAsync();
                return;
            }

            foreach (Embed epicEmbed in context.Message.Embeds)
            {
                if ((epicEmbed.Title.StartsWith("Trade offer") && !epicEmbed.Url.Contains("steamcommunity.com"))
                    || (epicEmbed.Title.StartsWith("Steam Community") && !epicEmbed.Url.Contains("steamcommunity.com")))
                {
                    await context.Message.DeleteAsync();
                }
            }
        }
    }
}
