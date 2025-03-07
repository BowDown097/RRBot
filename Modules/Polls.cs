﻿namespace RRBot.Modules;
[Summary("Polling and elections.")]
public partial class Polls : ModuleBase<SocketCommandContext>
{
    [Command("createpoll")]
    [Summary("Create a poll.")]
    [Remarks("$createpoll \"Is John gay?\" yes|yes|yes|yes|yes|yes|for sure|100%|confident")]
    [RequireStaffLevel(1)]
    [DoNotSanitize]
    public async Task<RuntimeResult> CreatePoll(string title, [Remainder] string choices)
    {
        string[] pollChoices = choices.Split('|');
        if (pollChoices.Length > 9)
            return CommandResult.FromError("A maximum of 9 choices are allowed.");
        
        DbConfigChannels channels = await MongoManager.FetchConfigAsync<DbConfigChannels>(Context.Guild.Id);
        if (Context.Guild.TextChannels.All(channel => channel.Id != channels.PollsChannel))
            return CommandResult.FromError("This server's polls channel has yet to be set or no longer exists.");

        StringBuilder choicesStr = new();
        for (int i = 1; i <= pollChoices.Length; i++)
            choicesStr.AppendLine($"**[{i}]** {pollChoices[i - 1]}");

        EmbedBuilder pollEmbed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle(title)
            .WithDescription(choicesStr.ToString());
        
        SocketTextChannel pollsChannel = Context.Guild.GetTextChannel(channels.PollsChannel);
        RestUserMessage pollMsg = await pollsChannel.SendMessageAsync(embed: pollEmbed.Build());
        for (int i = 1; i <= pollChoices.Length; i++)
            await pollMsg.AddReactionAsync(new Emoji(Constants.PollEmotes[i]));

        await Context.User.NotifyAsync(Context.Channel, $"Created a poll in {pollsChannel.Mention}.");
        return CommandResult.FromSuccess();
    }

    [Command("endelection")]
    [Summary("Preemptively end an ongoing election.")]
    [Remarks("$endelection 1")]
    [RequireStaffLevel(2)]
    public async Task<RuntimeResult> EndElection(int electionId)
    {
        DbElection election = await MongoManager.FetchElectionAsync(Context.Guild.Id, electionId, false);
        if (election is null)
            return CommandResult.FromError("There is no election with that ID!");
        
        DbConfigChannels channels = await MongoManager.FetchConfigAsync<DbConfigChannels>(Context.Guild.Id);
        if (Context.Guild.TextChannels.All(channel => channel.Id != channels.ElectionsAnnounceChannel))
            return CommandResult.FromError("This server's election announcement channel has yet to be set or no longer exists.");
        if (Context.Guild.TextChannels.All(channel => channel.Id != channels.ElectionsVotingChannel))
            return CommandResult.FromError("This server's election voting channel has yet to be set or no longer exists.");

        await ConcludeElection(election, channels, Context.Guild);
        await Context.User.NotifyAsync(Context.Channel, "Election ended.");
        await MongoManager.UpdateObjectAsync(election);
        return CommandResult.FromSuccess();
    }

    [Command("startelection")]
    [Summary("Start an election.")]
    [Remarks("$startelection John \"Obesity Contest\" 72 3")]
    [RequireStaffLevel(2)]
    public async Task<RuntimeResult> StartElection(IGuildUser firstCandidate, string role, long hours = Constants.ElectionDuration / 3600, int numWinners = 1)
    {
        DbConfigChannels channels = await MongoManager.FetchConfigAsync<DbConfigChannels>(Context.Guild.Id);
        if (Context.Guild.TextChannels.All(channel => channel.Id != channels.ElectionsAnnounceChannel))
            return CommandResult.FromError("This server's election announcement channel has yet to be set or no longer exists.");
        if (Context.Guild.TextChannels.All(channel => channel.Id != channels.ElectionsVotingChannel))
            return CommandResult.FromError("This server's election voting channel has yet to be set or no longer exists.");
        
        DbElection election = await MongoManager.FetchElectionAsync(Context.Guild.Id);
        election.Candidates = new Dictionary<ulong, int> { { firstCandidate.Id, 0 } };
        election.EndTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds((long)TimeSpan.FromHours(hours).TotalSeconds);
        election.NumWinners = numWinners;

        SocketTextChannel announcementsChannel = Context.Guild.GetTextChannel(channels.ElectionsAnnounceChannel);
        SocketTextChannel votingChannel = Context.Guild.GetTextChannel(channels.ElectionsVotingChannel);

        EmbedBuilder announcementEmbed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle($"{role} Election")
            .WithDescription($"**1**: {firstCandidate.Sanitize()} - 0 votes\n\n*Vote for members with $vote in {votingChannel.Mention}.*")
            .WithFooter($"# Winners • {election.NumWinners} • ID • {election.ElectionId} • Ends at")
            .WithTimestamp(DateTimeOffset.FromUnixTimeSeconds(election.EndTime));
        RestUserMessage announcementMessage = await announcementsChannel.SendMessageAsync(embed: announcementEmbed.Build());
        election.AnnouncementMessage = announcementMessage.Id;

        OverwritePermissions perms = votingChannel.GetPermissionOverwrite(Context.Guild.EveryoneRole) ?? OverwritePermissions.InheritAll;
        await votingChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, perms.Modify(sendMessages: PermValue.Allow));
        await Context.User.NotifyAsync(Context.Channel, "Election started.");

        await MongoManager.UpdateObjectAsync(election);
        return CommandResult.FromSuccess();
    }

    [Command("vote")]
    [Summary("Vote in an election.")]
    [Remarks("$vote 2 *Jazzy Hands*")]
    public async Task<RuntimeResult> Vote(int electionId, [Remainder] IGuildUser user)
    {
        if (user.IsBot)
            return CommandResult.FromError("Nope.");
        if (user.Id == Context.User.Id)
            return CommandResult.FromError("You can't vote for yourself!");
        
        DbElection election = await MongoManager.FetchElectionAsync(Context.Guild.Id, electionId, false);
        if (election is null)
            return CommandResult.FromError("There is no election with that ID!");
        
        DbConfigChannels channels = await MongoManager.FetchConfigAsync<DbConfigChannels>(Context.Guild.Id);
        int ageDays = (DateTimeOffset.UtcNow - (Context.User as IGuildUser)?.JoinedAt).GetValueOrDefault().Days;
        if (ageDays < channels.MinimumVotingAgeDays)
            return CommandResult.FromError($"You need to be in the server for at least {channels.MinimumVotingAgeDays} days to vote.");
        if (Context.Guild.TextChannels.All(channel => channel.Id != channels.ElectionsAnnounceChannel))
            return CommandResult.FromError("This server's election announcement channel has yet to be set or no longer exists.");
        if (Context.Channel.Id != channels.ElectionsVotingChannel)
            return CommandResult.FromError($"You must vote in {MentionUtils.MentionChannel(channels.ElectionsVotingChannel)}.");

        if (election.Voters.TryGetValue(Context.User.Id, out List<ulong>? votes))
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

        if (!election.Candidates.TryAdd(user.Id, 1))
            election.Candidates[user.Id]++;

        if (!election.Voters.TryGetValue(Context.User.Id, out List<ulong>? value))
            election.Voters.Add(Context.User.Id, [user.Id]);
        else
            value.Add(user.Id);

        await UpdateElection(election, channels, Context.Guild);
        await Context.User.NotifyAsync(Context.Channel, $"Voted for {user.Sanitize()}.");
        await MongoManager.UpdateObjectAsync(election);
        return CommandResult.FromSuccess();
    }
}