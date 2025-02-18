namespace RRBot.Modules;
[Summary("Jam out with the hombres!")]
public class Music : ModuleBase<SocketCommandContext>
{
    public AudioSystem AudioSystem { get; set; } = null!;

    [Command("dequeue", RunMode = RunMode.Async)]
    [Summary("Dequeue all tracks with a specific name.")]
    [Remarks("$dequeue kiminosei")]
    [RequireDj]
    [DoNotSanitize]
    public async Task<RuntimeResult> Dequeue([Remainder] string name) => await AudioSystem.DequeueAllWithNameAsync(Context, name);

    [Command("dequeueat", RunMode = RunMode.Async)]
    [Summary("Dequeue a track at a specific index in the queue (excluding the current track).")]
    [Remarks("$dequeueat 5")]
    [RequireDj]
    public async Task<RuntimeResult> DequeueAt(int index) => await AudioSystem.DequeueAtAsync(Context, index);

    [Alias("fs")]
    [Command("forceskip", RunMode = RunMode.Async)]
    [Summary("Skip the current playing track, ignoring the voting process.")]
    [RequireDj]
    public async Task<RuntimeResult> ForceSkip() => await AudioSystem.SkipTrackAsync(Context);

    [Command("loop", RunMode = RunMode.Async)]
    [Summary("Toggle looping.")]
    [RequireDj]
    public async Task<RuntimeResult> Loop() => await AudioSystem.LoopAsync(Context);

    [Command("lyrics", RunMode = RunMode.Async)]
    [Summary("Get the lyrics of the currently playing track.")]
    [RequireDj]
    public async Task<RuntimeResult> Lyrics() => await AudioSystem.LyricsAsync(Context);

    [Alias("np", "playing")]
    [Command("nowplaying", RunMode = RunMode.Async)]
    [Summary("Get details on the currently playing track, if there is one.")]
    public async Task<RuntimeResult> NowPlaying() => await AudioSystem.GetCurrentlyPlayingAsync(Context);
    
    [Command("pitch", RunMode = RunMode.Async)]
    [Summary($"Change the pitch of the currently playing track (must be between ${Constants.MinPitchString}% and ${Constants.MaxPitchString}%).")]
    [Remarks("$pitch 200")]
    [RequireDj]
    public async Task<RuntimeResult> Pitch(float pitch) => await AudioSystem.ChangePitchAsync(Context, pitch);

    [Command("play", RunMode = RunMode.Async)]
    [Summary("Play something from YouTube or SoundCloud.")]
    [Remarks("$play ram ranch 200")]
    [DoNotSanitize]
    public async Task<RuntimeResult> Play([Remainder] string url = "") => await AudioSystem.PlayAsync(Context, url);

    [Alias("list")]
    [Command("queue", RunMode = RunMode.Async)]
    [Summary("List tracks in the queue.")]
    public async Task<RuntimeResult> Queue() => await AudioSystem.ListAsync(Context);

    [Command("seek", RunMode = RunMode.Async)]
    [Summary("Seek to a position in the currently playing track.")]
    [Remarks("$seek 34:32")]
    [RequireDj]
    public async Task<RuntimeResult> Seek(string pos) => await AudioSystem.SeekAsync(Context, pos);

    [Command("shuffle", RunMode = RunMode.Async)]
    [Summary("Shuffle the queue.")]
    [RequireDj]
    public async Task<RuntimeResult> Shuffle() => await AudioSystem.ShuffleAsync(Context);

    [Alias("voteskip", "vs")]
    [Command("skip", RunMode = RunMode.Async)]
    [Summary("Vote to skip the currently playing track.")]
    public async Task<RuntimeResult> Skip() => await AudioSystem.VoteSkipTrackAsync(Context);
    
    [Command("speed", RunMode = RunMode.Async)]
    [Summary($"Change the speed (tempo + pitch) of the currently playing track (must be between ${Constants.MinSpeedString}% and ${Constants.MaxSpeedString}%).")]
    [Remarks("$speed 200")]
    [RequireDj]
    public async Task<RuntimeResult> Speed(float speed) => await AudioSystem.ChangeSpeedAsync(Context, speed);

    [Command("stop", RunMode = RunMode.Async)]
    [Summary("Stop playing entirely.")]
    [RequireDj]
    public async Task<RuntimeResult> Stop() => await AudioSystem.StopAsync(Context);
    
    [Command("tempo", RunMode = RunMode.Async)]
    [Summary($"Change the tempo of the currently playing track (must be between ${Constants.MinTempoString}% and ${Constants.MaxTempoString}%).")]
    [Remarks("$tempo 200")]
    [RequireDj]
    public async Task<RuntimeResult> Tempo(float tempo) => await AudioSystem.ChangeTempoAsync(Context, tempo);

    [Command("volume", RunMode = RunMode.Async)]
    [Summary($"Change the volume of the currently playing track (must be between ${Constants.MinVolumeString}% and ${Constants.MaxVolumeString}%).")]
    [Remarks("$volume 200")]
    [RequireDj]
    public async Task<RuntimeResult> Volume(float volume) => await AudioSystem.ChangeVolumeAsync(Context, volume);
}