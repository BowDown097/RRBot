using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.Entities;
using Victoria.Queue;

namespace RRBot.Systems
{
    public sealed class AudioSystem
    {
        private LavaRestClient lavaRestClient;
        private LavaSocketClient lavaSocketClient;
        private Logger logger;

        public AudioSystem(LavaRestClient rest, LavaSocketClient socket, Logger logger)
        {
            lavaRestClient = rest;
            lavaSocketClient = socket;
            this.logger = logger;
        }

        public async Task<RuntimeResult> GetCurrentlyPlayingAsync(SocketCommandContext context)
        {
            LavaPlayer player = lavaSocketClient.GetPlayer(context.Guild.Id);
            if (player != null && player.IsPlaying)
            {
                LavaTrack track = player.CurrentTrack;

                StringBuilder builder = new StringBuilder($"By: {track.Author}\n");
                if (!track.IsStream)
                {
                    TimeSpan pos = new TimeSpan(track.Position.Hours, track.Position.Minutes, track.Position.Seconds);
                    builder.AppendLine($"Length: {track.Length.ToString()}\nPosition: {pos.ToString()}");
                }

                EmbedBuilder embed = new EmbedBuilder
                {
                    Color = Color.Red,
                    Title = track.Title,
                    Description = builder.ToString()
                };

                await context.Channel.SendMessageAsync(embed: embed.Build());
                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError($"{context.User.Mention}, there is no currently playing track.");
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

            if (!track.IsStream && track.Length.TotalSeconds > 7200)
                return CommandResult.FromError($"{context.User.Mention}, this is too long for me to play! It must be 2 hours or shorter in length.");

            if (player.CurrentTrack != null && player.IsPlaying)
            {
                await context.Channel.SendMessageAsync($"**{track.Title}** has been added to the queue.");
                player.Queue.Enqueue(track);
                return CommandResult.FromSuccess();
            }

            await player.PlayAsync(track);

            StringBuilder message = new StringBuilder($"Now playing: {track.Title}\nBy: {track.Author}\n");
            if (!track.IsStream) message.AppendLine($"Length: {track.Length.ToString()}");
            message.AppendLine("*Tip: if the track instantly doesn't play, it's probably age restricted.*");

            await context.Channel.SendMessageAsync(message.ToString());

            await logger.Custom_TrackStarted(user, user.VoiceChannel, track.Uri);

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
                    playlist.AppendLine($"**{i + 1}**: {track.Title} by {track.Author}");
                    if (!track.IsStream) playlist.AppendLine($" ({track.Length.ToString()})");
                }

                EmbedBuilder embed = new EmbedBuilder
                {
                    Color = Color.Red,
                    Title = "Playlist",
                    Description = playlist.ToString()
                };

                await context.Channel.SendMessageAsync(embed: embed.Build());

                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError($"{context.User.Mention}, there are no tracks to list.");
        }

        public async Task<RuntimeResult> SkipTrackAsync(SocketCommandContext context)
        {
            LavaPlayer player = lavaSocketClient.GetPlayer(context.Guild.Id);
            if (player != null)
            {
                if (player.Queue.Count >= 1)
                {
                    LavaTrack track = player.Queue.Items.FirstOrDefault() as LavaTrack;
                    await player.PlayAsync(track);

                    StringBuilder message = new StringBuilder($"Now playing: {track.Title}\nBy: {track.Author}\n");
                    if (!track.IsStream) message.Append($"Length: {track.Length.ToString()}");

                    await context.Channel.SendMessageAsync(message.ToString());
                }
                else
                {
                    await context.Channel.SendMessageAsync("Current track skipped!");
                    await lavaSocketClient.DisconnectAsync(player.VoiceChannel);
                    await player.StopAsync();
                }

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

        // this is a fix for the player breaking if the bot is manually disconnected
        public async Task OnPlayerUpdated(LavaPlayer player, LavaTrack track, TimeSpan position)
        {
            if (!track.IsStream)
            {
                IEnumerable<IGuildUser> members = await player.VoiceChannel.GetUsersAsync().FlattenAsync();
                if (!members.Any(member => member.IsBot) && track.Position.TotalSeconds > 5)
                {
                    await lavaSocketClient.DisconnectAsync(player.VoiceChannel);
                    await player.StopAsync();
                }
            }
        }

        public async Task OnTrackFinished(LavaPlayer player, LavaTrack track, TrackEndReason reason)
        {
            if (player.Queue.Count > 0 && !reason.ShouldPlayNext())
            {
                player.Queue.Dequeue();
                return;
            }

            if (!player.Queue.TryDequeue(out IQueueObject item) || !(item is LavaTrack nextTrack) || !reason.ShouldPlayNext())
            {
                await lavaSocketClient.DisconnectAsync(player.VoiceChannel);
                await player.StopAsync();
            }
            else
            {
                await player.PlayAsync(nextTrack);

                StringBuilder message = new StringBuilder($"Now playing: {track.Title}\nBy: {track.Author}\n");
                if (!track.IsStream) message.Append($"Length: {track.Length.ToString()}");
                await player.TextChannel.SendMessageAsync(message.ToString());
            }
        }
    }
}