using Discord.Interactions;

namespace RRBot.Interactions;
public class Leaderboard : InteractionModuleBase<SocketInteractionContext<SocketMessageComponent>>
{
    [ComponentInteraction("lbnext-*-*-*-*-*-*")]
    public async Task GetNext(string executorIdStr, string currency, string startStr, string endStr, string failedUsersStr, string backStr)
    {
        ulong executorId = Convert.ToUInt64(executorIdStr);
        int start = Convert.ToInt32(startStr);
        int end = Convert.ToInt32(endStr);
        int failedUsers = Convert.ToInt32(failedUsersStr);
        bool back = Convert.ToBoolean(backStr);

        Embed embed = Context.Interaction.Message.Embeds.FirstOrDefault();
        IGuild guild = Context.Interaction.User.GetGuild();

        double cryptoValue = currency != "Cash" ? await Investments.QueryCryptoValue(currency) : 0;
        QuerySnapshot users = await Program.database.Collection($"servers/{guild.Id}/users")
            .OrderByDescending(currency).GetSnapshotAsync();
        StringBuilder lb = new("*Note: The leaderboard updates every 10 years, so stuff may not be up to date.*\n");
        int processedUsers = 10;
        foreach (DocumentSnapshot doc in users.Documents.Skip(start - 1 + failedUsers))
        {
            if (processedUsers == 10)
                break;

            IGuildUser guildUser = await guild.GetUserAsync(Convert.ToUInt64(doc.Id));
            if (guildUser == null)
            {
                if (!back) failedUsers++;
                continue;
            }

            DbUser dbUser = await DbUser.GetById(guild.Id, guildUser.Id, false);
            if (dbUser.Perks.ContainsKey("Pacifist"))
            {
                if (!back) failedUsers++;
                continue;
            }

            double val = (double)dbUser[currency];
            if (val < Constants.INVESTMENT_MIN_AMOUNT)
                break;

            if (currency == "Cash")
                lb.AppendLine($"{start + processedUsers}: **{guildUser.Sanitize()}**: {val:C2}");
            else
                lb.AppendLine($"{start + processedUsers}: **{guildUser.Sanitize()}**: {val:0.####} ({cryptoValue * val:C2})");

            processedUsers++;
        }

        EmbedBuilder embedBuilder = embed.ToEmbedBuilder()
            .WithDescription(lb.Length > 0 ? lb.ToString() : "Nothing to see here!");
        ComponentBuilder componentBuilder = new ComponentBuilder()
            .WithButton("Back", $"lbnext-{executorId}-{currency}-{start-10}-{end-10}-0-True", disabled: end <= 10)
            .WithButton("Next", $"lbnext-{executorId}-{currency}-{end+1}-{end+10}-{failedUsers}-False", disabled: processedUsers != 10 || users.Documents.Count < end + 1);
        await Context.Interaction.UpdateAsync(resp => {
            resp.Embed = embedBuilder.Build();
            resp.Components = componentBuilder.Build();
        });
    }
}
