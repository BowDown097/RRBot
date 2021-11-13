namespace RRBot.Interactions
{
    public static class TriviaInteractions
    {
        private static async Task<string> AnswerAt(Embed embed, string num)
        {
            using StringReader reader = new(embed.Description);
            for (string line = await reader.ReadLineAsync(); line != null; line = await reader.ReadLineAsync())
            {
                if (line.StartsWith(num))
                    return line[3..];
            }

            return "";
        }

        public static async Task Respond(SocketMessageComponent component, string num, bool correct)
        {
            Embed embed = component.Message.Embeds.First();
            string answer = await AnswerAt(embed, num);

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
}