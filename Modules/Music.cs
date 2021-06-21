using System.Threading.Tasks;
using Discord.Commands;
using RRBot.Preconditions;
using RRBot.Systems;

namespace RRBot.Modules
{
    public class Music : ModuleBase<SocketCommandContext>
    {
        public AudioSystem AudioSystem { get; set; }

        [Alias("np", "playing")]
        [Command("nowplaying")]
        [Summary("Gives details on the currently playing track, if there is one.")]
        [Remarks("``$nowplaying``")]
        public async Task<RuntimeResult> NowPlaying() => await AudioSystem.GetCurrentlyPlayingAsync(Context);

        [Command("play")]
        [Summary("Plays something from YouTube.")]
        [Remarks("``$play [url]``")]
        public async Task<RuntimeResult> Play([Remainder] string url) => await AudioSystem.PlayAsync(Context, url);

        [Alias("list")]
        [Command("queue")]
        [Summary("List tracks in the queue.")]
        [Remarks("``$queue``")]
        public async Task<RuntimeResult> Queue() => await AudioSystem.ListAsync(Context);

        [Command("skip")]
        [Summary("Skips the currently playing track.")]
        [Remarks("``$skip``")]
        [RequireDJ]
        public async Task<RuntimeResult> Skip() => await AudioSystem.SkipTrackAsync(Context);

        [Command("stop")]
        [Summary("Stops playing entirely.")]
        [Remarks("``$stop``")]
        [RequireDJ]
        public async Task<RuntimeResult> Stop() => await AudioSystem.StopAsync(Context);

        [Command("volume")]
        [Summary("Changes the volume of the currently playing track (must be between 5% and 200%).")]
        [Remarks("``$volume [volume]")]
        [RequireDJ]
        public async Task<RuntimeResult> ChangeVolume(int volume) => await AudioSystem.ChangeVolumeAsync(Context, volume);
    }
}
