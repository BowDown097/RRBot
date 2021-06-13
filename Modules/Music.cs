using System.Threading.Tasks;
using Discord.Commands;
using RRBot.Preconditions;
using RRBot.Services;

namespace RRBot.Modules
{
    public class Music : ModuleBase<SocketCommandContext>
    {
        public AudioService AudioService { get; set; }

        [Alias("np", "playing")]
        [Command("nowplaying")]
        [Summary("Gives details on the currently playing track, if there is one.")]
        [Remarks("``$nowplaying``")]
        public async Task<RuntimeResult> NowPlaying() => await AudioService.GetCurrentlyPlayingAsync(Context);

        [Command("play")]
        [Summary("Plays something from YouTube.")]
        [Remarks("``$play [url]``")]
        public async Task<RuntimeResult> Play([Remainder] string url) => await AudioService.PlayAsync(Context, url);

        [Alias("list")]
        [Command("queue")]
        [Summary("List tracks in the queue.")]
        [Remarks("``$queue``")]
        public async Task<RuntimeResult> Queue() => await AudioService.ListAsync(Context);

        [Command("skip")]
        [Summary("Skips the currently playing track.")]
        [Remarks("``$skip``")]
        [RequireDJ]
        public async Task<RuntimeResult> Skip() => await AudioService.SkipTrackAsync(Context);

        [Command("stop")]
        [Summary("Stops playing entirely.")]
        [Remarks("``$stop``")]
        [RequireDJ]
        public async Task<RuntimeResult> Stop() => await AudioService.StopAsync(Context);

        [Command("volume")]
        [Summary("Changes the volume of the currently playing track (must be between 5% and 200%).")]
        [Remarks("``$volume [volume]")]
        [RequireDJ]
        public async Task<RuntimeResult> ChangeVolume(int volume) => await AudioService.ChangeVolumeAsync(Context, volume);
    }
}
