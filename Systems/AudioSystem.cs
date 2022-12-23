﻿namespace RRBot.Systems;
public sealed class AudioSystem
{
    private readonly IAudioService _audioService;

    public AudioSystem(IAudioService audioService) => _audioService = audioService;

    public async Task<RuntimeResult> ChangeVolumeAsync(SocketCommandContext context, float volume)
    {
        if (volume is < Constants.MinVolume or > Constants.MaxVolume)
            return CommandResult.FromError($"Volume must be between {Constants.MinVolume}% and {Constants.MaxVolume}%.");
        if (!_audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = _audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        await player.SetVolumeAsync(volume / 100f, true);
        await context.Channel.SendMessageAsync($"Set volume to {volume}%.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> DequeueAllWithNameAsync(SocketCommandContext context, string name)
    {
        if (!_audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = _audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        int count = player.Queue.RemoveAll(t => (t.Context as TrackMetadata)?.Title.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
        if (count == 0)
            return CommandResult.FromError("There are no tracks in the queue with that name.");

        await context.User.NotifyAsync(context.Channel, $"Removed all **{count}** tracks with that title.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> GetCurrentlyPlayingAsync(SocketCommandContext context)
    {
        if (!_audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = _audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        if (player.CurrentTrack is null)
            return CommandResult.FromError("There is no track currently playing.");

        TrackMetadata metadata = player.CurrentTrack.Context as TrackMetadata;
        StringBuilder builder = new($"By: {metadata.Author}\n");
        if (!player.CurrentTrack.IsLiveStream)
            builder.AppendLine($"Duration: {player.CurrentTrack.Duration.Round()}\nPosition: {player.Position.Position.Round()}");

        using ArtworkService artworkService = new();
        Uri artwork = metadata.Artwork ?? await artworkService.ResolveAsync(player.CurrentTrack);

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle(metadata.Title)
            .WithThumbnailUrl(artwork?.ToString())
            .WithDescription(builder.ToString());
        await context.Channel.SendMessageAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> ListAsync(SocketCommandContext context)
    {
        if (!_audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = _audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        TrackMetadata currMetadata = player.CurrentTrack.Context as TrackMetadata;

        if (player.Queue.IsEmpty)
        {
            await context.Channel.SendMessageAsync($"Now playing: \"{currMetadata.Title}\". Nothing else is queued.", allowedMentions: Constants.Mentions);
            return CommandResult.FromSuccess();
        }

        LavalinkTrack[] tracks = player.Queue.Prepend(player.CurrentTrack).ToArray();
        TimeSpan totalLength = TimeSpan.Zero;
        StringBuilder playlist = new();
        for (int i = 0; i < tracks.Length; i++)
        {
            LavalinkTrack track = tracks[i];
            TrackMetadata metadata = track.Context as TrackMetadata;
            playlist.Append($"**{i+1}**: [\"{metadata.Title}\" by {metadata.Author}]({track.Uri})");
            if (!track.IsLiveStream)
            {
                playlist.Append($" ({track.Duration.Round()})");
                totalLength += track.Duration.Round();
            }
            playlist.AppendLine($" | Added by: {metadata.Requester}");
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
        if (!_audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = _audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        player.LoopMode = player.LoopMode == PlayerLoopMode.Track ? PlayerLoopMode.None : PlayerLoopMode.Track;
        await context.Channel.SendMessageAsync($"Looping turned {(player.LoopMode == PlayerLoopMode.Track ? "ON" : "OFF")}.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> PlayAsync(SocketCommandContext context, string query)
    {
        Attachment attachment = context.Message.Attachments.FirstOrDefault();
        if (attachment?.ContentType?.StartsWith("video/") == true || attachment?.ContentType?.StartsWith("audio/") == true)
            query = attachment.Url;

        if (string.IsNullOrWhiteSpace(query))
            return CommandResult.FromError("You must provide a search query or media attachment.");

        SocketGuildUser user = context.User as SocketGuildUser;
        if (user.VoiceChannel is null)
            return CommandResult.FromError("You must be in a voice channel.");

        VoteLavalinkPlayer player = _audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild.Id)
            ?? await _audioService.JoinAsync<VoteLavalinkPlayer>(context.Guild.Id, user.VoiceChannel.Id, true);

        LavalinkTrack track;
        if (Uri.TryCreate(query, UriKind.Absolute, out Uri uri))
        {
            SearchMode searchMode = uri.Host.Replace("www.", "") switch
            {
                "soundcloud.com" or "snd.sc" => SearchMode.SoundCloud,
                "youtube.com" or "youtu.be" => SearchMode.YouTube,
                _ => SearchMode.None
            };

            track = searchMode switch
            {
                SearchMode.None when !uri.ToString().Split('/').Last().Contains('.') => await _audioService
                    .YtdlpGetTrackAsync(uri, context.User),
                SearchMode.YouTube when uri.AbsolutePath == "/watch" => await _audioService.GetYtTrackAsync(uri,
                    context.Guild, context.User),
                _ => await _audioService.RrGetTrackAsync(query, context.User, searchMode)
            };
        }
        else
        {
            track = await _audioService.RrGetTrackAsync(query, context.User, SearchMode.YouTube);
        }
        
        if (track is null)
            return CommandResult.FromError("No results were found. Either your search query didn't return anything or your URL is unsupported.");
        if (track.Identifier == "restricted")
            return CommandResult.FromError("A result was found, but is age restricted. Age restricted content can be enabled if an admin runs $togglensfw.");
        if ((context.User as IGuildUser)?.GuildPermissions.Has(GuildPermission.Administrator) == false && !track.IsLiveStream && track.Duration.TotalSeconds > 7200)
            return CommandResult.FromError("This is too long for me to play! It must be 2 hours or shorter in length.");

        TrackMetadata metadata = track.Context as TrackMetadata;
        if (await FilterSystem.ContainsFilteredWord(context.Guild, metadata.Title))
            return CommandResult.FromError("Nope.");
        int position = await player.PlayAsync(track, enqueue: true);

        if (position == 0)
        {
            StringBuilder message = new($"Now playing: \"{metadata.Title}\"\nBy: {metadata.Author}\n");
            if (!track.IsLiveStream)
                message.AppendLine($"Length: {track.Duration.Round()}");
            await context.Channel.SendMessageAsync(message.ToString(), allowedMentions: Constants.Mentions);
        }
        else
        {
            await context.Channel.SendMessageAsync($"**{metadata.Title}** has been added to the queue.", allowedMentions: Constants.Mentions);
        }

        await LoggingSystem.Custom_TrackStarted(user, track.Uri.ToString());
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> SeekAsync(SocketCommandContext context, string pos)
    {
        if (!TimeSpan.TryParse(pos, out TimeSpan ts))
            return CommandResult.FromError("Not a valid seek position!\nExample valid seek position: 00:13:08");
        if (!_audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = _audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        if (ts < TimeSpan.Zero || ts > player.CurrentTrack.Duration)
            return CommandResult.FromError($"You can't seek to a negative position or a position longer than the track duration ({player.CurrentTrack.Duration.Round()}).");

        await player.SeekPositionAsync(ts);
        await context.Channel.SendMessageAsync($"Seeked to **{ts.Round()}**.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> ShuffleAsync(SocketCommandContext context)
    {
        if (!_audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = _audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        if (player.Queue.Count <= 1)
            return CommandResult.FromError("There must be at least 2 tracks in the queue to shuffle.");

        player.Queue.Shuffle();
        await context.Channel.SendMessageAsync("Shuffled the queue.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> SkipTrackAsync(SocketCommandContext context)
    {
        if (!_audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = _audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        TrackMetadata metadata = player.CurrentTrack.Context as TrackMetadata;
        await context.Channel.SendMessageAsync($"Skipped \"{metadata.Title}\".", allowedMentions: Constants.Mentions);
        if (!player.Queue.TryDequeue(out LavalinkTrack track))
        {
            await player.StopAsync(true);
        }
        else
        {
            await player.SkipAsync();
            await player.PlayAsync(track);
        }

        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> StopAsync(SocketCommandContext context)
    {
        if (!_audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = _audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        await player.StopAsync(true);
        await context.Channel.SendMessageAsync("Stopped playing the current track and removed any existing tracks in the queue.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> VoteSkipTrackAsync(SocketCommandContext context)
    {
        if (!_audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = _audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        TrackMetadata metadata = player.CurrentTrack.Context as TrackMetadata;
        UserVoteSkipInfo info = await player.VoteAsync(context.User.Id);
        if (!info.WasAdded)
            return CommandResult.FromError("You already voted to skip!");

        int votesNeeded = (int)Math.Ceiling((double)info.TotalUsers / 2) - info.Votes.Count;
        if (votesNeeded > 0)
        {
            await context.Channel.SendMessageAsync($"Vote received! **{votesNeeded}** more votes are needed.");
        }
        else
        {
            await context.Channel.SendMessageAsync($"Skipped \"{metadata.Title}\".", allowedMentions: Constants.Mentions);
        }

        return CommandResult.FromSuccess();
    }
}
