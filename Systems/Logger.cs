using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using Newtonsoft.Json.Linq;

namespace RRBot.Systems
{
    public class Logger
    {
        private readonly DiscordSocketClient client;

        public Logger(DiscordSocketClient client) => this.client = client;

        private async Task WriteToLogs(SocketGuild guild, EmbedBuilder embed)
        {
            DocumentReference doc = Program.database.Collection($"servers/{guild.Id}/config").Document("channels");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("logsChannel", out ulong id))
                await guild.GetTextChannel(id).SendMessageAsync(embed: embed.Build());
        }

        public async Task Client_ChannelCreated(SocketChannel channel)
        {
            if (channel is SocketGuildChannel channelGuild)
            {
                EmbedBuilder embed = new EmbedBuilder
                {
                    Color = Color.Blue,
                    Title = "Channel Created",
                    Description = channelGuild.Name,
                    Timestamp = DateTime.Now
                };

                await WriteToLogs(channelGuild.Guild, embed);
            }
        }

        public async Task Client_ChannelDestroyed(SocketChannel channel)
        {
            if (channel is SocketGuildChannel channelGuild)
            {
                EmbedBuilder embed = new EmbedBuilder
                {
                    Color = Color.Blue,
                    Title = "Channel Deleted",
                    Description = channelGuild.Name,
                    Timestamp = DateTime.Now
                };

                await WriteToLogs(channelGuild.Guild, embed);
            }
        }

        public async Task Client_ChannelUpdated(SocketChannel before, SocketChannel after)
        {
            if (before is SocketGuildChannel beforeGuild && after is SocketGuildChannel afterGuild)
            {
                EmbedBuilder embed = new EmbedBuilder
                {
                    Color = Color.Blue,
                    Title = "Channel Updated",
                    Description = "**Previous Name:** ``" + beforeGuild.Name + "``\n**Current Name:** ``" + afterGuild.Name + "``\n" +
                    "**Previous Position:** ``" + beforeGuild.Position + "``\n**Current Position:** ``" + afterGuild.Position + "``\n" +
                    "**Previous Member Count:** ``" + beforeGuild.Users.Count + "``\n**Current Member Count:** ``" + afterGuild.Users.Count + "``\n" +
                    "If nothing here appears changed, then the channel permissions were updated.",
                    Timestamp = DateTime.Now
                };

                await WriteToLogs(afterGuild.Guild, embed);
            }
        }

        public async Task Client_InviteCreated(SocketInvite invite)
        {
            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Blue,
                Title = "Invite Created",
                Description = "**URL:** ``" + invite.Url + "``\n**Channel:** ``" + invite.Channel.Name + "``\n**Inviter:** ``" + invite.Inviter + "``\n" +
                "**Max Age:** ``" + invite.MaxAge + "``\n**Max Uses:** ``" + invite.MaxUses + "``\n",
                Timestamp = DateTime.Now
            };

