using Discord.Interactions;

namespace RRBot.Interactions;
public class Trivia : InteractionModuleBase<SocketInteractionContext<SocketMessageComponent>>
{
    [ComponentInteraction("trivia-*-*")]
    public async Task Respond(string num, bool correct)
    {
        Embed embed = Context.Interaction.Message.Embeds.First();
        string answer = Array.Find(embed.Description.Split('\n'), l => l.StartsWith(num))[3..];

        if (correct)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Trivia Over!")
                .WithDescription($"**{Context.Interaction.User}** was the first to get the correct answer of \"{answer}\"!\n~~{embed.Description}~~");
            await Context.Interaction.UpdateAsync(resp => {
                resp.Embed = embedBuilder.Build();
                resp.Components = null;
            });
        }
        else
        {
            await Context.Interaction.RespondAsync($"Big L! \"{answer}\" is not the correct answer.", ephemeral: true);
        }
    }
}