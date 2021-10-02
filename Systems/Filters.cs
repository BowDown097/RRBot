using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RRBot.Systems
{
    public static class Filters
    {
        public static readonly Regex INVITE_REGEX = new(@"discord(?:app.com\/invite|.gg|.me|.io)(?:[\\]+)?\/([a-zA-Z0-9\-]+)");
        public static readonly Regex NWORD_REGEX = new("[nɴⁿₙñńņňÑŃŅŇℕ𝒩][i1!¡ɪᶦᵢ¹₁jįīïîíìl|;:𝕀ℐ][g9ɢᵍ𝓰𝓰qģğĢĞ𝔾𝒢][g9ɢᵍ𝓰𝓰qģğĢĞ𝔾𝒢][e3€ᴇᵉₑ³₃ĖĘĚĔėęěĕəèéêëēеЕ£ℇ𝔼ℰ][rʀʳᵣŔŘŕřяℝℛ]");

        public static async Task DoInviteCheckAsync(SocketUserMessage message, DiscordSocketClient client)
        {
            foreach (Match match in INVITE_REGEX.Matches(message.Content))
            {
                string inviteCode = match.Groups[1].Value;
                RestInviteMetadata invite = await client.GetInviteAsync(inviteCode);
                if (invite != null)
                    await message.DeleteAsync();
            }
        }

        public static async Task DoNWordCheckAsync(SocketUserMessage message, ISocketMessageChannel channel)
        {
            char[] cleaned = message.Content.Where(c => char.IsLetterOrDigit(c) || char.IsSymbol(c) || char.IsPunctuation(c)).ToArray();
            if (channel.Name != "extremely-funny" && NWORD_REGEX.Matches(new string(cleaned).ToLower()).Count != 0)
                await message.DeleteAsync();
        }

        public static async Task DoScamCheckAsync(SocketUserMessage message)
        {
            string content = message.Content.ToLower();
            if ((content.Contains("skins") && content.Contains("imgur"))
                || (content.Contains("nitro") && content.Contains("free") && content.Contains("http")))
            {
                await message.DeleteAsync();
                return;
            }

            foreach (Embed epicEmbed in message.Embeds)
            {
                if ((epicEmbed.Title.StartsWith("Trade offer") && !epicEmbed.Url.Contains("steamcommunity.com"))
                    || (epicEmbed.Title.StartsWith("Steam Community") && !epicEmbed.Url.Contains("steamcommunity.com")))
                {
                    await message.DeleteAsync();
                }
            }
        }
    }
}
