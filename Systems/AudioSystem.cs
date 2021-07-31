using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;
using Victoria.Responses.Search;

namespace RRBot.Systems
{
    public sealed class AudioSystem
    {
        private readonly LavaNode lavaNode;
        private Logger logger;

        public AudioSystem(LavaNode lavaNode, Logger logger)
        {
            this.lavaNode = lavaNode;
            this.logger = logger;
        }

        public async Task<RuntimeResult> GetCurrentlyPlayingAsync(SocketCommandContext context)
        {
            LavaPlayer player = lavaNode.GetPlayer(context.Guild);
            if (player != null && player.PlayerState == PlayerState.Playing)
            {
                LavaTrack track = player.Track;

                StringBuilder builder = new StringBuilder($"By: {track.Author}\n");
                if (!track.IsStream)
                {
                    TimeSpan pos = new TimeSpan(track.Position.Hours, track.Position.Minutes, track.Position.Seconds);
                    builder.AppendLine($"Length: {track.Duration.ToString()}\nPosition: {pos.ToString()}");
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

        public async Task<RuntimeResult> ListAsync(SocketCommandContext context)
        {
            LavaPlayer player = lavaNode.GetPlayer(context.Guild);
            if (player != null && player.PlayerState == PlayerState.Playing)
            {
                if (player.Queue.Count < 1 && player.Track != null)
                {
                    await context.Channel.SendMessageAsync($"Now playing: **{player.Track.Title}**. Nothing else is queued.");
                    return CommandResult.FromSuccess();
                }

                StringBuilder playlist = new StringBuilder();
                for (int i = 0; i < player.Queue.Count; i++)
                {
                    LavaTrack track = player.Queue.ElementAt(i) as LavaTrack;
                    playlist.AppendLine($"**{i + 1}**: {track.Title} by {track.Author}");
                    if (!track.IsStream) playlist.AppendLine($" ({track.Duration.ToString()})");
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

        public async Task<RuntimeResult> PlayAsync(SocketCommandContext context, string query)
        {
            SocketGuildUser user = context.User as SocketGuildUser;
            if (user.VoiceChannel is null) return CommandResult.FromError($"{context.User.Mention}, you must be in a voice channel.");
            
            await lavaNode.JoinAsync(user.VoiceChannel);
            LavaPlayer player = lavaNode.GetPlayer(context.Guild);

            if (player is null)
            {
                await lavaNode.JoinAsync(user.VoiceChannel);
                player = lavaNode.GetPlayer(context.Guild);
            }

            SearchResponse search = Uri.IsWellFormedUriString(query, UriKind.Absolute) 
                ? await lavaNode.SearchAsync(SearchType.Direct, query) 
                : await lavaNode.SearchYouTubeAsync(query);
            if (search.Status == SearchStatus.NoMatches || search.Status == SearchStatus.LoadFailed)
                return CommandResult.FromError($"{context.User.Mention}, I could not find anything given your query.");

            LavaTrack track = search.Tracks.FirstOrDefault();
            if (!track.IsStream && track.Duration.TotalSeconds > 7200)
                return CommandResult.FromError($"{context.User.Mention}, this is too long for me to play! It must be 2 hours or shorter in length.");

            if (player.Track != null && player.PlayerState == PlayerState.Playing)
            {
                await context.Channel.SendMessageAsync($"**{track.Title}** has been added to the queue.");
                player.Queue.Enqueue(track);
                return CommandResult.FromSuccess();
            }

            await player.PlayAsync(track);

            StringBuilder message = new StringBuilder($"Now playing: {track.Title}\nBy: {track.Author}\n");
            if (!track.IsStream) message.AppendLine($"Length: {track.Duration.ToString()}");
            message.AppendLine("*Tip: if the track instantly doesn't play, it's probably age restricted.*");

            await context.Channel.SendMessageAsync(message.ToString());
            await logger.Custom_TrackStarted(user, user.VoiceChannel, track.Url);
            return CommandResult.FromSuccess();
        }

        public async Task<RuntimeResult> SkipTrackAsync(SocketCommandContext context)
        {
            LavaPlayer player = lavaNode.GetPlayer(context.Guild);
            if (player != null)
            {
                if (player.Queue.Count >= 1)
                {
                    LavaTrack track = player.Queue.FirstOrDefault() as LavaTrack;
                    await player.PlayAsync(track);

                    StringBuilder message = new StringBuilder($"Now playing: {track.Title}\nBy: {track.Author}\n");
                    if (!track.IsStream) message.Append($"Length: {track.Duration.ToString()}");

                    await context.Channel.SendMessageAsync(message.ToString());
                }
                else
                {
                    await context.Channel.SendMessageAsync("Current track skipped!");
                    await lavaNode.LeaveAsync(player.VoiceChannel);
                    await player.StopAsync();
                }

                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError($"{context.User.Mention}, there are no tracks to skip.");
        }

        public async Task<RuntimeResult> StopAsync(SocketCommandContext context)
        {
            LavaPlayer player = lavaNode.GetPlayer(context.Guild);
            if (player is null) return CommandResult.FromError($"{context.User.Mention}, the bot is not currently being used.");

            if (player.PlayerState == PlayerState.Playing) await player.StopAsync();
            foreach (LavaTrack track in player.Queue) player.Queue.TryDequeue(out _);
            await lavaNode.LeaveAsync(player.VoiceChannel);

            await context.Channel.SendMessageAsync("Stopped playing the current track and removed any existing tracks in the queue.");
            return CommandResult.FromSuccess();
        }

        public async Task<RuntimeResult> ChangeVolumeAsync(SocketCommandContext context, ushort volume)
        {
            if (volume < 5 || volume > 200) return CommandResult.FromError($"{context.User.Mention}, volume must be between 5% and 200%.");

            LavaPlayer player = lavaNode.GetPlayer(context.Guild);
            if (player is null) return CommandResult.FromError($"{context.User.Mention}, the bot is not currently being used.");

            await player.UpdateVolumeAsync(volume);
            await context.Channel.SendMessageAsync($"Set volume to {volume}%.");
            return CommandResult.FromSuccess();
        }

        // this is a fix for the player breaking if the bot is manually disconnected
        public async Task OnPlayerUpdated(PlayerUpdateEventArgs args)
        {
            if (!args.Track.IsStream)
            {
                IEnumerable<IGuildUser> members = await args.Player.VoiceChannel.GetUsersAsync().FlattenAsync();
                if (!members.Any(member => member.IsBot) && args.Track.Position.TotalSeconds > 5)
                {
                    await lavaNode.LeaveAsync(args.Player.VoiceChannel);
                    await args.Player.StopAsync();
                }
            }
        }

        public async Task OnTrackEnded(TrackEndedEventArgs args)
        {
            if (args.Player.Queue.Count > 0 && !args.Reason.ShouldPlayNext())
            {
                args.Player.Queue.TryDequeue(out _);
                return;
            }

            if (!args.Player.Queue.TryDequeue(out LavaTrack track) || !args.Reason.ShouldPlayNext())
            {
                await lavaNode.LeaveAsync(args.Player.VoiceChannel);
                await args.Player.StopAsync();
            }
            else
            {
                await args.Player.PlayAsync(track);

                StringBuilder message = new StringBuilder($"Now playing: {track.Title}\nBy: {track.Author}\n");
                if (!track.IsStream) message.Append($"Length: {track.Duration.ToString()}");
                await args.Player.TextChannel.SendMessageAsync(message.ToString());
            }
        }
    }
}