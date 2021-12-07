namespace RRBot.Interactions;
public static class TriviaInteractions
{
    public static async Task Respond(SocketMessageComponent component, string num, bool correct)
    {
        Embed embed = component.Message.Embeds.First();
        string answer = Array.Find(embed.Description.Split('\n'), l => l.StartsWith(num))[3..];

        if (correct)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Trivia Over!")
                .WithDescription($"**{component.User}** was the first to get the correct answer of \"{answer}\"!\n~~{embed.Description}~~");
            await component.UpdateAsync(resp => {
                resp.Embed = embedBuilder.Build();
                resp.Components = null;
            });
        }
        else
        {
            await component.RespondAsync($"Big L! \"{answer}\" is not the correct answer.", ephemeral: true);
        }
    }
}