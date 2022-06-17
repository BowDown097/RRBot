using Discord.Interactions;

namespace RRBot.Interactions;
public class Leaderboard : InteractionModuleBase<SocketInteractionContext<SocketMessageComponent>>
{
    [ComponentInteraction("lbnext-*-*-*-*-*-*")]
    public async Task GetNext(ulong executorId, string currency, int start, int end, int failedUsers, bool back)
    {
        if (Context.Interaction.User.Id != executorId)
        {
            await Context.Interaction.RespondAsync("Action not permitted: You did not execute the original command.", ephemeral: true);
            return;
        }

        Embed embed = Context.Interaction.Message.Embeds.FirstOrDefault();
        double cryptoValue = currency != "Cash" ? await Investments.QueryCryptoValue(currency) : 0;
        QuerySnapshot users = await Program.database.Collection($"servers/{Context.Guild.Id}/users")
            .OrderByDescending(currency).GetSnapshotAsync();
        StringBuilder lb = new("*Note: The leaderboard updates every 10 minutes, so stuff may not be up to date.*\n");
        int processedUsers = 0;
        foreach (DocumentSnapshot doc in users.Documents.Skip(start - 1 + failedUsers))
        {
            if (processedUsers == 10)
                break;

            IGuildUser guildUser = Context.Guild.GetUser(Convert.ToUInt64(doc.Id));
            if (guildUser == null)
            {
                if (!back) failedUsers++;
                continue;
            }

            DbUser dbUser = await DbUser.GetById(Context.Guild.Id, guildUser.Id, false);
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

    [ComponentInteraction("ganglbnext-*-*-*")]
    public async Task GetNextGangs(ulong executorId, int start, int end)
    {
        if (Context.Interaction.User.Id != executorId)
        {
            await Context.Interaction.RespondAsync("Action not permitted: You did not execute the original command.", ephemeral: true);
            return;
        }

        Embed embed = Context.Interaction.Message.Embeds.FirstOrDefault();
        QuerySnapshot gangs = await Program.database.Collection($"servers/{Context.Guild.Id}/gangs")
            .OrderByDescending("VaultBalance").GetSnapshotAsync();
        StringBuilder lb = new("*Note: The leaderboard updates every 10 minutes, so stuff may not be up to date.*\n");
        int processedGangs = 0;
        foreach (DocumentSnapshot doc in gangs.Documents)
        {
            if (processedGangs == 10)
                break;

            DbGang gang = await DbGang.GetByName(Context.Guild.Id, doc.Id, false);
            if (gang.VaultBalance < Constants.INVESTMENT_MIN_AMOUNT)
                break;

            lb.AppendLine($"{processedGangs + 1}: **{Format.Sanitize(doc.Id)}**: {gang.VaultBalance:C2}");
            processedGangs++;
        }

        EmbedBuilder embedBuilder = embed.ToEmbedBuilder()
            .WithDescription(lb.Length > 0 ? lb.ToString() : "Nothing to see here!");
        ComponentBuilder componentBuilder = new ComponentBuilder()
            .WithButton("Back", $"ganglbnext-{executorId}-{start-10}-{end-10}", disabled: end <= 10)
            .WithButton("Next", $"ganglbnext-{executorId}-{end+1}-{end+10}", disabled: processedGangs != 10 || gangs.Documents.Count < end + 1);
        await Context.Interaction.UpdateAsync(resp => {
            resp.Embed = embedBuilder.Build();
            resp.Components = componentBuilder.Build();
        });
    }
}