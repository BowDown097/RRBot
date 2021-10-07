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
            embed.Timestamp = DateTime.Now;
            DbConfigChannels channels = await DbConfigChannels.GetById(guild.Id);
            if (guild.TextChannels.Any(channel => channel.Id == channels.LogsChannel))
                await guild.GetTextChannel(channels.LogsChannel).SendMessageAsync(embed: embed.Build());
        }

        public static async Task Client_ChannelCreated(SocketChannel channel)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "Channel Created",
                Description = channel.ToString()
            };

            await WriteToLogs((channel as SocketGuildChannel)?.Guild, embed);
        }

        public static async Task Client_ChannelDestroyed(SocketChannel channel)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "Channel Deleted",
                Description = channel.ToString()
            };

            await WriteToLogs((channel as SocketGuildChannel)?.Guild, embed);
        }

        public static async Task Client_ChannelUpdated(SocketChannel before, SocketChannel after)
        {
            SocketGuildChannel beforeGuild = before as SocketGuildChannel;
            SocketGuildChannel afterGuild = after as SocketGuildChannel;
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "Channel Updated",
                Description = $"**Previous Name:** ``{before}``\n**New Name:** ``{after}\n\n" +
                $"**Previous Position:** ``{beforeGuild.Position}``\n**New Position:** ``{afterGuild.Position}``\n\n" +
                $"**Previous Member Count:** ``{before.Users.Count}``\n**New Member Count:** ``{after.Users.Count}``\n" +
                "If nothing here appears changed, then the channel permissions were updated."
            };

            await WriteToLogs(afterGuild.Guild, embed);
        }

        public static async Task Client_InviteCreated(SocketInvite invite)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "Invite Created",
                Description = $"**URL:** ``{invite.Url}``\n**Channel:** ``{invite.Channel}``\n**Inviter:** ``{invite.Inviter}``\n" +
                $"**Max Age:** ``{invite.MaxAge}``\n**Max Uses:** ``{invite.MaxUses}``"
            };

            await WriteToLogs(invite.Guild, embed);
        }

        public static async Task Client_MessageDeleted(Cacheable<IMessage, ulong> msgCached, Cacheable<IMessageChannel, ulong> channelCached)
        {
            IMessage msg = await msgCached.GetOrDownloadAsync();
            SocketGuildChannel channel = await channelCached.GetOrDownloadAsync() as SocketGuildChannel;
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = $"Message sent by {msg.Author} deleted in #{channel}",
                Description = msg.Content
            };

            foreach (Embed msgEmbed in msg.Embeds)
                embed.Description += $"\n**Embed:**\nTitle: {msgEmbed.Title}\nDescription: {msgEmbed.Description}";

            await WriteToLogs(channel.Guild, embed);
        }

        public static async Task Client_MessageUpdated(Cacheable<IMessage, ulong> msgBeforeCached, SocketMessage msgAfter, ISocketMessageChannel channel)
        {
            IMessage msgBefore = await msgBeforeCached.GetOrDownloadAsync();
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = $"Message sent by {msgAfter.Author} updated in #{channel}",
                Description = "**Previous Content:** "
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
                Description = role.Name
            };

            await WriteToLogs(role.Guild, embed);
        }

        public static async Task Client_RoleDeleted(SocketRole role)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "Role Deleted",
                Description = role.Name
            };

            await WriteToLogs(role.Guild, embed);
        }

        public static async Task Client_SpeakerAdded(SocketStageChannel stage, SocketGuildUser user)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = $"Speaker Added in #{stage}",
                Description = user.ToString()
            };

            await WriteToLogs(user.Guild, embed);
        }

        public static async Task Client_SpeakerRemoved(SocketStageChannel stage, SocketGuildUser user)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = $"Speaker Removed in #{stage}",
                Description = user.ToString()
            };

            await WriteToLogs(user.Guild, embed);
        }

        public static async Task Client_StageEnded(SocketStageChannel stage)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "Stage Ended",
                Description = $"**Topic:** {stage.Topic}\n**In:** {stage}"
            };

            await WriteToLogs(stage.Guild, embed);
        }

        public static async Task Client_StageStarted(SocketStageChannel stage)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "Stage Started",
                Description = $"**Topic:** {stage.Topic}\n**In:** {stage}"
            };

            await WriteToLogs(stage.Guild, embed);
        }

        public static async Task Client_StageUpdated(SocketStageChannel stageBefore, SocketStageChannel stageAfter)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "Stage Updated",
                Description = $"**Previous Channel Name:** {stageBefore}\n**New Channel Name:** {stageAfter}\n\n" +
                    $"**Previous Topic:** {stageBefore.Topic}\n**New Lock Status:** {stageAfter.Topic}\n\n" +
                    $"**Previous Channel Position:** {stageBefore.Position}\n**New Channel Position:** {stageAfter.Position}\n" +
                    $"**Previous Discoverability Status:** {!stageBefore.DiscoverableDisabled}\n**New Discoverability Status:** {!stageAfter.DiscoverableDisabled}\n\n" +
                    $"**Previous User Limit:** {stageBefore.UserLimit}\n**New User Limit:** {stageAfter.UserLimit}\n" +
                    "If nothing here appears changed, then the channel permissions were updated."
            };

            await WriteToLogs(stageAfter.Guild, embed);
        }

        public static async Task Client_ThreadCreated(SocketThreadChannel threadChannel)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "Thread Created",
                Description = threadChannel.Name
            };

            await WriteToLogs(threadChannel.Guild, embed);
        }

        public static async Task Client_ThreadDeleted(Cacheable<SocketThreadChannel, ulong> threadChannelCached)
        {
            SocketThreadChannel threadChannel = await threadChannelCached.GetOrDownloadAsync();
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "Thread Deleted",
                Description = threadChannel.Name
            };

            await WriteToLogs(threadChannel.Guild, embed);
        }

        public static async Task Client_ThreadMemberJoined(SocketThreadUser threadUser)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "User Joined Thread",
                Description = threadUser.ToString()
            };

            await WriteToLogs(threadUser.Guild, embed);
        }

        public static async Task Client_ThreadMemberLeft(SocketThreadUser threadUser)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "User Left Thread",
                Description = threadUser.ToString()
            };

            await WriteToLogs(threadUser.Guild, embed);
        }

        public static async Task Client_ThreadUpdated(Cacheable<SocketThreadChannel, ulong> threadBeforeCached, SocketThreadChannel threadAfter)
        {
            SocketThreadChannel threadBefore = await threadBeforeCached.GetOrDownloadAsync();
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "Thread Updated",
                Description = $"**Previous Name:** {threadBefore}\n**New Name:** {threadAfter}\n\n" +
                    $"**Previous Lock Status:** {threadBefore.Locked}\n**New Lock Status:** {threadAfter.Locked}\n\n" +
                    $"**Previous Member Count:** {threadBefore.MemberCount}\n**New Member Count:** {threadAfter.MemberCount}\n\n" +
                    $"**Previous Position:** {threadBefore.Position}\n**New Position:** {threadAfter.Position}\n" +
                    "If nothing here appears changed, then the channel permissions were updated."
            };

            await WriteToLogs(threadAfter.Guild, embed);
        }

        public static async Task Client_UserBanned(SocketUser user, SocketGuild guild)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "User Banned",
                Description = user.ToString()
            };

            await WriteToLogs(guild, embed);
        }

        public static async Task Client_UserJoined(SocketGuildUser user)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "User Joined",
                Description = user.ToString()
            };

            await WriteToLogs(user.Guild, embed);
        }

        public static async Task Client_UserLeft(SocketGuildUser user)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "User Left",
                Description = user.ToString()
            };

            await WriteToLogs(user.Guild, embed);
        }

        public static async Task Client_UserUnbanned(SocketUser user, SocketGuild guild)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "User Unbanned",
                Description = user.ToString()
            };

            await WriteToLogs(guild, embed);
        }

        public static async Task Client_UserVoiceStateUpdated(SocketUser user, SocketVoiceState voiceStateOrig, SocketVoiceState voiceState)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Description = voiceState.VoiceChannel == null
                    ? $"{user}\nIn: {voiceStateOrig.VoiceChannel}"
                    : $"{user}\nIn: {voiceState.VoiceChannel}"
            };

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
                Description = $"See them at: https://hastebin.com/{hbKey}"
            };

            await WriteToLogs(guild, embed);
        }

        public static async Task Custom_TrackStarted(SocketGuildUser user, Uri url)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = $"Track started by {user}",
                Description = $"URL: {url}"
            };

            await WriteToLogs(user.Guild, embed);
        }

        public static async Task Custom_UserBullied(IGuildUser target, SocketUser actor, string nickname)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "User Bullied",
                Description = $"{actor} bullied {target} to '{nickname}'"
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
                Description = description
            };

            await WriteToLogs(target.Guild as SocketGuild, embed);
        }

        public static async Task Custom_UserUnmuted(IGuildUser target, SocketUser actor)
        {
            EmbedBuilder embed = new()
            {
                Color = Color.Blue,
                Title = "User Unmuted",
                Description = $"{actor} unmuted {target}"
            };

            await WriteToLogs(target.Guild as SocketGuild, embed);
        }
    }
}
