namespace RRBot.Systems
{
    public static class Filters
    {
        public static readonly Regex INVITE_REGEX = new(@"discord(?:app.com\/invite|.gg|.me|.io)(?:[\\]+)?\/([a-zA-Z0-9\-]+)");
        public static readonly Regex NWORD_REGEX = new("[nɴⁿₙñńņňÑŃŅŇℕ𝒩][i1!¡ɪᶦᵢ¹₁jįīïîíìl|;:𝕀ℐ][g9ɢᵍ𝓰𝓰qģğĢĞ𝔾𝒢][g9ɢᵍ𝓰𝓰qģğĢĞ𝔾𝒢][e3€ᴇᵉₑ³₃ĖĘĚĔėęěĕəèéêëēеЕ£ℇ𝔼ℰ][rʀʳᵣŔŘŕřяℝℛ]");
        public static readonly string NWORD_SPCHARS = "𝒩!¡¹₁|;:𝕀𝓰𝓰𝔾𝒢𝓰𝓰𝔾𝒢€³₃£𝔼";

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

        public static async Task DoNWordCheckAsync(SocketUserMessage message, IMessageChannel channel)
        {
            string cleaned = new string(message.Content
                .Where(c => char.IsLetterOrDigit(c) || NWORD_SPCHARS.Contains(c))
                .ToArray()).ToLower();
            if (!channel.Name.In("extremely-funny", "bot-commands-for-retards", "private-godfather") && NWORD_REGEX.IsMatch(cleaned))
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
                if (Uri.TryCreate(epicEmbed.Url, UriKind.Absolute, out Uri uri))
                {
                    string host = uri.Host.Replace("www.", "").ToLower();
                    if ((epicEmbed.Title.StartsWith("Trade offer") && host != "steamcommunity.com")
                        || (epicEmbed.Title.StartsWith("Steam Community") && host != "steamcommunity.com")
                        || (epicEmbed.Title.StartsWith("You've been gifted") && host != "discord.gift"))
                    {
                        await message.DeleteAsync();
                    }
                }
            }
        }
    }
}
