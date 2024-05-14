
namespace RRBot.Systems;
public sealed class AudioSystem(IAudioService audioService, ILyricsService lyricsService)
{
    private static readonly IOptions<VoteLavalinkPlayerOptions> PlayerOptions = Options.Create(new VoteLavalinkPlayerOptions());

    private async ValueTask<PlayerResult<VoteLavalinkPlayer>> GetPlayerAsync(
        SocketCommandContext context,
        PlayerChannelBehavior channelBehavior = PlayerChannelBehavior.None,
        ImmutableArray<IPlayerPrecondition> preconditions = default)
    {
        PlayerRetrieveOptions retrieveOptions = new(
            ChannelBehavior: channelBehavior,
            Preconditions: preconditions,
            VoiceStateBehavior: MemberVoiceStateBehavior.RequireSame
        );

        return await audioService.Players.RetrieveAsync(
            context.Guild.Id,
            (context.User as SocketGuildUser)?.VoiceChannel?.Id,
            PlayerFactory.Vote,
            PlayerOptions,
            retrieveOptions
        );
    }

    private async ValueTask<PlayerResult<VoteLavalinkPlayer>> GetPlayerAsync(
        SocketCommandContext context,
        IPlayerPrecondition precondition,
        PlayerChannelBehavior channelBehavior = PlayerChannelBehavior.None)
        => await GetPlayerAsync(context, channelBehavior, [precondition]);
    
    public async Task<RuntimeResult> ChangePitchAsync(SocketCommandContext context, float pitch)
    {
        if (pitch is < Constants.MinPitch or > Constants.MaxPitch)
            return CommandResult.FromError($"Pitch must be between {Constants.MinPitchString}% and {Constants.MaxPitchString}%.");

        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(context, PlayerPrecondition.Playing);
        if (!playerResult.IsSuccess)
            return CommandResult.FromError(playerResult.ErrorMessage());
        
        playerResult.Player.Filters.Timescale = new TimescaleFilterOptions
        {
            Pitch = pitch / 100f,
            Speed = playerResult.Player.Filters.Timescale?.Speed
        };

        await playerResult.Player.Filters.CommitAsync();
        await context.Channel.SendMessageAsync($"Set pitch to {pitch}%.");
        return CommandResult.FromSuccess(); 
    }
    
