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
        decimal cryptoValue = currency != "Cash" ? await Investments.QueryCryptoValue(currency) : 0;

        SortDefinition<DbUser> sort = Builders<DbUser>.Sort.Descending(currency);
        IAsyncCursor<DbUser> cursor = await MongoManager.Users.FindAsync(u => u.GuildId == Context.Guild.Id,
            new FindOptions<DbUser> { Sort = sort });
        List<DbUser> users = await cursor.ToListAsync();

        StringBuilder lb = new("*Note: The leaderboard updates every 10 minutes, so stuff may not be up to date.*\n");
        int processedUsers = 0;
        foreach (DbUser user in users)
        {
            if (processedUsers == 10)
                break;

            IGuildUser guildUser = Context.Guild.GetUser(user.UserId);
            if (guildUser == null || user.Perks.ContainsKey("Pacifist")) 
            {
                if (!back) failedUsers++;
                continue;
            }

            decimal val = (decimal)user[currency];
            if (val < Constants.InvestmentMinAmount)
                break;

            lb.AppendLine(currency == "Cash"
                ? $"{start + processedUsers}: **{guildUser.Sanitize()}**: {val:C2}"
                : $"{start + processedUsers}: **{guildUser.Sanitize()}**: {val:0.####} ({cryptoValue * val:C2})");

            processedUsers++;
        }

        EmbedBuilder embedBuilder = embed.ToEmbedBuilder()
            .WithDescription(lb.Length > 0 ? lb.ToString() : "Nothing to see here!");
        ComponentBuilder componentBuilder = new ComponentBuilder()
            .WithButton("Back", $"lbnext-{executorId}-{currency}-{start-10}-{end-10}-0-True", disabled: end <= 10)
            .WithButton("Next", $"lbnext-{executorId}-{currency}-{end+1}-{end+10}-{failedUsers}-False", 
                disabled: processedUsers != 10 || users.Count < end + 1);
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
        
        SortDefinition<DbGang> sort = Builders<DbGang>.Sort.Descending(g => g.VaultBalance);
        IAsyncCursor<DbGang> cursor = await MongoManager.Gangs.FindAsync(u => u.GuildId == Context.Guild.Id,
            new FindOptions<DbGang> { Sort = sort });
        List<DbGang> gangs = await cursor.ToListAsync();

        StringBuilder lb = new("*Note: The leaderboard updates every 10 minutes, so stuff may not be up to date.*\n");
        int processedGangs = 0;
        foreach (DbGang gang in gangs)
        {
            if (processedGangs == 10 || gang.VaultBalance < Constants.InvestmentMinAmount)
                break;
            lb.AppendLine($"{processedGangs + 1}: **{Format.Sanitize(gang.Name).Replace("\\:", ":").Replace("\\/", "/").Replace("\\.", ".")}**: {gang.VaultBalance:C2}");
            processedGangs++;
        }

        EmbedBuilder embedBuilder = embed.ToEmbedBuilder()
            .WithDescription(lb.Length > 0 ? lb.ToString() : "Nothing to see here!");
        ComponentBuilder componentBuilder = new ComponentBuilder()
            .WithButton("Back", $"ganglbnext-{executorId}-{start-10}-{end-10}", disabled: end <= 10)
            .WithButton("Next", $"ganglbnext-{executorId}-{end+1}-{end+10}",
                disabled: processedGangs != 10 || gangs.Count < end + 1);
        await Context.Interaction.UpdateAsync(resp => {
            resp.Embed = embedBuilder.Build();
            resp.Components = componentBuilder.Build();
        });
    }
}