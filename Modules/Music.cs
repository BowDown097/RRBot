namespace RRBot.Modules;
[Summary("Jam out with the hombres!")]
public class Music : ModuleBase<SocketCommandContext>
{
    public AudioSystem AudioSystem { get; set; }

    [Alias("fs")]
    [Command("forceskip")]
    [Summary("Skip the current playing track, ignoring the voting process.")]
    [Remarks("$forceskip")]
    [RequireDJ]
    public async Task<RuntimeResult> ForceSkip() => await AudioSystem.SkipTrackAsync(Context);

    [Command("loop")]
    [Summary("Toggle looping.")]
    [Remarks("$loop")]
    [RequireDJ]
    public async Task<RuntimeResult> Loop() => await AudioSystem.LoopAsync(Context);

    [Command("lyrics")]
    [Summary("View the lyrics of the current playing track, if any.")]
    [Remarks("$lyrics")]
    public async Task<RuntimeResult> Lyrics() => await AudioSystem.GetLyricsAsync(Context);

    [Alias("np", "playing")]
    [Command("nowplaying")]
    [Summary("Gives details on the currently playing track, if there is one.")]
    [Remarks("$nowplaying")]
    public async Task<RuntimeResult> NowPlaying() => await AudioSystem.GetCurrentlyPlayingAsync(Context);

    [Command("play")]
    [Summary("Plays something from YouTube.")]
    [Remarks("$play [url]")]
    public async Task<RuntimeResult> Play([Remainder] string url) => await AudioSystem.PlayAsync(Context, url);

    [Alias("list")]
    [Command("queue")]
    [Summary("List tracks in the queue.")]
    [Remarks("$queue")]
    public async Task<RuntimeResult> Queue() => await AudioSystem.ListAsync(Context);

    [Command("shuffle")]
    [Summary("Shuffle the queue.")]
    [Remarks("$shuffle")]
    [RequireDJ]
    public async Task<RuntimeResult> Shuffle() => await AudioSystem.ShuffleAsync(Context);

    [Alias("voteskip", "vs")]
    [Command("skip")]
    [Summary("Vote to skip the currently playing track.")]
    [Remarks("$skip")]
    public async Task<RuntimeResult> Skip() => await AudioSystem.VoteSkipTrackAsync(Context);

    [Command("stop")]
    [Summary("Stops playing entirely.")]
    [Remarks("$stop")]
    [RequireDJ]
    public async Task<RuntimeResult> Stop() => await AudioSystem.StopAsync(Context);

    [Command("volume")]
    [Summary("Changes the volume of the currently playing track (must be between 5% and 200%).")]
    [Remarks("$volume [volume]")]
    [RequireDJ]
    public async Task<RuntimeResult> Volume(float volume) => await AudioSystem.ChangeVolumeAsync(Context, volume);
}