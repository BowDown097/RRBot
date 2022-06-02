namespace RRBot.Systems;
public sealed class AudioSystem
{
    private readonly IAudioService audioService;

    public AudioSystem(IAudioService audioService)
    {
        this.audioService = audioService;
    }

    public async Task<RuntimeResult> ChangeVolumeAsync(SocketCommandContext context, float volume)
    {
        if (volume < Constants.MIN_VOLUME || volume > Constants.MAX_VOLUME)
            return CommandResult.FromError($"Volume must be between {Constants.MIN_VOLUME}% and {Constants.MAX_VOLUME}%.");
        if (!audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        await player.SetVolumeAsync(volume / 100f, true);
        await context.Channel.SendMessageAsync($"Set volume to {volume}%.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> GetCurrentlyPlayingAsync(SocketCommandContext context)
    {
        if (!audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
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

    public async Task<RuntimeResult> GetLyricsAsync(SocketCommandContext context)
    {
        if (!audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        TrackMetadata metadata = player.CurrentTrack.Context as TrackMetadata;

        using LyricsService lyricsService = new(new LyricsOptions());
        string lyrics = await lyricsService.RequestLyricsAsync(TrackDecoder.DecodeTrackInfo(player.CurrentTrack.Identifier));
        if (string.IsNullOrWhiteSpace(lyrics))
            return CommandResult.FromError("No lyrics found!");

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle($"{metadata.Title} Lyrics")
            .WithDescription(Format.Sanitize(lyrics));
        await context.Channel.SendMessageAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> ListAsync(SocketCommandContext context)
    {
        if (!audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        TrackMetadata currMetadata = player.CurrentTrack.Context as TrackMetadata;

        if (player.Queue.IsEmpty)
        {
            await context.Channel.SendMessageAsync($"Now playing: \"{currMetadata.Title}\". Nothing else is queued.", allowedMentions: Constants.MENTIONS);
            return CommandResult.FromSuccess();
        }

        StringBuilder playlist = new($"**1**: \"{currMetadata.Title}\" by {currMetadata.Author} {(!player.CurrentTrack.IsLiveStream ? $"({player.CurrentTrack.Duration.Round()})\n" : "\n")}");
        for (int i = 0; i < player.Queue.Count; i++)
        {
            LavalinkTrack track = player.Queue[i];
            TrackMetadata metadata = track.Context as TrackMetadata;
            playlist.AppendLine($"**{i + 2}**: \"{metadata.Title}\" by {metadata.Author} {(!track.IsLiveStream ? $"({track.Duration.Round()})" : "")}");
        }

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Playlist")
            .WithDescription(playlist.ToString());
        await context.Channel.SendMessageAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> LoopAsync(SocketCommandContext context)
    {
        if (!audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        player.IsLooping = !player.IsLooping;
        await context.Channel.SendMessageAsync($"Looping turned {(player.IsLooping ? "ON" : "OFF")}.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> PlayAsync(SocketCommandContext context, string query)
    {
        query = query.Replace("\\", "");
        SocketGuildUser user = context.User as SocketGuildUser;
        if (user.VoiceChannel is null)
            return CommandResult.FromError("You must be in a voice channel.");

        VoteLavalinkPlayer player = audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild.Id)
            ?? await audioService.JoinAsync<VoteLavalinkPlayer>(context.Guild.Id, user.VoiceChannel.Id, true);

        LavalinkTrack track = null;
        if (Uri.TryCreate(query, UriKind.Absolute, out Uri uri))
        {
            SearchMode searchMode = uri.Host.Replace("www.", "") switch
            {
                "soundcloud.com" or "snd.sc" => SearchMode.SoundCloud,
                "youtube.com" or "youtu.be" => SearchMode.YouTube,
                _ => SearchMode.None
            };

            if (searchMode == SearchMode.None && !uri.ToString().Split('/').Last().Contains('.'))
            {
                using Process ytdlpProc = new();
                ytdlpProc.StartInfo.FileName = new FileInfo("yt-dlp").GetFullPath();
                ytdlpProc.StartInfo.Arguments = $"-xj --no-warnings {uri}";
                ytdlpProc.StartInfo.CreateNoWindow = true;
                ytdlpProc.StartInfo.RedirectStandardOutput = true;
                ytdlpProc.StartInfo.UseShellExecute = false;
                ytdlpProc.Start();

                string output = await ytdlpProc.StandardOutput.ReadToEndAsync();
                await ytdlpProc.WaitForExitAsync();

                JObject obj = JObject.Parse(output);
                track = await audioService.GetTrackAsync(obj["url"].ToString());
                if (track != null)
                {
                    track.Context = new TrackMetadata(
                        obj["thumbnail"]?.ToString(),
                        obj["uploader"]?.ToString() ?? obj["channel"]?.ToString(),
                        obj["title"]?.ToString());
                }
            }
            else
            {
                track = await audioService.GetTrackAsync(query, searchMode);
                if (track != null)
                    track.Context = new TrackMetadata(track);
            }
        }
        else
        {
            track = await audioService.GetTrackAsync(query, SearchMode.YouTube);
            if (track != null)
                track.Context = new TrackMetadata(track);
        }

        if (track is null)
            return CommandResult.FromError("No results were found. Either your search query didn't return anything or your URL is unsupported.");
        if ((context.User as IGuildUser)?.GuildPermissions.Has(GuildPermission.Administrator) == false && !track.IsLiveStream && track.Duration.TotalSeconds > 7200)
            return CommandResult.FromError("This is too long for me to play! It must be 2 hours or shorter in length.");

        int position = await player.PlayAsync(track, enqueue: true);
        TrackMetadata metadata = track.Context as TrackMetadata;
        if (position == 0)
        {
            StringBuilder message = new($"Now playing: \"{metadata.Title}\"\nBy: {metadata.Author}\n");
            if (!track.IsLiveStream)
                message.AppendLine($"Length: {track.Duration.Round()}");
            message.AppendLine("*Tip: if the track instantly doesn't play, it's probably age restricted.*");
            await context.Channel.SendMessageAsync(message.ToString(), allowedMentions: Constants.MENTIONS);
        }
        else
        {
            await context.Channel.SendMessageAsync($"**{metadata.Title}** has been added to the queue.", allowedMentions: Constants.MENTIONS);
        }

        await LoggingSystem.Custom_TrackStarted(user, track.Source);
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> SeekAsync(SocketCommandContext context, string pos)
    {
        if (!TimeSpan.TryParse(pos, out TimeSpan ts))
            return CommandResult.FromError("Not a valid seek position!\nExample valid seek position: 00:13:08");
        if (!audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        if (ts < TimeSpan.Zero || ts > player.CurrentTrack.Duration)
            return CommandResult.FromError($"You can't seek to a negative position or a position longer than the track duration ({player.CurrentTrack.Duration.Round()}).");

        await player.SeekPositionAsync(ts);
        await context.Channel.SendMessageAsync($"Seeked to **{ts.Round()}**.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> ShuffleAsync(SocketCommandContext context)
    {
        if (!audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        if (player.Queue.Count <= 1)
            return CommandResult.FromError("There must be at least 2 tracks in the queue to shuffle.");

        player.Queue.Shuffle();
        await context.Channel.SendMessageAsync("Shuffled the queue.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> SkipTrackAsync(SocketCommandContext context)
    {
        if (!audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        TrackMetadata metadata = player.CurrentTrack.Context as TrackMetadata;
        await context.Channel.SendMessageAsync($"Skipped \"{metadata.Title}\".", allowedMentions: Constants.MENTIONS);
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
        if (!audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
        await player.StopAsync(true);
        await context.Channel.SendMessageAsync("Stopped playing the current track and removed any existing tracks in the queue.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> VoteSkipTrackAsync(SocketCommandContext context)
    {
        if (!audioService.HasPlayer(context.Guild))
            return CommandResult.FromError("The bot is not currently being used.");

        VoteLavalinkPlayer player = audioService.GetPlayer<VoteLavalinkPlayer>(context.Guild);
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
            await context.Channel.SendMessageAsync($"Skipped \"{metadata.Title}\".", allowedMentions: Constants.MENTIONS);
        }

        return CommandResult.FromSuccess();
    }
}