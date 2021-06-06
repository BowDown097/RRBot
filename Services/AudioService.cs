using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.Entities;
using Victoria.Queue;

namespace RRBot.Services
{
    public sealed class AudioService
    {
        private LavaRestClient lavaRestClient;
        private LavaSocketClient lavaSocketClient;

        public AudioService(LavaRestClient rest, LavaSocketClient socket)
        {
            lavaRestClient = rest;
            lavaSocketClient = socket;
        }

        public async Task<RuntimeResult> PlayAsync(SocketCommandContext context, string query)
        {
            SocketGuildUser user = context.User as SocketGuildUser;
            if (user.VoiceChannel is null) return CommandResult.FromError($"{context.User.Mention}, you must be in a voice channel.");

            await lavaSocketClient.ConnectAsync(user.VoiceChannel);
            LavaPlayer player = lavaSocketClient.GetPlayer(context.Guild.Id);

            if (player is null)
            {
                await lavaSocketClient.ConnectAsync(player.VoiceChannel);
                player = lavaSocketClient.GetPlayer(context.Guild.Id);
            }

            Victoria.Entities.SearchResult search = await lavaRestClient.SearchYouTubeAsync(query);
            if (search.LoadType == LoadType.NoMatches || search.LoadType == LoadType.LoadFailed) 
                return CommandResult.FromError($"{context.User.Mention}, I could not find anything given your query.");
            LavaTrack track = search.Tracks.FirstOrDefault();

            if (track.Length.TotalSeconds > 7200 && !track.IsStream)
            {
                await lavaSocketClient.DisconnectAsync(player.VoiceChannel);
                return CommandResult.FromError($"{context.User.Mention}, this is too long for me to play! It must be 2 hours or shorter in length.");
            }

            if (player.CurrentTrack != null && player.IsPlaying)
            {
                await context.Channel.SendMessageAsync($"**{track.Title}** has been added to the queue.");
                player.Queue.Enqueue(track);
                return CommandResult.FromSuccess();
            }

            await player.PlayAsync(track);

            string message = $"Now playing: {track.Title}\nBy: {track.Author}\n" + (track.IsStream ? "" : $"Length: {track.Length.ToString()}");
            await context.Channel.SendMessageAsync(message);
            await Program.logger.Custom_TrackStarted(user, user.VoiceChannel, track.Uri);

            return CommandResult.FromSuccess();
        }

        public async Task<RuntimeResult> ListAsync(SocketCommandContext context)
        {
            LavaPlayer player = lavaSocketClient.GetPlayer(context.Guild.Id);
            if (player != null && player.IsPlaying)
            {
                if (player.Queue.Count < 1 && player.CurrentTrack != null)
                {
                    await context.Channel.SendMessageAsync($"Now playing: {player.CurrentTrack.Title}. Nothing else is queued.");
                    return CommandResult.FromSuccess();
                }

                StringBuilder playlist = new StringBuilder();
                for (int i = 0; i < player.Queue.Items.Count(); i++)
                {
                    LavaTrack track = player.Queue.Items.ElementAt(i) as LavaTrack;
                    playlist.Append($"**{i + 1}**: {track.Title} by {track.Author} ({track.Length.ToString()})\n");
                }

                EmbedBuilder embed = new EmbedBuilder
                {
                    Color = Color.Red,
                    Title = "Playlist",
                    Description = playlist.ToString(),
                    Timestamp = DateTime.Now
                };

                await context.Channel.SendMessageAsync(embed: embed.Build());
                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError($"{context.User.Mention}, there are no tracks to list.");
        }

        public async Task<RuntimeResult> SkipTrackAsync(SocketCommandContext context)
        {
            LavaPlayer player = lavaSocketClient.GetPlayer(context.Guild.Id);
            if (player != null && player.Queue.Count >= 1)
            {
                await player.SkipAsync();
                await context.Channel.SendMessageAsync("Current track skipped!");
                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError($"{context.User.Mention}, there are no tracks to skip.");
        }

        public async Task<RuntimeResult> StopAsync(SocketCommandContext context)
        {
            LavaPlayer player = lavaSocketClient.GetPlayer(context.Guild.Id);
            if (player is null) return CommandResult.FromError($"{context.User.Mention}, the bot is not currently being used.");

            if (player.IsPlaying) await player.StopAsync();
            foreach (LavaTrack track in player.Queue.Items) player.Queue.Dequeue();
            await lavaSocketClient.DisconnectAsync(player.VoiceChannel);

            await context.Channel.SendMessageAsync("Stopped playing the current track and removed any existing tracks in the queue.");
            return CommandResult.FromSuccess();
        }

        public async Task<RuntimeResult> ChangeVolumeAsync(SocketCommandContext context, int volume)
        {
            if (volume < 5 || volume > 200) return CommandResult.FromError($"{context.User.Mention}, volume must be between 5% and 200%.");

            LavaPlayer player = lavaSocketClient.GetPlayer(context.Guild.Id);
            if (player is null) return CommandResult.FromError($"{context.User.Mention}, the bot is not currently being used.");

            await player.SetVolumeAsync(volume);
            await context.Channel.SendMessageAsync($"Set volume to {volume}%.");
            return CommandResult.FromSuccess();
        }

        public async Task OnFinished(LavaPlayer player, LavaTrack track, TrackEndReason reason)
        {
            if (!reason.ShouldPlayNext()) return;

            if (!player.Queue.TryDequeue(out IQueueObject item) || !(item is LavaTrack nextTrack))
            {
                await player.TextChannel.SendMessageAsync("There are no remaining tracks in the queue. I will now leave the voice channel.");
                await player.StopAsync();
                await lavaSocketClient.DisconnectAsync(player.VoiceChannel);
            }
            else
            {
                await player.PlayAsync(nextTrack);
                string message = $"Now playing: {track.Title}\nBy: {track.Author}\n" + (track.IsStream ? "" : $"Length: {track.Length.ToString()}");
                await player.TextChannel.SendMessageAsync(message);
            }
        }
    }
}