namespace RRBot.Modules;
public partial class Polls
{
    public static async Task ConcludeElection(DbElection election, DbConfigChannels channels, SocketGuild guild)
    {
        SocketTextChannel announcementsChannel = guild.GetTextChannel(channels.ElectionsAnnounceChannel);
        SocketTextChannel votingChannel = guild.GetTextChannel(channels.ElectionsVotingChannel);
        if (await announcementsChannel.GetMessageAsync(election.AnnouncementMessage) is not IUserMessage announcementMessage)
            return;

        List<string> winners = new(election.NumWinners);
        foreach ((ulong id, _) in election.Candidates.OrderByDescending(kvp => kvp.Value))
        {
            if (winners.Count == winners.Capacity)
                break;
            SocketGuildUser user = guild.GetUser(id);
            if (user is not null)
                winners.Add(user.Sanitize());
        }

        string winnerList = string.Join(", ", winners.Take(winners.Count - 1)) +
            (winners.Count > 1 ? " and " : "") + winners.LastOrDefault();

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle(announcementMessage.Embeds.First().Title)
            .WithDescription(winners.Count > 1
                ? $"Election concluded! **{winnerList}** were the winners!"
                : $"Election concluded! **{winnerList}** was the winner!")
            .WithFooter($"Original ID • {election.ElectionId} • Ended at")
            .WithCurrentTimestamp();

        await announcementMessage.ModifyAsync(msg => msg.Embed = embed.Build());
        OverwritePermissions perms = votingChannel.GetPermissionOverwrite(guild.EveryoneRole) ?? OverwritePermissions.InheritAll;
        await votingChannel.AddPermissionOverwriteAsync(guild.EveryoneRole, perms.Modify(sendMessages: PermValue.Deny));
        election.EndTime = -1;
    }

    public static async Task UpdateElection(DbElection election, DbConfigChannels channels, SocketGuild guild)
    {
        SocketTextChannel announcementsChannel = guild.GetTextChannel(channels.ElectionsAnnounceChannel);
        if (await announcementsChannel.GetMessageAsync(election.AnnouncementMessage) is not IUserMessage announcementMessage)
            return;

        StringBuilder description = new();
        int processedUsers = 0;
        foreach (KeyValuePair<ulong, int> kvp in election.Candidates.OrderByDescending(k => k.Value))
        {
            if (processedUsers == 20)
                break;

            IGuildUser user = guild.GetUser(Convert.ToUInt64(kvp.Key));
            if (user is null)
                continue;

            description.AppendLine($"**{processedUsers + 1}**: {user.Sanitize()} - {kvp.Value} votes");
            processedUsers++;
        }

        description.AppendLine($"\n*Vote for members with $vote in {MentionUtils.MentionChannel(channels.ElectionsVotingChannel)}.*");
        IEmbed ogEmbed = announcementMessage.Embeds.First();
        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle(ogEmbed.Title)
            .WithDescription(description.ToString())
            .WithFooter(ogEmbed.Footer.GetValueOrDefault().Text)
            .WithTimestamp(ogEmbed.Timestamp.GetValueOrDefault());
        await announcementMessage.ModifyAsync(msg => msg.Embed = embed.Build());
    }
}