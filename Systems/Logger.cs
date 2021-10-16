using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using RRBot.Entities;
using RRBot.Extensions;
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
            embed.Color = Color.Blue;
            embed.Timestamp = DateTime.Now;
            DbConfigChannels channels = await DbConfigChannels.GetById(guild.Id);
            if (guild.TextChannels.Any(channel => channel.Id == channels.LogsChannel))
                await guild.GetTextChannel(channels.LogsChannel).SendMessageAsync(embed: embed.Build());
        }

        public static async Task Client_ChannelCreated(SocketChannel channel)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Channel Created")
                .WithDescription($"<#{channel}>");

            await WriteToLogs((channel as SocketGuildChannel)?.Guild, embed);
        }

        public static async Task Client_ChannelDestroyed(SocketChannel channel)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Channel Deleted")
                .WithDescription($"<#{channel}>");

            await WriteToLogs((channel as SocketGuildChannel)?.Guild, embed);
        }

        public static async Task Client_ChannelUpdated(SocketChannel before, SocketChannel after)
        {
            SocketTextChannel beforeText = before as SocketTextChannel;
            SocketTextChannel afterText = after as SocketTextChannel;
            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Channel Updated")
                .WithDescription("*(If nothing here appears changed, then the channel permissions were updated)*")
                .AddField("Previous Name", before, true)
                .AddField("New Name", after, true)
                .AddSeparatorField()
                .AddField("Previous Topic", beforeText.Topic, true)
                .AddField("New Topic", afterText.Topic, true)
                .AddSeparatorField()
                .AddField("Previous Position", beforeText.Position, true)
                .AddField("New Position", afterText.Position, true)
                .AddSeparatorField()
                .AddField("Previous Member Count", before.Users.Count, true)
                .AddField("New Member Count", after.Users.Count, true);

            await WriteToLogs(beforeText.Guild, embed);
        }

        public static async Task Client_InviteCreated(SocketInvite invite)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Invite Created")
                .AddField("URL", invite.Url)
                .AddField("Channel", invite.Channel)
                .AddField("Inviter", invite.Inviter)
                .AddField("Max Age", invite.MaxAge)
                .AddField("Max Uses", invite.MaxUses);

            await WriteToLogs(invite.Guild, embed);
        }

        public static async Task Client_MessageDeleted(Cacheable<IMessage, ulong> msgCached, Cacheable<IMessageChannel, ulong> channelCached)
        {
            IMessage msg = await msgCached.GetOrDownloadAsync();
            SocketGuildChannel channel = await channelCached.GetOrDownloadAsync() as SocketGuildChannel;
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(msg.Author)
                .WithTitle($"Message Deleted in <#{channel}>")
                .WithDescription(msg.Content)
                .WithFooter($"ID: {msg.Id}");

            foreach (Embed msgEmbed in msg.Embeds)
            {
                embed = embed.AddField("Embed Title", msgEmbed.Title)
                    .AddField("Embed Description", msgEmbed.Description)
                    .AddSeparatorField();
            }

            await WriteToLogs(channel.Guild, embed);
        }

        public static async Task Client_MessageUpdated(Cacheable<IMessage, ulong> msgBeforeCached, SocketMessage msgAfter, ISocketMessageChannel channel)
        {
            IMessage msgBefore = await msgBeforeCached.GetOrDownloadAsync();
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(msgAfter.Author)
                .WithTitle($"Message Updated in <#{channel}>")
                .AddField("Previous Content", msgBefore.Content ?? "N/A");

            foreach (Embed msgEmbed in msgBefore.Embeds)
            {
                embed = embed.AddField("Embed Title", msgEmbed.Title)
                    .AddField("Embed Description", msgEmbed.Description)
                    .AddSeparatorField();
            }

            embed = embed.AddField("New Content", msgAfter.Content ?? "N/A");
            foreach (Embed msgEmbed in msgAfter.Embeds)
            {
                embed = embed.AddField("Embed Title", msgEmbed.Title)
                    .AddField("Embed Description", msgEmbed.Description)
                    .AddSeparatorField();
            }

            await WriteToLogs((channel as SocketGuildChannel)?.Guild, embed);
        }

        public static async Task Client_RoleCreated(SocketRole role)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Role Created")
                .WithDescription(role.Name);

            await WriteToLogs(role.Guild, embed);
        }

        public static async Task Client_RoleDeleted(SocketRole role)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Role Deleted")
                .WithDescription(role.Name);

            await WriteToLogs(role.Guild, embed);
        }

        public static async Task Client_SpeakerAdded(SocketStageChannel stage, SocketGuildUser user)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(user)
                .WithTitle("Speaker Added to Stage")
                .WithDescription($"<#{stage}>");

            await WriteToLogs(user.Guild, embed);
        }

        public static async Task Client_SpeakerRemoved(SocketStageChannel stage, SocketGuildUser user)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(user)
                .WithTitle("Speaker Removed from Stage")
                .WithDescription($"<#{stage}>");

            await WriteToLogs(user.Guild, embed);
        }

        public static async Task Client_StageEnded(SocketStageChannel stage)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Stage Ended")
                .AddField("Channel", stage)
                .AddField("Topic", stage.Topic);

            await WriteToLogs(stage.Guild, embed);
        }

        public static async Task Client_StageStarted(SocketStageChannel stage)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Stage Started")
                .AddField("Channel", stage)
                .AddField("Topic", stage.Topic);

            await WriteToLogs(stage.Guild, embed);
        }

        public static async Task Client_StageUpdated(SocketStageChannel stageBefore, SocketStageChannel stageAfter)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Stage Updated")
                .WithDescription("*(If nothing here appears changed, then the channel permissions were updated)*")
                .AddField("Previous Channel Name", stageBefore, true)
                .AddField("New Channel Name", stageAfter, true)
                .AddSeparatorField()
                .AddField("Previous Topic", stageBefore.Topic, true)
                .AddField("New Topic", stageAfter.Topic, true)
                .AddSeparatorField()
                .AddField("Previous Discoverability Status", !stageBefore.DiscoverableDisabled, true)
                .AddField("New Discoverability Status", !stageAfter.DiscoverableDisabled, true)
                .AddSeparatorField()
                .AddField("Previous User Limit", stageBefore.UserLimit, true)
                .AddField("New User Limit", stageAfter.UserLimit, true);

            await WriteToLogs(stageAfter.Guild, embed);
        }

        public static async Task Client_ThreadCreated(SocketThreadChannel threadChannel)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Thread Created")
                .AddField("Channel", threadChannel.ParentChannel)
                .AddField("Name", threadChannel);

            await WriteToLogs(threadChannel.Guild, embed);
        }

        public static async Task Client_ThreadDeleted(Cacheable<SocketThreadChannel, ulong> threadChannelCached)
        {
            SocketThreadChannel threadChannel = await threadChannelCached.GetOrDownloadAsync();
            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Thread Deleted")
                .AddField("Channel", threadChannel.ParentChannel)
                .AddField("Name", threadChannel);

            await WriteToLogs(threadChannel.Guild, embed);
        }

        public static async Task Client_ThreadMemberJoined(SocketThreadUser threadUser)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(threadUser)
                .WithTitle("User Joined Thread");

            await WriteToLogs(threadUser.Guild, embed);
        }

        public static async Task Client_ThreadMemberLeft(SocketThreadUser threadUser)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(threadUser)
                .WithTitle("User Left Thread");

            await WriteToLogs(threadUser.Guild, embed);
        }

        public static async Task Client_ThreadUpdated(Cacheable<SocketThreadChannel, ulong> threadBeforeCached, SocketThreadChannel threadAfter)
        {
            SocketThreadChannel threadBefore = await threadBeforeCached.GetOrDownloadAsync();
            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Thread Updated")
                .WithDescription("*(If nothing here appears changed, then the thread permissions were updated)*")
                .AddField("Previous Name", threadBefore, true)
                .AddField("New Name", threadAfter, true)
                .AddSeparatorField()
                .AddField("Previous Lock Status", threadBefore.Locked, true)
                .AddField("New Lock Status", threadAfter.Locked, true)
                .AddSeparatorField()
                .AddField("Previous Member Count", threadBefore.MemberCount, true)
                .AddField("New Member Count", threadAfter.MemberCount, true)
                .AddSeparatorField()
                .AddField("Previous Position", threadBefore.Position, true)
                .AddField("New Position", threadAfter.Position, true);

            await WriteToLogs(threadAfter.Guild, embed);
        }

        public static async Task Client_UserBanned(SocketUser user, SocketGuild guild)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(user)
                .WithTitle("User Banned");

            await WriteToLogs(guild, embed);
        }

        public static async Task Client_UserJoined(SocketGuildUser user)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(user)
                .WithTitle("User Joined");

            await WriteToLogs(user.Guild, embed);
        }

        public static async Task Client_UserLeft(SocketGuildUser user)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(user)
                .WithTitle("User Left");

            await WriteToLogs(user.Guild, embed);
        }

        public static async Task Client_UserUnbanned(SocketUser user, SocketGuild guild)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(user)
                .WithTitle("User Unbanned");

            await WriteToLogs(guild, embed);
        }

        public static async Task Client_UserVoiceStateUpdated(SocketUser user, SocketVoiceState voiceStateOrig, SocketVoiceState voiceState)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(user)
                .WithDescription(voiceState.VoiceChannel == null
                    ? $"{user}\nIn: {voiceStateOrig.VoiceChannel}"
                    : $"{user}\nIn: {voiceState.VoiceChannel}");

            if (voiceStateOrig.VoiceChannel.Id != voiceState.VoiceChannel.Id)
            {
                embed.Title = "User Moved Voice Channels";
                embed.Description = $"{user}\nOriginal: {voiceStateOrig.VoiceChannel}\nCurrent: {voiceState.VoiceChannel}";
            }

            if (voiceStateOrig.VoiceChannel == null)
                embed.Title = "User Joined Voice Channel";
            else if (voiceState.VoiceChannel == null)
                embed.Title = "User Left Voice Channel";
            else if (!voiceStateOrig.IsDeafened && voiceState.IsDeafened)
                embed.Title = "User Server Deafened";
            else if (voiceStateOrig.IsDeafened && !voiceState.IsDeafened)
                embed.Title = "User Server Undeafened";
            else if (!voiceStateOrig.IsMuted && voiceState.IsMuted)
                embed.Title = "User Server Muted";
            else if (voiceStateOrig.IsMuted && !voiceState.IsMuted)
                embed.Title = "User Server Unmuted";
            else if (!voiceStateOrig.IsSelfDeafened && voiceState.IsSelfDeafened)
                embed.Title = "User Self Deafened";
            else if (voiceStateOrig.IsSelfDeafened && !voiceState.IsSelfDeafened)
                embed.Title = "User Self Undeafened";
            else if (!voiceStateOrig.IsSelfMuted && voiceState.IsSelfMuted)
                embed.Title = "User Self Muted";
            else if (voiceStateOrig.IsSelfMuted && !voiceState.IsSelfMuted)
                embed.Title = "User Self Unmuted";
            else
                embed.Title = "User Voice Status Changed";

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

            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle($"{messages.Count() - 1} Messages Purged")
                .WithDescription($"See them [here](https://hastebin.com/{hbKey})");

            await WriteToLogs(guild, embed);
        }

        public static async Task Custom_TrackStarted(SocketGuildUser user, Uri url)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(user)
                .WithTitle("User Started Track")
                .WithDescription(url.ToString());

            await WriteToLogs(user.Guild, embed);
        }

        public static async Task Custom_UserBullied(IGuildUser target, SocketUser actor, string nickname)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(target)
                .WithTitle("User Bullied")
                .WithDescription($"{actor.Mention} bullied {target.Mention} to '{nickname}'");

            await WriteToLogs(target.Guild as SocketGuild, embed);
        }

        public static async Task Custom_UserMuted(IGuildUser target, SocketUser actor, string duration, string reason)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(target)
                .WithTitle("User Muted")
                .AddField("Duration", duration)
                .AddField("Muter", actor)
                .AddField("Reason", reason ?? "None");

            await WriteToLogs(target.Guild as SocketGuild, embed);
        }

        public static async Task Custom_UserUnmuted(IGuildUser target, SocketUser actor)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(target)
                .WithTitle("User Unmuted")
                .WithDescription($"by {actor.Mention}");

            await WriteToLogs(target.Guild as SocketGuild, embed);
        }
    }
}