    public async Task<RuntimeResult> ChangeSpeedAsync(SocketCommandContext context, float speed)
    {
        if (speed is < Constants.MinSpeed or > Constants.MaxSpeed)
            return CommandResult.FromError($"Speed must be between {Constants.MinSpeedString}% and {Constants.MaxSpeedString}%.");

        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(context, PlayerPrecondition.Playing);
        if (!playerResult.IsSuccess)
            return CommandResult.FromError(playerResult.ErrorMessage());

        playerResult.Player.Filters.Timescale = new TimescaleFilterOptions
        {
            Pitch = speed / 100f,
            Speed = speed / 100f
        };

        await playerResult.Player.Filters.CommitAsync();
        await context.Channel.SendMessageAsync($"Set speed to {speed}%.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> ChangeTempoAsync(SocketCommandContext context, float tempo)
    {
        if (tempo is < Constants.MinTempo or > Constants.MaxTempo)
            return CommandResult.FromError($"Tempo must be between {Constants.MinTempoString}% and {Constants.MaxTempoString}%.");

        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(context, PlayerPrecondition.Playing);
        if (!playerResult.IsSuccess)
            return CommandResult.FromError(playerResult.ErrorMessage());

        playerResult.Player.Filters.Timescale = new TimescaleFilterOptions
        {
            Pitch = playerResult.Player.Filters.Timescale?.Pitch,
            Speed = tempo / 100f
        };
        
        await playerResult.Player.Filters.CommitAsync();
        await context.Channel.SendMessageAsync($"Set tempo to {tempo}%.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> ChangeVolumeAsync(SocketCommandContext context, float volume)
    {
        if (volume is < Constants.MinVolume or > Constants.MaxVolume)
            return CommandResult.FromError($"Volume must be between {Constants.MinVolumeString}% and {Constants.MaxVolumeString}%.");

        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(context, PlayerPrecondition.Playing);
        if (!playerResult.IsSuccess)
            return CommandResult.FromError(playerResult.ErrorMessage());

        await playerResult.Player.SetVolumeAsync(volume / 100f);
        await context.Channel.SendMessageAsync($"Set volume to {volume}%.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> DequeueAllWithNameAsync(SocketCommandContext context, string name)
    {
        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(context);
        if (!playerResult.IsSuccess)
            return CommandResult.FromError(playerResult.ErrorMessage());

        int count = await playerResult.Player.Queue.RemoveAllAsync(t => t.As<RrTrack>().Title.Equals(name, StringComparison.OrdinalIgnoreCase));
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
        if (playerResult.Player.Queue.ElementAtOrDefault(index - 2) is not RrTrack track)
            return CommandResult.FromError("There is no track at that index.");

        await playerResult.Player.Queue.RemoveAsync(track!);
        await context.Channel.SendMessageAsync($"Successfully removed the track at that index (\"{track.Title}\").", allowedMentions: AllowedMentions.None);
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> GetCurrentlyPlayingAsync(SocketCommandContext context)
    {
        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(context, PlayerPrecondition.Playing);
        if (!playerResult.IsSuccess || playerResult.Player.CurrentItem is not RrTrack track)
            return CommandResult.FromError(playerResult.ErrorMessage());

        StringBuilder builder = new($"By: {track.Author}\n");
        if (!track.Track.IsLiveStream)
            builder.AppendLine(
                $"Duration: {track.Track.Duration.Condense()}\nPosition: {playerResult.Player.Position.GetValueOrDefault().Position.Condense()}");

        using ArtworkService artworkService = new();
        Uri artworkUri = track.ArtworkUri ?? await artworkService.ResolveAsync(track.Track);

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle(track.Title)
            .WithDescription(builder.ToString());

        if (artworkUri is not null)
            embed.WithThumbnailUrl(artworkUri.ToString());

        await context.Channel.SendMessageAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> ListAsync(SocketCommandContext context)
    {
        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(context, PlayerPrecondition.Playing);
        if (!playerResult.IsSuccess || playerResult.Player.CurrentItem is not RrTrack currentTrack)
            return CommandResult.FromError(playerResult.ErrorMessage());

        if (playerResult.Player.Queue.IsEmpty)
        {
            await context.Channel.SendMessageAsync($"Now playing: \"{currentTrack.Title}\". Nothing else is queued.", allowedMentions: AllowedMentions.None);
            return CommandResult.FromSuccess();
        }

        StringBuilder playlist = new();
        ITrackQueueItem[] tracks = playerResult.Player.Queue.Prepend(currentTrack).ToArray();
        TimeSpan totalLength = TimeSpan.Zero;

        for (int i = 0; i < tracks.Length; i++)
        {
            RrTrack track = tracks[i].As<RrTrack>();
            playlist.Append($"**{i + 1}**: [\"{track.Title}\" by {track.Author}]({track.Track.Uri})");

            if (!track.Track.IsLiveStream)
            {
                playlist.Append($" ({track.Track.Duration.Condense()})");
                totalLength += track.Track.Duration;
            }
            
            playlist.AppendLine($" | Added by: {track.Requester}");
        }
        
        playlist.AppendLine($"\n**Total Length: {totalLength.Condense()}**");

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Playlist")
            .WithElidedDescription(playlist.ToString());

        await context.Channel.SendMessageAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> LoopAsync(SocketCommandContext context)
    {
        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(context, PlayerPrecondition.Playing);
        if (!playerResult.IsSuccess)
            return CommandResult.FromError(playerResult.ErrorMessage());

        playerResult.Player.RepeatMode = playerResult.Player.RepeatMode == TrackRepeatMode.Track
            ? TrackRepeatMode.None : TrackRepeatMode.Track;

        string loopStatus = playerResult.Player.RepeatMode == TrackRepeatMode.Track ? "ON" : "OFF";
        await context.Channel.SendMessageAsync($"Looping turned {loopStatus}.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> LyricsAsync(SocketCommandContext context)
    {
        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(context, PlayerPrecondition.Playing);
        if (!playerResult.IsSuccess || playerResult.Player.CurrentItem is not RrTrack track)
            return CommandResult.FromError(playerResult.ErrorMessage());

        string lyrics = await lyricsService.GetLyricsAsync(track.Track!);
        if (string.IsNullOrEmpty(lyrics))
            return CommandResult.FromError("No lyrics were found.");

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle($"\"{track.Title}\" by {track.Author}")
            .WithDescription(StringCleaner.Sanitize(lyrics));

        await context.Channel.SendMessageAsync(embed: embed.Build());
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

        RrTrack track;
        if (Uri.TryCreate(query, UriKind.Absolute, out Uri uri))
        {
            TrackSearchMode searchMode = uri.Host.Replace("www.", "") switch
            {
                "music.youtube.com" => TrackSearchMode.YouTubeMusic,
                "soundcloud.com" or "snd.sc" => TrackSearchMode.SoundCloud,
                "youtube.com" or "youtu.be" => TrackSearchMode.YouTube,
                _ => TrackSearchMode.None
            };

            if (searchMode == TrackSearchMode.None)
            {
                string lastSegment = uri.Segments.LastOrDefault();
                track = lastSegment.Contains('.') // check for direct link, use yt-dlp if not
                    ? await audioService.RrGetTrackAsync(query, context.User, searchMode, lastSegment)
                    : await audioService.YtDlpGetTrackAsync(uri, context.User);
            }
            else if (searchMode == TrackSearchMode.YouTube)
            {
                track = await audioService.GetYtTrackAsync(uri, context.Guild, context.User);
            }
            else
            {
                track = await audioService.RrGetTrackAsync(query, context.User, searchMode);
            }
        }
        else
        {
            track = await audioService.SearchYtTrackAsync(query, context.Guild, context.User);
        }
        
        if (track is null)
            return CommandResult.FromError("No results were found. Either your search query didn't return anything or your URL is unsupported.");
        if (track.Track.Identifier == "restricted")
            return CommandResult.FromError("A result was found, but is age restricted. Age restricted content can be enabled if an admin runs $togglensfw.");
        if ((context.User as IGuildUser)?.GuildPermissions.Has(GuildPermission.Administrator) == false && track.Track is { IsLiveStream: false, Duration.TotalSeconds: > 7200 })
            return CommandResult.FromError("This is too long for me to play! It must be 2 hours or shorter in length.");

        int position = await playerResult.Player.PlayAsync(track);

        if (position == 0)
        {
            StringBuilder message = new($"Now playing: \"{track.Title}\"\nBy: {track.Author}\n");
            if (!track.Track.IsLiveStream)
                message.AppendLine($"Length: {track.Track.Duration.Condense()}");
            await context.Channel.SendMessageAsync(message.ToString(), allowedMentions: AllowedMentions.None);
        }
        else
        {
            await context.Channel.SendMessageAsync($"**{track.Title}** has been added to the queue.", allowedMentions: AllowedMentions.None);
        }

        await LoggingSystem.Custom_TrackStarted(context.User as SocketGuildUser, track.Track.Uri.ToString());
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> SeekAsync(SocketCommandContext context, string pos)
    {
        if (!TimeSpan.TryParseExact(pos, ["%s", @"m\:s", @"h\:m\:s"], null, out TimeSpan ts))
            return CommandResult.FromError("Not a valid seek position!\nExample valid seek position: 13:08");

        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(context, PlayerPrecondition.Playing);
        if (!playerResult.IsSuccess)
            return CommandResult.FromError(playerResult.ErrorMessage());
        if (ts < TimeSpan.Zero || ts > playerResult.Player.CurrentTrack.Duration)
            return CommandResult.FromError($"You can't seek to a negative position or a position longer than the track duration ({playerResult.Player.CurrentTrack.Duration.Condense()}).");

        await playerResult.Player.SeekAsync(ts);
        await context.Channel.SendMessageAsync($"Seeked to **{ts.Condense()}**.");
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
        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(context, PlayerPrecondition.Playing);
        if (!playerResult.IsSuccess || playerResult.Player.CurrentItem is not RrTrack track)
            return CommandResult.FromError(playerResult.ErrorMessage());

        await playerResult.Player.SkipAsync();
        await context.Channel.SendMessageAsync($"Skipped \"{track.Title}\".", allowedMentions: AllowedMentions.None);
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> StopAsync(SocketCommandContext context)
    {
        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(context, PlayerPrecondition.Playing);
        if (!playerResult.IsSuccess)
            return CommandResult.FromError(playerResult.ErrorMessage());

        await playerResult.Player.StopAsync();
        await context.Channel.SendMessageAsync("Stopped playing the current track and removed any existing tracks in the queue.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> VoteSkipTrackAsync(SocketCommandContext context)
    {
        PlayerResult<VoteLavalinkPlayer> playerResult = await GetPlayerAsync(context, PlayerPrecondition.Playing);
        if (!playerResult.IsSuccess || playerResult.Player.CurrentItem is not RrTrack track)
            return CommandResult.FromError(playerResult.ErrorMessage());

        UserVoteResult vote = await playerResult.Player.VoteAsync(context.User.Id, default);
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
        switch (vote)
        {
            case UserVoteResult.AlreadySubmitted:
                return CommandResult.FromError("You already voted to skip!");
            case UserVoteResult.Skipped:
                await context.Channel.SendMessageAsync($"Skipped \"{track.Title}\".", allowedMentions: AllowedMentions.None);
                break;
            case UserVoteResult.Submitted:
            {
                VoteSkipInformation skipInfo = await playerResult.Player.GetVotesAsync();
                int votesNeeded = (int)Math.Ceiling(skipInfo.TotalUsers * PlayerOptions.Value.SkipThreshold) - skipInfo.Votes.Length;
                await context.Channel.SendMessageAsync($"Vote received! **{votesNeeded}** more vote(s) are needed.");
                break;
            }
        }

        return CommandResult.FromSuccess();
    }
}
