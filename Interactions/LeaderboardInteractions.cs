namespace RRBot.Interactions;
public static class LeaderboardInteractions
{
    public static async Task GetNext(SocketMessageComponent component, ulong executorId, string currency, int start, int end, int failedUsers, bool back)
    {
        Embed embed = component.Message.Embeds.FirstOrDefault();
        IGuild guild = component.User.GetGuild();

        double cryptoValue = currency != "Cash" ? await Investments.QueryCryptoValue(currency) : 0;
        QuerySnapshot users = await Program.database.Collection($"servers/{guild.Id}/users")
            .OrderByDescending(currency).GetSnapshotAsync();
        StringBuilder lb = new("*Note: The leaderboard updates every 10 minutes, so stuff may not be up to date.*\n");
        int processedUsers = 0;
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
        await component.UpdateAsync(resp => {
            resp.Embed = embedBuilder.Build();
            resp.Components = componentBuilder.Build();
        });
    }
}