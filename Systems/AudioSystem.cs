#pragma warning disable RCS1163, IDE0060 // both warnings fire for events, which they shouldn't

namespace RRBot.Systems;
public sealed class AudioSystem
{
    private readonly DiscordSocketClient client;
    private readonly LavaNode lavaNode;

    public AudioSystem(DiscordSocketClient client, LavaNode lavaNode)
    {
        this.client = client;
        this.lavaNode = lavaNode;
    }

    public async Task<RuntimeResult> GetCurrentlyPlayingAsync(SocketCommandContext context)
    {
        if (!lavaNode.TryGetPlayer(context.Guild, out LavaPlayer player))
            return CommandResult.FromError("There is no currently playing track.");

        LavaTrack track = player.Track;
        StringBuilder builder = new($"By: {track.Author}\n");
        if (!track.IsStream)
        {
            TimeSpan pos = new(track.Position.Hours, track.Position.Minutes, track.Position.Seconds);
            builder.AppendLine($"Duration: {track.Duration}\nPosition: {pos}");
        }

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle(track.Title)
            .WithDescription(builder.ToString());
        await context.Channel.SendMessageAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> PlayAsync(SocketCommandContext context, string query)
    {
        SocketGuildUser user = context.User as SocketGuildUser;
        if (user.VoiceChannel is null)
            return CommandResult.FromError("You must be in a voice channel.");

        if (!lavaNode.HasPlayer(context.Guild))
            await lavaNode.JoinAsync(user.VoiceChannel, context.Channel as ITextChannel);

        SearchResponse search = await lavaNode.SearchYouTubeAsync(query);
        if (search.Status == SearchStatus.NoMatches || search.Status == SearchStatus.LoadFailed)
            return CommandResult.FromError("No results were found for your query.");
        LavaTrack track = search.Tracks.FirstOrDefault();

        if (!track.IsStream && track.Duration.TotalSeconds > 7200)
            return CommandResult.FromError("This is too long for me to play! It must be 2 hours or shorter in length.");

        LavaPlayer player = lavaNode.GetPlayer(context.Guild);
        if (player.Track != null && player.PlayerState == PlayerState.Playing)
        {
            await context.Channel.SendMessageAsync($"**{track.Title}** has been added to the queue.");
            player.Queue.Enqueue(track);
            return CommandResult.FromSuccess();
        }

        await player.PlayAsync(track);

        StringBuilder message = new($"Now playing: \"{track.Title}\"\nBy: {track.Author}\n");
        if (!track.IsStream)
            message.AppendLine($"Length: {track.Duration}");
        message.AppendLine("*Tip: if the track instantly doesn't play, it's probably age restricted.*");

        await context.Channel.SendMessageAsync(message.ToString());
        await LoggingSystem.Custom_TrackStarted(user, track.Url);
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> ListAsync(SocketCommandContext context)
    {
        if (!lavaNode.TryGetPlayer(context.Guild, out LavaPlayer player) || player.Track is null)
            return CommandResult.FromError("There are no tracks to list.");

        if (player.Queue.Count < 1)
        {
            await context.Channel.SendMessageAsync($"Now playing: \"{player.Track.Title}\". Nothing else is queued.");
            return CommandResult.FromSuccess();
        }

        StringBuilder playlist = new($"**1**: \"{player.Track.Title}\" by {player.Track.Author} {(!player.Track.IsStream ? $"({player.Track.Duration})\n" : "\n")}");
        for (int i = 0; i < player.Queue.Count; i++)
        {
            LavaTrack track = player.Queue.ElementAt(i);
            playlist.AppendLine($"**{i + 2}**: \"{track.Title}\" by {track.Author} {(!track.IsStream ? $"({track.Duration})" : "")}");
        }

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Playlist")
            .WithDescription(playlist.ToString());
        await context.Channel.SendMessageAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> SkipTrackAsync(SocketCommandContext context)
    {
        if (!lavaNode.TryGetPlayer(context.Guild, out LavaPlayer player) || player.Track is null)
            return CommandResult.FromError("There are no tracks to skip.");

        await context.Channel.SendMessageAsync($"Skipped \"{player.Track.Title}\".");
        await player.StopAsync();
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> StopAsync(SocketCommandContext context)
    {
        if (!lavaNode.TryGetPlayer(context.Guild, out LavaPlayer player))
            return CommandResult.FromError("The bot is not currently being used.");

        await lavaNode.LeaveAsync(player.VoiceChannel);
        await context.Channel.SendMessageAsync("Stopped playing the current track and removed any existing tracks in the queue.");
        return CommandResult.FromSuccess();
    }

    public async Task<RuntimeResult> ChangeVolumeAsync(SocketCommandContext context, ushort volume)
    {
        if (volume < Constants.MIN_VOLUME || volume > Constants.MAX_VOLUME)
            return CommandResult.FromError($"Volume must be between {Constants.MIN_VOLUME}% and {Constants.MAX_VOLUME}%.");
        if (!lavaNode.TryGetPlayer(context.Guild, out LavaPlayer player))
            return CommandResult.FromError("The bot is not currently being used.");

        await player.UpdateVolumeAsync(volume);
        await context.Channel.SendMessageAsync($"Set volume to {volume}%.");
        return CommandResult.FromSuccess();
    }

    // this is a fix for the player breaking if the bot is manually disconnected
    public async Task LeaveOnDisconnect(SocketUser user, SocketVoiceState voiceStateOrig, SocketVoiceState voiceState)
    {
        if (user.Id != client.CurrentUser.Id || voiceState.VoiceChannel != null)
            return;

        await lavaNode.LeaveAsync(voiceStateOrig.VoiceChannel ?? voiceState.VoiceChannel);
    }

    public async Task OnTrackEnded(TrackEndedEventArgs args)
    {
        if (args.Player.Queue?.TryDequeue(out LavaTrack item) == true)
            await args.Player.PlayAsync(item);
        else
            await lavaNode.LeaveAsync(args.Player.VoiceChannel);
    }
}