            await WriteToLogs(invite.Guild, embed);
        }

        public async Task Client_MessageDeleted(Cacheable<IMessage, ulong> msgCached, ISocketMessageChannel channel)
        {
            if (!msgCached.HasValue) return;

            IMessage msg = msgCached.Value;
            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Blue,
                Title = $"Message sent by {msg.Author.ToString()} deleted in {channel.ToString()}",
                Description = msg.Content,
                Timestamp = msg.Timestamp
            };

            await WriteToLogs((channel as SocketGuildChannel).Guild, embed);
        }

        public async Task Client_MessageUpdated(Cacheable<IMessage, ulong> msgBeforeCached, SocketMessage msgAfter, ISocketMessageChannel channel)
        {
            if (!msgBeforeCached.HasValue) return;

            IMessage msgBefore = msgBeforeCached.Value;
            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Blue,
                Title = $"Message sent by {msgAfter.Author.ToString()} updated in {channel.ToString()}",
                Description = "**Previous Content:** ``" + msgBefore.Content + "``\n**New Content:** ``" + msgAfter.Content + "``",
                Timestamp = DateTime.Now
            };

            if (Global.niggerRegex.Matches(new string(msgAfter.Content.Where(char.IsLetter).ToArray()).ToLower()).Count != 0)
            {
                Global.RunInBackground(() =>
                {
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    msgAfter.DeleteAsync();
                });
            }

            await WriteToLogs((channel as SocketGuildChannel).Guild, embed);
        }

        public async Task Client_RoleCreated(SocketRole role)
        {
            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Blue,
                Title = "Role Created",
                Description = role.Name,
                Timestamp = DateTime.Now
            };

            await WriteToLogs(role.Guild, embed);
        }

        public async Task Client_RoleDeleted(SocketRole role)
        {
            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Blue,
                Title = "Role Deleted",
                Description = role.Name,
                Timestamp = DateTime.Now
            };

            await WriteToLogs(role.Guild, embed);
        }

        public async Task Client_UserBanned(SocketUser user, SocketGuild guild)
        {
            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Blue,
                Title = "User Banned",
                Description = user.ToString(),
                Timestamp = DateTime.Now
            };

            await WriteToLogs(guild, embed);
        }

        public async Task Client_UserJoined(SocketGuildUser user)
        {
            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Blue,
                Title = "User Joined",
                Description = user.ToString(),
                Timestamp = DateTime.Now
            };

            await WriteToLogs(user.Guild, embed);
        }

        public async Task Client_UserLeft(SocketGuildUser user)
        {
            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Blue,
                Title = "User Left",
                Description = user.ToString(),
                Timestamp = DateTime.Now
            };

            await WriteToLogs(user.Guild, embed);
        }

        public async Task Client_UserUnbanned(SocketUser user, SocketGuild guild)
        {
            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Blue,
                Title = "User Unbanned",
                Description = user.ToString(),
                Timestamp = DateTime.Now
            };

            await WriteToLogs(guild, embed);
        }

        public async Task Client_UserVoiceStateUpdated(SocketUser user, SocketVoiceState voiceStateOrig, SocketVoiceState voiceState)
        {
            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Blue,
                Description = $"{user.ToString()}\nIn: {voiceState.VoiceChannel.ToString()}",
                Timestamp = DateTime.Now
            };

            if (!voiceStateOrig.IsDeafened && voiceState.IsDeafened) embed.Title = "User Server Deafened";
            else if (voiceStateOrig.IsDeafened && !voiceState.IsDeafened) embed.Title = "User Server Undeafened";
            else if (!voiceStateOrig.IsMuted && voiceState.IsMuted) embed.Title = "User Server Muted";
            else if (voiceStateOrig.IsMuted && !voiceState.IsMuted) embed.Title = "User Server Unmuted";
            else if (!voiceStateOrig.IsSelfDeafened && voiceState.IsSelfDeafened) embed.Title = "User Self Deafened";
            else if (voiceStateOrig.IsSelfDeafened && !voiceState.IsSelfDeafened) embed.Title = "User Self Undeafened";
            else if (!voiceStateOrig.IsSelfMuted && voiceState.IsSelfMuted) embed.Title = "User Self Muted";
            else if (voiceStateOrig.IsSelfMuted && !voiceState.IsSelfMuted) embed.Title = "User Self Unmuted";
            else if (voiceStateOrig.VoiceChannel == null) embed.Title = "User Joined Voice Channel";
            else if (voiceStateOrig.VoiceChannel.Id != voiceState.VoiceChannel.Id)
            {
                embed.Title = "User Moved Voice Channels";
                embed.Description = $"Original: ``{voiceStateOrig.VoiceChannel.ToString()}``\nCurrent: ``{voiceState.VoiceChannel.ToString()}``";
            }

            await WriteToLogs(voiceState.VoiceChannel.Guild, embed);     
        }

        public Task Custom_MessagesPurged(IEnumerable<IMessage> messages, SocketGuild guild)
        {
            string msgLogs = string.Empty;
            foreach (IMessage message in messages)
                msgLogs += $"{message.Author.ToString()} @ {message.Timestamp.ToString()}: {message.Content}\n";

            Global.RunInBackground(() =>
            {
                try
                {
                    using (WebClient webClient = new WebClient())
                    {
                        string hbPOST = webClient.UploadString("https://hastebin.com/documents", msgLogs);
                        string hbKey = JObject.Parse(hbPOST)["key"].ToString();
                        string hbUrl = $"https://hastebin.com/{hbKey}";

                        EmbedBuilder embed = new EmbedBuilder
                        {
                            Color = Color.Blue,
                            Title = $"{messages.Count() - 1} Messages Purged",
                            Description = $"See them at: {hbUrl}",
                            Timestamp = DateTime.Now
                        };

                        WriteToLogs(guild, embed);
                    }
                }
                catch (WebException)
                {
                    EmbedBuilder embed = new EmbedBuilder
                    {
                        Color = Color.Blue,
                        Title = $"{messages.Count() - 1} Messages Purged",
                        Description = "I couldn't upload the messages to HasteBin for some reason, attaching them instead.",
                        Timestamp = DateTime.Now
                    };

                    File.WriteAllText("messages.txt", msgLogs);
                    WriteToLogs(guild, embed);
                    DocumentReference doc = Program.database.Collection($"servers/{guild.Id}/config").Document("channels");
                    DocumentSnapshot snap = doc.GetSnapshotAsync().Result;
                    if (snap.TryGetValue("logsChannel", out ulong id))
                        guild.GetTextChannel(id).SendFileAsync("messages.txt", string.Empty);
                    File.Delete("messages.txt");
                }
            });

            return Task.CompletedTask;
        }

        public async Task Custom_TrackStarted(SocketGuildUser user, SocketVoiceChannel vc, Uri url)
        {
            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Blue,
                Title = $"Track started by {user.ToString()}",
                Description = $"URL: {url}",
                Timestamp = DateTime.Now
            };

            await WriteToLogs(user.Guild, embed);
        }

        public async Task Custom_UserBullied(IGuildUser target, SocketUser actor, string nickname)
        {
            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Blue,
                Title = "User Bullied",
                Description = $"{actor.ToString()} bullied {target.ToString()} to '{nickname}'",
                Timestamp = DateTime.Now
            };

            await WriteToLogs(target.Guild as SocketGuild, embed);
        }

        public async Task Custom_UserMuted(IGuildUser target, SocketUser actor, string duration, string reason)
        {
            string description = $"{actor.ToString()} muted {target.ToString()} for {duration}";
            if (!string.IsNullOrEmpty(reason)) description += $" for '{reason}'";

            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Blue,
                Title = "User Muted",
                Description = description,
                Timestamp = DateTime.Now
            };

            await WriteToLogs(target.Guild as SocketGuild, embed);
        }

        public async Task Custom_UserUnmuted(IGuildUser target, SocketUser actor)
        {
            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Blue,
                Title = "User Unmuted",
                Description = $"{actor.ToString()} unmuted {target.ToString()}",
                Timestamp = DateTime.Now
            };

            await WriteToLogs(target.Guild as SocketGuild, embed);
        }
    }
}
