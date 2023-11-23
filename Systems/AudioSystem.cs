namespace RRBot.Systems;
public sealed class AudioSystem
{
    private readonly IAudioService _audioService;
    private static readonly IOptions<VoteLavalinkPlayerOptions> PlayerOptions = Options.Create(new VoteLavalinkPlayerOptions());
    public AudioSystem(IAudioService audioService) => _audioService = audioService;

    private async ValueTask<PlayerResult<VoteLavalinkPlayer>> GetPlayerAsync(
        SocketCommandContext context,
        PlayerChannelBehavior channelBehavior = PlayerChannelBehavior.None,
        ImmutableArray<IPlayerPrecondition> preconditions = default)
    {
        SocketGuildUser guildUser = context.User as SocketGuildUser;
        PlayerRetrieveOptions retrieveOptions = new(
            ChannelBehavior: channelBehavior,
            Preconditions: preconditions,
            VoiceStateBehavior: MemberVoiceStateBehavior.RequireSame
        );

        return await _audioService.Players.RetrieveAsync(
            context.Guild.Id,
            guildUser.VoiceChannel.Id,
            PlayerFactory.Vote,
            PlayerOptions,
            retrieveOptions
        );
    }

    public async Task<RuntimeResult> ChangeVolumeAsync(SocketCommandContext context, float volume)
    {
        if (volume is < Constants.MinVolume or > Constants.MaxVolume)
            return CommandResult.FromError($"Volume must be between {Constants.MinVolume}% and {Constants.MaxVolume}%.");

        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(
            context, PlayerChannelBehavior.None, ImmutableArray.Create(PlayerPrecondition.Playing));
        if (!playerResult.IsSuccess)
            return CommandResult.FromError(playerResult.ErrorMessage());

        await playerResult.Player.SetVolumeAsync(volume);
        await context.Channel.SendMessageAsync($"Set volume to {volume}%.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> DequeueAllWithNameAsync(SocketCommandContext context, string name)
    {
        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(context);
        if (!playerResult.IsSuccess)
            return CommandResult.FromError(playerResult.ErrorMessage());

        int count = await playerResult.Player.Queue.RemoveAllAsync(t =>
            t.As<RrTrack>().Title.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (count == 0)
            return CommandResult.FromError("There are no tracks in the queue with that name.");
        
        await context.User.NotifyAsync(context.Channel, $"Removed all **{count}** tracks with that title.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> DequeueAtAsync(SocketCommandContext context, int index)
    {
        switch (index)
        {
            case <= 0:
                return CommandResult.FromError("Invalid index.");
            case 1:
                await SkipTrackAsync(context);
                return CommandResult.FromSuccess();
        }

        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(context);
        if (!playerResult.IsSuccess)
            return CommandResult.FromError(playerResult.ErrorMessage());
        if (index - 2 > playerResult.Player.Queue.Count)
            return CommandResult.FromError("There are less tracks in the queue than your index.");

        RrTrack track = playerResult.Player.Queue.ElementAt(index - 2).As<RrTrack>();
        await playerResult.Player.Queue.RemoveAsync(track!);

        await context.User.NotifyAsync(context.Channel,
            $"Successfully removed the track at that index (\"{track.Title}\").");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> GetCurrentlyPlayingAsync(SocketCommandContext context)
    {
        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(
            context, PlayerChannelBehavior.None, ImmutableArray.Create(PlayerPrecondition.Playing));
        if (!playerResult.IsSuccess)
            return CommandResult.FromError(playerResult.ErrorMessage());

        RrTrack track = playerResult.Player.CurrentItem.As<RrTrack>();
        StringBuilder builder = new($"By: {track.Author}\n");
        if (!track.Track.IsLiveStream)
            builder.AppendLine($"Duration: {track.Track.Duration.Round()}\nPosition: {playerResult.Player.Position.GetValueOrDefault().Position.Round()}");

        using ArtworkService artworkService = new();
        Uri artworkUri = track.ArtworkUri ?? await artworkService.ResolveAsync(track.Track);

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle(track.Title)
            .WithThumbnailUrl(artworkUri.ToString())
            .WithDescription(builder.ToString());

        await context.Channel.SendMessageAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> ListAsync(SocketCommandContext context)
    {
        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(
            context, PlayerChannelBehavior.None, ImmutableArray.Create(PlayerPrecondition.Playing));
        if (!playerResult.IsSuccess)
            return CommandResult.FromError(playerResult.ErrorMessage());

        RrTrack currentTrack = playerResult.Player.CurrentItem.As<RrTrack>();
        if (playerResult.Player.Queue.IsEmpty)
        {
            await context.Channel.SendMessageAsync($"Now playing: \"{currentTrack.Title}\". Nothing else is queued.", allowedMentions: Constants.Mentions);
            return CommandResult.FromSuccess();
        }
        
        ITrackQueueItem[] tracks = playerResult.Player.Queue.Prepend(currentTrack).ToArray();
        TimeSpan totalLength = TimeSpan.Zero;
        StringBuilder playlist = new();

        for (int i = 0; i < tracks.Length; i++)
        {
            RrTrack track = tracks[i].As<RrTrack>();
            playlist.Append($"**{i + 1}**: [\"{track.Title}\" by {track.Author}]({track.Track.Uri})");

            if (!track.Track.IsLiveStream)
            {
                TimeSpan duration = track.Track.Duration.Round();
                playlist.Append($" ({duration})");
                totalLength += duration.Round();
            }
            
            playlist.AppendLine($" | Added by: {track.Requester}");
        }
        
        playlist.AppendLine($"\n**Total Length: {totalLength}**");
        
        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Playlist")
            .WithDescription(playlist.ToString());

        await context.Channel.SendMessageAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> LoopAsync(SocketCommandContext context)
    {
        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(
            context, PlayerChannelBehavior.None, ImmutableArray.Create(PlayerPrecondition.Playing));
        if (!playerResult.IsSuccess)
            return CommandResult.FromError(playerResult.ErrorMessage());

        playerResult.Player.RepeatMode = playerResult.Player.RepeatMode == TrackRepeatMode.Track
            ? TrackRepeatMode.None : TrackRepeatMode.Track;

        string loopStatus = playerResult.Player.RepeatMode == TrackRepeatMode.Track ? "ON" : "OFF";
        await context.Channel.SendMessageAsync($"Looping turned {loopStatus}.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> PlayAsync(SocketCommandContext context, string query)
    {
        Attachment attachment = context.Message.Attachments.FirstOrDefault();
        if (attachment?.ContentType?.StartsWith("video/") == true || attachment?.ContentType?.StartsWith("audio/") == true)
            query = attachment.Url;

        if (string.IsNullOrWhiteSpace(query))
            return CommandResult.FromError("You must provide a search query or media attachment.");
        
        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(context, PlayerChannelBehavior.Join);
        if (!playerResult.IsSuccess)
            return CommandResult.FromError(playerResult.ErrorMessage());
        
        RrTrack track = null;
        if (Uri.TryCreate(query, UriKind.Absolute, out Uri uri))
        {
            TrackSearchMode searchMode = uri.Host.Replace("www.", "") switch
            {
                "music.youtube.com" => TrackSearchMode.YouTubeMusic,
                "soundcloud.com" or "snd.sc" => TrackSearchMode.SoundCloud,
                "youtube.com" or "youtu.be" => TrackSearchMode.YouTube,
                _ => TrackSearchMode.None
            };
            
            if (searchMode == TrackSearchMode.None && !uri.Segments.LastOrDefault().Contains('.'))
                await _audioService.YtDlpGetTrackAsync(uri, context.User);
            else if (searchMode == TrackSearchMode.YouTube)
                await _audioService.GetYtTrackAsync(uri, context.Guild, context.User);
            else
                await _audioService.RrGetTrackAsync(query, context.User, searchMode);
        }
        else
        {
            track = await _audioService.RrGetTrackAsync(query, context.User, TrackSearchMode.YouTube);
        }
        
        if (track is null)
            return CommandResult.FromError("No results were found. Either your search query didn't return anything or your URL is unsupported.");
        if (track.Track.Identifier == "restricted")
            return CommandResult.FromError("A result was found, but is age restricted. Age restricted content can be enabled if an admin runs $togglensfw.");
        if ((context.User as IGuildUser)?.GuildPermissions.Has(GuildPermission.Administrator) == false && !track.Track.IsLiveStream && track.Track.Duration.TotalSeconds > 7200)
            return CommandResult.FromError("This is too long for me to play! It must be 2 hours or shorter in length.");

        int position = await playerResult.Player.PlayAsync(track);

        if (position == 0)
        {
            StringBuilder message = new($"Now playing: \"{track.Title}\"\nBy: {track.Author}\n");
            if (!track.Track.IsLiveStream)
                message.AppendLine($"Length: {track.Track.Duration.Round()}");
            await context.Channel.SendMessageAsync(message.ToString(), allowedMentions: Constants.Mentions);
        }
        else
        {
            await context.Channel.SendMessageAsync($"**{track.Title}** has been added to the queue.", allowedMentions: Constants.Mentions);
        }

        await LoggingSystem.Custom_TrackStarted(context.User as SocketGuildUser, track.Track.Uri.ToString());
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> SeekAsync(SocketCommandContext context, string pos)
    {
        if (!TimeSpan.TryParseExact(pos, new[] { "%s", @"m\:s", @"h\:m\:s" }, null, out TimeSpan ts))
            return CommandResult.FromError("Not a valid seek position!\nExample valid seek position: 13:08");

        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(
            context, PlayerChannelBehavior.None, ImmutableArray.Create(PlayerPrecondition.Playing));
        if (!playerResult.IsSuccess)
            return CommandResult.FromError(playerResult.ErrorMessage());
        if (ts < TimeSpan.Zero || ts > playerResult.Player.CurrentTrack.Duration)
            return CommandResult.FromError($"You can't seek to a negative position or a position longer than the track duration ({playerResult.Player.CurrentTrack.Duration.Round()}).");

        await playerResult.Player.SeekAsync(ts);
        await context.Channel.SendMessageAsync($"Seeked to **{ts.Round()}**.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> ShuffleAsync(SocketCommandContext context)
    {
        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(context);
        if (!playerResult.IsSuccess)
            return CommandResult.FromError(playerResult.ErrorMessage());
        if (playerResult.Player.Queue.Count <= 1)
            return CommandResult.FromError("There must be at least 2 tracks in the queue to shuffle.");

        playerResult.Player.Shuffle = !playerResult.Player.Shuffle;
        string shuffleStatus = playerResult.Player.Shuffle ? "Shuffled" : "Unshuffled";

        await context.Channel.SendMessageAsync($"{shuffleStatus} the queue.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> SkipTrackAsync(SocketCommandContext context)
    {
        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(
            context, PlayerChannelBehavior.None, ImmutableArray.Create(PlayerPrecondition.Playing));
        if (!playerResult.IsSuccess)
            return CommandResult.FromError(playerResult.ErrorMessage());

        RrTrack skippedTrack = playerResult.Player.CurrentItem.As<RrTrack>();
        await playerResult.Player.SkipAsync();
        await context.Channel.SendMessageAsync($"Skipped \"{skippedTrack.Title}\".", allowedMentions: Constants.Mentions);

        if (playerResult.Player.CurrentTrack is null)
            await playerResult.Player.StopAsync();

        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> StopAsync(SocketCommandContext context)
    {
        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(
            context, PlayerChannelBehavior.None, ImmutableArray.Create(PlayerPrecondition.Playing));
        if (!playerResult.IsSuccess)
            return CommandResult.FromError(playerResult.ErrorMessage());

        await playerResult.Player.StopAsync();
        await context.Channel.SendMessageAsync("Stopped playing the current track and removed any existing tracks in the queue.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> VoteSkipTrackAsync(SocketCommandContext context)
    {
        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(
            context, PlayerChannelBehavior.None, ImmutableArray.Create(PlayerPrecondition.Playing));
        if (!playerResult.IsSuccess)
            return CommandResult.FromError(playerResult.ErrorMessage());

        RrTrack track = playerResult.Player.CurrentItem.As<RrTrack>();
        UserVoteResult vote = await playerResult.Player.VoteAsync(context.User.Id, default);

        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (vote)
        {
            case UserVoteResult.AlreadySubmitted:
                return CommandResult.FromError("You already voted to skip!");
            case UserVoteResult.Skipped:
                await context.Channel.SendMessageAsync($"Skipped \"{track.Title}\".", allowedMentions: Constants.Mentions);
                break;
            case UserVoteResult.Submitted:
            {
                VoteSkipInformation skipInfo = await playerResult.Player.GetVotesAsync();
                int votesNeeded = (int)Math.Ceiling(skipInfo.TotalUsers * PlayerOptions.Value.SkipThreshold) - skipInfo.Votes.Length;
                await context.Channel.SendMessageAsync($"Vote received! **{votesNeeded}** more votes are needed.");
                break;
            }
        }

        return CommandResult.FromSuccess();
    }
}
