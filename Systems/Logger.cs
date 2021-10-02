using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using RRBot.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RRBot.Systems
{
    public static class Logger
    {
        private static async Task WriteToLogs(SocketGuild guild, EmbedBuilder embed)
        {
            DbConfigChannels channels = await DbConfigChannels.GetById(guild.Id);
            if (guild.TextChannels.Any(channel => channel.Id == channels.LogsChannel))
                await guild.GetTextChannel(channels.LogsChannel).SendMessageAsync(embed: embed.Build());
        }

        public static async Task Client_ChannelCreated(SocketChannel channel)
        {
            if (channel is SocketGuildChannel channelGuild)
            {
                EmbedBuilder embed = new()
                {
                    Color = Color.Blue,
                    Title = "Channel Created",
                    Description = channelGuild.Name,
                    Timestamp = DateTime.Now
                };

                await WriteToLogs(channelGuild.Guild, embed);
            }
        }

        public static async Task Client_ChannelDestroyed(SocketChannel channel)
        {
            if (channel is SocketGuildChannel channelGuild)
            {
                EmbedBuilder embed = new()
                {
                    Color = Color.Blue,
                    Title = "Channel Deleted",
                    Description = channelGuild.Name,
                    Timestamp = DateTime.Now
                };

                await WriteToLogs(channelGuild.Guild, embed);
            }
        }

        public static async Task Client_ChannelUpdated(SocketChannel before, SocketChannel after)
        {
            if (before is SocketGuildChannel beforeGuild && after is SocketGuildChannel afterGuild)
            {
                EmbedBuilder embed = new()
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

        public static async Task Client_InviteCreated(SocketInvite invite)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "Invite Created",
                Description = "**URL:** ``" + invite.Url + "``\n**Channel:** ``" + invite.Channel.Name + "``\n**Inviter:** ``" + invite.Inviter + "``\n" +
                "**Max Age:** ``" + invite.MaxAge + "``\n**Max Uses:** ``" + invite.MaxUses + "``\n",
                Timestamp = DateTime.Now
            };

            await WriteToLogs(invite.Guild, embed);
        }

        public static async Task Client_MessageDeleted(Cacheable<IMessage, ulong> msgCached, ISocketMessageChannel channel)
        {
            if (!msgCached.HasValue)
                return;

            IMessage msg = msgCached.Value;
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = $"Message sent by {msg.Author} deleted in #{channel}",
                Description = msg.Content,
                Timestamp = msg.Timestamp
            };

            foreach (Embed msgEmbed in msg.Embeds)
                embed.Description += $"\n**Embed:**\nTitle: {msgEmbed.Title}\nDescription: {msgEmbed.Description}";

            await WriteToLogs((channel as SocketGuildChannel)?.Guild, embed);
        }

        public static async Task Client_MessageUpdated(Cacheable<IMessage, ulong> msgBeforeCached, SocketMessage msgAfter, ISocketMessageChannel channel)
        {
            if (!msgBeforeCached.HasValue)
                return;

            IMessage msgBefore = msgBeforeCached.Value;
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = $"Message sent by {msgAfter.Author} updated in #{channel}",
                Description = "**Previous Content:** ",
                Timestamp = DateTime.Now
            };

            if (!string.IsNullOrWhiteSpace(msgBefore.Content))
            {
                embed.Description += msgBefore.Content;
            }
            else if (msgBefore.Embeds.Count > 0)
            {
                foreach (Embed msgEmbed in msgBefore.Embeds)
                    embed.Description += $"\n**Embed:**\nTitle: {msgEmbed.Title}\nDescription: {msgEmbed.Description}";
            }
            else
            {
                embed.Description += "None";
            }

            embed.Description += "\n**New Content:** ";
            if (!string.IsNullOrWhiteSpace(msgAfter.Content))
            {
                embed.Description += msgAfter.Content;
            }
            else if (msgAfter.Embeds.Count > 0)
            {
                foreach (Embed msgEmbed in msgAfter.Embeds)
                    embed.Description += $"\n**Embed:**\nTitle: {msgEmbed.Title}\nDescription: {msgEmbed.Description}";
            }
            else
            {
                embed.Description += "None";
            }

            await WriteToLogs((channel as SocketGuildChannel)?.Guild, embed);
        }

        public static async Task Client_RoleCreated(SocketRole role)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "Role Created",
                Description = role.Name,
                Timestamp = DateTime.Now
            };

            await WriteToLogs(role.Guild, embed);
        }

        public static async Task Client_RoleDeleted(SocketRole role)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "Role Deleted",
                Description = role.Name,
                Timestamp = DateTime.Now
            };

            await WriteToLogs(role.Guild, embed);
        }

        public static async Task Client_UserBanned(SocketUser user, SocketGuild guild)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "User Banned",
                Description = user.ToString(),
                Timestamp = DateTime.Now
            };

            await WriteToLogs(guild, embed);
        }

        public static async Task Client_UserJoined(SocketGuildUser user)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "User Joined",
                Description = user.ToString(),
                Timestamp = DateTime.Now
            };

            await WriteToLogs(user.Guild, embed);
        }

        public static async Task Client_UserLeft(SocketGuildUser user)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "User Left",
                Description = user.ToString(),
                Timestamp = DateTime.Now
            };

            await WriteToLogs(user.Guild, embed);
        }

        public static async Task Client_UserUnbanned(SocketUser user, SocketGuild guild)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "User Unbanned",
                Description = user.ToString(),
                Timestamp = DateTime.Now
            };

            await WriteToLogs(guild, embed);
        }

        public static async Task Client_UserVoiceStateUpdated(SocketUser user, SocketVoiceState voiceStateOrig, SocketVoiceState voiceState)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Timestamp = DateTime.Now
            };

            embed.Description = voiceState.VoiceChannel == null
                ? $"{user}\nIn: {voiceStateOrig.VoiceChannel}"
                : $"{user}\nIn: {voiceState.VoiceChannel}";

            if (voiceStateOrig.VoiceChannel == null)
            {
                embed.Title = "User Joined Voice Channel";
            }
            else if (voiceState.VoiceChannel == null)
            {
                embed.Title = "User Left Voice Channel";
            }
            else if (!voiceStateOrig.IsDeafened && voiceState.IsDeafened)
            {
                embed.Title = "User Server Deafened";
            }
            else if (voiceStateOrig.IsDeafened && !voiceState.IsDeafened)
            {
                embed.Title = "User Server Undeafened";
            }
            else if (!voiceStateOrig.IsMuted && voiceState.IsMuted)
            {
                embed.Title = "User Server Muted";
            }
            else if (voiceStateOrig.IsMuted && !voiceState.IsMuted)
            {
                embed.Title = "User Server Unmuted";
            }
            else if (!voiceStateOrig.IsSelfDeafened && voiceState.IsSelfDeafened)
            {
                embed.Title = "User Self Deafened";
            }
            else if (voiceStateOrig.IsSelfDeafened && !voiceState.IsSelfDeafened)
            {
                embed.Title = "User Self Undeafened";
            }
            else if (!voiceStateOrig.IsSelfMuted && voiceState.IsSelfMuted)
            {
                embed.Title = "User Self Muted";
            }
            else if (voiceStateOrig.IsSelfMuted && !voiceState.IsSelfMuted)
            {
                embed.Title = "User Self Unmuted";
            }
            else if (voiceStateOrig.VoiceChannel.Id != voiceState.VoiceChannel.Id)
            {
                embed.Title = "User Moved Voice Channels";
                embed.Description = $"{user}\nOriginal: {voiceStateOrig.VoiceChannel}\nCurrent: {voiceState.VoiceChannel}";
            }
            else
            {
                embed.Title = "User Voice Status Changed";
            }

            await WriteToLogs((user as SocketGuildUser)?.Guild, embed);
        }

        public static async Task Custom_MessagesPurged(IEnumerable<IMessage> messages, SocketGuild guild)
        {
            StringBuilder msgLogs = new();
            foreach (IMessage message in messages)
                msgLogs.AppendLine($"{message.Author} @ {message.Timestamp}: {message.Content}");

            using WebClient webClient = new();
            string hbPOST = await webClient.UploadStringTaskAsync("https://hastebin.com/documents", msgLogs.ToString());
            string hbKey = JObject.Parse(hbPOST)["key"].ToString();

            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = $"{messages.Count() - 1} Messages Purged",
                Description = $"See them at: https://hastebin.com/{hbKey}",
                Timestamp = DateTime.Now
            };

            await WriteToLogs(guild, embed);
        }

        public static async Task Custom_TrackStarted(SocketGuildUser user, Uri url)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = $"Track started by {user}",
                Description = $"URL: {url}",
                Timestamp = DateTime.Now
            };

            await WriteToLogs(user.Guild, embed);
        }

        public static async Task Custom_UserBullied(IGuildUser target, SocketUser actor, string nickname)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "User Bullied",
                Description = $"{actor} bullied {target} to '{nickname}'",
                Timestamp = DateTime.Now
            };

            await WriteToLogs(target.Guild as SocketGuild, embed);
        }

        public static async Task Custom_UserMuted(IGuildUser target, SocketUser actor, string duration, string reason)
        {
            string description = $"{actor} muted {target} for {duration}";
            if (!string.IsNullOrEmpty(reason))
                description += $" for '{reason}'";

            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "User Muted",
                Description = description,
                Timestamp = DateTime.Now
            };

            await WriteToLogs(target.Guild as SocketGuild, embed);
        }

        public static async Task Custom_UserUnmuted(IGuildUser target, SocketUser actor)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "User Unmuted",
                Description = $"{actor} unmuted {target}",
                Timestamp = DateTime.Now
            };

            await WriteToLogs(target.Guild as SocketGuild, embed);
        }
    }
}
