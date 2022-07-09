namespace RRBot.Modules;
[Summary("Polling and elections.")]
public class Polls : ModuleBase<SocketCommandContext>
{
    #region Commands
    [Command("createpoll")]
    [Summary("Create a poll.")]
    [Remarks("$createpoll \"Is John gay?\" yes|yes|yes|yes|yes|yes|for sure|100%|confident")]
    [RequireStaff]
    public async Task<RuntimeResult> CreatePoll(string title, [Remainder] string choices)
    {
        DbConfigChannels channels = await DbConfigChannels.GetById(Context.Guild.Id);
        if (!Context.Guild.TextChannels.Any(channel => channel.Id == channels.PollsChannel))
            return CommandResult.FromError("This server's polls channel has yet to be set or no longer exists.");

        SocketTextChannel pollsChannel = Context.Guild.GetTextChannel(channels.PollsChannel);

        string[] pollChoices = choices.Split('|');
        if (pollChoices.Length > 9)
            return CommandResult.FromError("A maximum of 9 choices are allowed.");

        StringBuilder choicesStr = new();
        for (int i = 1; i <= pollChoices.Length; i++)
            choicesStr.AppendLine($"**[{i}]** {pollChoices[i - 1]}");

        EmbedBuilder pollEmbed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle(title)
            .WithDescription(choicesStr.ToString());

        RestUserMessage pollMsg = await pollsChannel.SendMessageAsync(embed: pollEmbed.Build());
        for (int i = 1; i <= pollChoices.Length; i++)
            await pollMsg.AddReactionAsync(new Emoji(Constants.POLL_EMOTES[i]));

        await Context.User.NotifyAsync(Context.Channel, $"Created a poll [here]({pollMsg.GetJumpUrl()}).");
        return CommandResult.FromSuccess();
    }

