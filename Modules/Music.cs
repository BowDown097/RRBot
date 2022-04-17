namespace RRBot.Modules;
[Summary("Jam out with the hombres!")]
public class Music : ModuleBase<SocketCommandContext>
{
    public AudioSystem AudioSystem { get; set; }

    [Alias("fs")]
    [Command("forceskip")]
    [Summary("Skip the current playing track, ignoring the voting process.")]
    [RequireDJ]
    public async Task<RuntimeResult> ForceSkip() => await AudioSystem.SkipTrackAsync(Context);

    [Command("loop")]
    [Summary("Toggle looping.")]
    [RequireDJ]
    public async Task<RuntimeResult> Loop() => await AudioSystem.LoopAsync(Context);

    [Command("lyrics")]
    [Summary("View the lyrics of the current playing track, if any.")]
    public async Task<RuntimeResult> Lyrics() => await AudioSystem.GetLyricsAsync(Context);

    [Alias("np", "playing")]
    [Command("nowplaying")]
    [Summary("Get details on the currently playing track, if there is one.")]
    public async Task<RuntimeResult> NowPlaying() => await AudioSystem.GetCurrentlyPlayingAsync(Context);

    [Command("play")]
    [Summary("Play something from YouTube or SoundCloud.")]
    [Remarks("$play ram ranch 200")]
    public async Task<RuntimeResult> Play([Remainder] string url) => await AudioSystem.PlayAsync(Context, url);

    [Alias("list")]
    [Command("queue")]
    [Summary("List tracks in the queue.")]
    public async Task<RuntimeResult> Queue() => await AudioSystem.ListAsync(Context);

    [Command("seek")]
    [Summary("Seek to a position in the currently playing track.")]
    [Remarks("$seek 34:32")]
    [RequireDJ]
    public async Task<RuntimeResult> Seek(string pos) => await AudioSystem.SeekAsync(Context, pos);

    [Command("shuffle")]
    [Summary("Shuffle the queue.")]
    [RequireDJ]
    public async Task<RuntimeResult> Shuffle() => await AudioSystem.ShuffleAsync(Context);

    [Alias("voteskip", "vs")]
    [Command("skip")]
    [Summary("Vote to skip the currently playing track.")]
    public async Task<RuntimeResult> Skip() => await AudioSystem.VoteSkipTrackAsync(Context);

    [Command("stop")]
    [Summary("Stop playing entirely.")]
    [RequireDJ]
    public async Task<RuntimeResult> Stop() => await AudioSystem.StopAsync(Context);

    [Command("volume")]
    [Summary("Change the volume of the currently playing track (must be between 5% and 200%).")]
    [Remarks("$volume 200")]
    [RequireDJ]
    public async Task<RuntimeResult> Volume(float volume) => await AudioSystem.ChangeVolumeAsync(Context, volume);
}