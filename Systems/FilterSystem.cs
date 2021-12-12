namespace RRBot.Systems;
public static class FilterSystem
{
    private static readonly Regex INVITE_REGEX = new(@"discord(?:app.com\/invite|.gg|.me|.io)(?:[\\]+)?\/([a-zA-Z0-9\-]+)");
    private static readonly Regex NWORD_REGEX = new("[nɴⁿₙñńņňÑŃŅŇℕ𝒩][i1!¡ɪᶦᵢ¹₁jįīïîíìl|;:𝕀ℐ][g9ɢᵍ𝓰𝓰qģğĢĞ𝔾𝒢][g9ɢᵍ𝓰𝓰qģğĢĞ𝔾𝒢][e3€ᴇᵉₑ³₃ĖĘĚĔėęěĕəèéêëēеЕ£ℇ𝔼ℰ][rʀʳᵣŔŘŕřяℝℛ]");
    private const string NWORD_SPCHARS = "𝒩!¡¹₁|;:𝕀𝓰𝓰𝔾𝒢𝓰𝓰𝔾𝒢€³₃£𝔼";

    public static bool ContainsNWord(string input)
    {
        string cleaned = new(input.Where(c => char.IsLetterOrDigit(c) || NWORD_SPCHARS.Contains(c)).ToArray());
        return NWORD_REGEX.IsMatch(cleaned.ToLower());
    }

    public static async Task DoInviteCheckAsync(SocketUserMessage message, SocketGuild guild, DiscordSocketClient client)
    {
        DbConfigOptionals optionals = await DbConfigOptionals.GetById(guild.Id);
        if (!optionals.InviteFilterEnabled || optionals.NoFilterChannels.Contains(message.Channel.Id))
            return;

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
        if (!channel.Name.In("extremely-funny", "bot-commands-for-retards", "private-godfather") && ContainsNWord(message.Content))
            await message.DeleteAsync();
    }

    public static async Task DoScamCheckAsync(SocketUserMessage message, SocketGuild guild)
    {
        DbConfigOptionals optionals = await DbConfigOptionals.GetById(guild.Id);
        if (!optionals.ScamFilterEnabled || optionals.NoFilterChannels.Contains(message.Channel.Id))
            return;

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
                if ((epicEmbed.Title?.StartsWith("Trade offer") == true && host != "steamcommunity.com")
                    || (epicEmbed.Title?.StartsWith("Steam Community") == true && host != "steamcommunity.com")
                    || (epicEmbed.Title?.StartsWith("You've been gifted") == true && host != "discord.gift"))
                {
                    await message.DeleteAsync();
                }
            }
        }
    }
}