    [Command("endelection")]
    [Summary("Preemptively end an ongoing election.")]
    [Remarks("$endelection 1")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task<RuntimeResult> EndElection(int electionId)
    {
        QuerySnapshot elections = await Program.database.Collection($"servers/{Context.Guild.Id}/elections").GetSnapshotAsync();
        if (!MemoryCache.Default.Any(k => k.Key.StartsWith("election") && k.Key.EndsWith(electionId.ToString())) && !elections.Any(r => r.Id == electionId.ToString()))
            return CommandResult.FromError("There is no election with that ID!");

        DbConfigChannels channels = await DbConfigChannels.GetById(Context.Guild.Id);
        if (!Context.Guild.TextChannels.Any(channel => channel.Id == channels.ElectionsAnnounceChannel))
            return CommandResult.FromError("This server's election announcement channel has yet to be set or no longer exists.");
        if (!Context.Guild.TextChannels.Any(channel => channel.Id == channels.ElectionsVotingChannel))
            return CommandResult.FromError("This server's election voting channel has yet to be set or no longer exists.");

        DbElection election = await DbElection.GetById(Context.Guild.Id, electionId);
        await ConcludeElection(election, channels, Context.Guild);
        await Context.User.NotifyAsync(Context.Channel, "Election ended.");
        return CommandResult.FromSuccess();
    }

    [Command("startelection")]
    [Summary("Start an election.")]
    [Remarks("$startelection John \"Obesity Contest\" 72 3")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task<RuntimeResult> StartElection(IGuildUser firstCandidate, string role, long hours = Constants.ELECTION_DURATION / 3600, int numWinners = 1)
    {
        DbConfigChannels channels = await DbConfigChannels.GetById(Context.Guild.Id);
        if (!Context.Guild.TextChannels.Any(channel => channel.Id == channels.ElectionsAnnounceChannel))
            return CommandResult.FromError("This server's election announcement channel has yet to be set or no longer exists.");
        if (!Context.Guild.TextChannels.Any(channel => channel.Id == channels.ElectionsVotingChannel))
            return CommandResult.FromError("This server's election voting channel has yet to be set or no longer exists.");

        DbElection election = await DbElection.GetById(Context.Guild.Id);
        election.Candidates = new() { { firstCandidate.Id.ToString(), 0 } };
        election.EndTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds((long)TimeSpan.FromHours(hours).TotalSeconds);
        election.NumWinners = numWinners;

        SocketTextChannel announcementsChannel = Context.Guild.GetTextChannel(channels.ElectionsAnnounceChannel);
        SocketTextChannel votingChannel = Context.Guild.GetTextChannel(channels.ElectionsVotingChannel);

        EmbedBuilder announcementEmbed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle($"{role} Election")
            .WithDescription($"**1**: {firstCandidate.Sanitize()} - 0 votes\n\n*Vote for members with $vote in {MentionUtils.MentionChannel(votingChannel.Id)}.*")
            .WithFooter($"# Winners • {election.NumWinners} • ID • {election.Reference.Id} • Ends at")
            .WithTimestamp(DateTimeOffset.FromUnixTimeSeconds(election.EndTime));
        RestUserMessage announcementMessage = await announcementsChannel.SendMessageAsync(embed: announcementEmbed.Build());
        election.AnnouncementMessage = announcementMessage.Id;

        OverwritePermissions perms = votingChannel.GetPermissionOverwrite(Context.Guild.EveryoneRole) ?? OverwritePermissions.InheritAll;
        await votingChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, perms.Modify(sendMessages: PermValue.Allow));
        await Context.User.NotifyAsync(Context.Channel, "Election started.");
        return CommandResult.FromSuccess();
    }

    [Command("vote")]
    [Summary("Vote in an election.")]
    [Remarks("$vote 2 *Jazzy Hands*")]
    public async Task<RuntimeResult> Vote(int electionId, IGuildUser user)
    {
        if (user.IsBot || await FilterSystem.ContainsFilteredWord(Context.Guild, user.ToString()))
            return CommandResult.FromError("Nope.");
        if (user.Id == Context.User.Id)
            return CommandResult.FromError("You can't vote for yourself!");

        QuerySnapshot elections = await Program.database.Collection($"servers/{Context.Guild.Id}/elections").GetSnapshotAsync();
        if (!MemoryCache.Default.Any(k => k.Key.StartsWith("election") && k.Key.EndsWith(electionId.ToString())) && !elections.Any(r => r.Id == electionId.ToString()))
            return CommandResult.FromError("There is no election with that ID!");

        DbConfigChannels channels = await DbConfigChannels.GetById(Context.Guild.Id);
        int ageDays = (DateTimeOffset.UtcNow - (Context.User as IGuildUser)?.JoinedAt).GetValueOrDefault().Days;
        if (ageDays < channels.MinimumVotingAgeDays)
            return CommandResult.FromError($"You need to be in the server for at least {channels.MinimumVotingAgeDays} days to vote.");
        if (!Context.Guild.TextChannels.Any(channel => channel.Id == channels.ElectionsAnnounceChannel))
            return CommandResult.FromError("This server's election announcement channel has yet to be set or no longer exists.");
        if (Context.Channel.Id != channels.ElectionsVotingChannel)
            return CommandResult.FromError($"You must vote in {MentionUtils.MentionChannel(channels.ElectionsVotingChannel)}.");

        DbElection election = await DbElection.GetById(Context.Guild.Id, electionId);
        if (election.Voters.TryGetValue(Context.User.Id.ToString(), out List<ulong> votes))
        {
            if (votes.Contains(user.Id))
                return CommandResult.FromError($"You already voted for {user.Sanitize()}!");

            if (votes.Count == election.NumWinners)
            {
                return CommandResult.FromError(election.NumWinners == 1
                    ? "You already voted in this election!"
                    : $"You already voted for the maximum of {election.NumWinners} candidates in this election!");
            }
        }

        if (!election.Candidates.ContainsKey(user.Id.ToString()))
            election.Candidates.Add(user.Id.ToString(), 1);
        else
            election.Candidates[user.Id.ToString()]++;

        if (!election.Voters.ContainsKey(Context.User.Id.ToString()))
            election.Voters.Add(Context.User.Id.ToString(), new() { user.Id });
        else
            election.Voters[Context.User.Id.ToString()].Add(user.Id);

        await UpdateElection(election, channels, Context.Guild);
        await Context.User.NotifyAsync(Context.Channel, $"Voted for {user.Sanitize()}.");
        return CommandResult.FromSuccess();
    }
    #endregion

    #region Helpers
    public static async Task ConcludeElection(DbElection election, DbConfigChannels channels, SocketGuild guild)
    {
        SocketTextChannel announcementsChannel = guild.GetTextChannel(channels.ElectionsAnnounceChannel);
        SocketTextChannel votingChannel = guild.GetTextChannel(channels.ElectionsVotingChannel);
        IUserMessage announcementMessage = await announcementsChannel.GetMessageAsync(election.AnnouncementMessage) as IUserMessage;

        IEnumerable<IGuildUser> winners = election.Candidates
            .OrderByDescending(kvp => kvp.Value)
            .Take(election.NumWinners)
            .Select(kvp => guild.GetUser(Convert.ToUInt64(kvp.Key)));
        string winnerList = string.Join(", ", winners.Take(winners.Count() - 1).Select(u => u.Sanitize())) +
            (winners.Count() > 1 ? " and " : "") + winners.LastOrDefault().Sanitize();

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle(announcementMessage.Embeds.First().Title)
            .WithDescription(winners.Count() > 1
                ? $"Election concluded! **{winnerList}** were the winners!"
                : $"Election concluded! **{winnerList}** was the winner!")
            .WithFooter($"Original ID • {election.Reference.Id} • Ended at")
            .WithCurrentTimestamp();

        await announcementMessage.ModifyAsync(msg => msg.Embed = embed.Build());
        OverwritePermissions perms = votingChannel.GetPermissionOverwrite(guild.EveryoneRole) ?? OverwritePermissions.InheritAll;
        await votingChannel.AddPermissionOverwriteAsync(guild.EveryoneRole, perms.Modify(sendMessages: PermValue.Deny));
        election.EndTime = -1;
    }

    public static async Task UpdateElection(DbElection election, DbConfigChannels channels, SocketGuild guild)
    {
        SocketTextChannel announcementsChannel = guild.GetTextChannel(channels.ElectionsAnnounceChannel);
        IUserMessage announcementMessage = await announcementsChannel.GetMessageAsync(election.AnnouncementMessage) as IUserMessage;

        StringBuilder description = new();
        int processedUsers = 0;
        foreach (KeyValuePair<string, int> kvp in election.Candidates.OrderByDescending(k => k.Value))
        {
            if (processedUsers == 20)
                break;

            IGuildUser user = guild.GetUser(Convert.ToUInt64(kvp.Key));
            if (user == null)
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
            .WithFooter(ogEmbed.Footer.Value.Text)
            .WithTimestamp(ogEmbed.Timestamp.Value);
        await announcementMessage.ModifyAsync(msg => msg.Embed = embed.Build());
    }
    #endregion
}