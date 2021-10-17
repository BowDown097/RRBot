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
                .WithDescription($"**Channel Created**\n{MentionUtils.MentionChannel(channel.Id)}");

            await WriteToLogs((channel as SocketGuildChannel)?.Guild, embed);
        }

        public static async Task Client_ChannelDestroyed(SocketChannel channel)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithDescription($"**Channel Deleted**\n{MentionUtils.MentionChannel(channel.Id)}");

            await WriteToLogs((channel as SocketGuildChannel)?.Guild, embed);
        }

        public static async Task Client_ChannelUpdated(SocketChannel before, SocketChannel after)
        {
            SocketTextChannel beforeText = before as SocketTextChannel;
            SocketTextChannel afterText = after as SocketTextChannel;
            EmbedBuilder embed = new EmbedBuilder()
                .WithDescription("**Channel Updated**\n*(If nothing here appears changed, then the channel permissions were updated)*")
                .AddStringField("Previous Name", $"#{before}", true)
                .AddStringField("Current Name", MentionUtils.MentionChannel(after.Id), true)
                .AddSeparatorField()
                .AddStringField("Previous Topic", beforeText.Topic, true)
                .AddStringField("Current Topic", afterText.Topic, true)
                .AddSeparatorField()
                .AddField("Previous Position", beforeText.Position, true)
                .AddField("Current Position", afterText.Position, true)
                .AddSeparatorField()
                .AddField("Previous Member Count", before.Users.Count, true)
                .AddField("Current Member Count", after.Users.Count, true);

            await WriteToLogs(beforeText.Guild, embed);
        }

        public static async Task Client_InviteCreated(SocketInvite invite)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithDescription("**Invite Created**")
                .AddStringField("URL", invite.Url)
                .AddStringField("Channel", MentionUtils.MentionChannel(invite.ChannelId))
                .AddStringField("Inviter", invite.Inviter.Mention)
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
                .WithDescription($"**Message Deleted in {MentionUtils.MentionChannel(channel.Id)}**\n{msg.Content}")
                .WithFooter($"ID: {msg.Id}");

            foreach (Embed msgEmbed in msg.Embeds)
            {
                embed.AddStringField("Embed Title", msgEmbed.Title)
                    .AddStringField("Embed Description", msgEmbed.Description)
                    .AddSeparatorField();
            }

            await WriteToLogs(channel.Guild, embed);
        }

        public static async Task Client_MessageUpdated(Cacheable<IMessage, ulong> msgBeforeCached, SocketMessage msgAfter, ISocketMessageChannel channel)
        {
            IMessage msgBefore = await msgBeforeCached.GetOrDownloadAsync();
            if (msgBefore.Content == msgAfter.Content)
                return;

            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(msgAfter.Author)
                .WithDescription($"**Message Updated in {MentionUtils.MentionChannel(channel.Id)}**")
                .AddStringField("Previous Content", msgBefore.Content);

            foreach (Embed msgEmbed in msgBefore.Embeds)
            {
                embed.AddStringField("Embed Title", msgEmbed.Title)
                    .AddStringField("Embed Description", msgEmbed.Description)
                    .AddSeparatorField();
            }

            embed.AddStringField("Current Content", msgAfter.Content);
            foreach (Embed msgEmbed in msgAfter.Embeds)
            {
                embed.AddStringField("Embed Title", msgEmbed.Title)
                    .AddStringField("Embed Description", msgEmbed.Description)
                    .AddSeparatorField();
            }

            await WriteToLogs((channel as SocketGuildChannel)?.Guild, embed);
        }

        public static async Task Client_RoleCreated(SocketRole role)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithDescription($"**Role Created**\n{role.Name}");

            await WriteToLogs(role.Guild, embed);
        }

        public static async Task Client_RoleDeleted(SocketRole role)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithDescription($"**Role Deleted**\n{role.Name}");

            await WriteToLogs(role.Guild, embed);
        }

        public static async Task Client_SpeakerAdded(SocketStageChannel stage, SocketGuildUser user)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(user)
                .WithDescription($"**Speaker Added to Stage**\n{MentionUtils.MentionChannel(stage.Id)}");

            await WriteToLogs(user.Guild, embed);
        }

        public static async Task Client_SpeakerRemoved(SocketStageChannel stage, SocketGuildUser user)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(user)
                .WithDescription($"**Speaker Removed from Stage**\n{MentionUtils.MentionChannel(stage.Id)}");

            await WriteToLogs(user.Guild, embed);
        }

        public static async Task Client_StageEnded(SocketStageChannel stage)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithDescription("**Stage Ended**")
                .AddStringField("Channel", MentionUtils.MentionChannel(stage.Id))
                .AddStringField("Topic", stage.Topic);

            await WriteToLogs(stage.Guild, embed);
        }

        public static async Task Client_StageStarted(SocketStageChannel stage)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithDescription("**Stage Started**")
                .AddStringField("Channel", MentionUtils.MentionChannel(stage.Id))
                .AddStringField("Topic", stage.Topic);

            await WriteToLogs(stage.Guild, embed);
        }

        public static async Task Client_StageUpdated(SocketStageChannel stageBefore, SocketStageChannel stageAfter)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithDescription("**Stage Updated**\n*(If nothing here appears changed, then the channel permissions were updated)*")
                .AddStringField("Previous Channel Name", $"#{stageBefore}", true)
                .AddStringField("Current Channel Name", MentionUtils.MentionChannel(stageAfter.Id), true)
                .AddSeparatorField()
                .AddStringField("Previous Topic", stageBefore.Topic, true)
                .AddStringField("Current Topic", stageAfter.Topic, true)
                .AddSeparatorField()
                .AddField("Previous Discoverability Status", !stageBefore.DiscoverableDisabled, true)
                .AddField("Current Discoverability Status", !stageAfter.DiscoverableDisabled, true)
                .AddSeparatorField()
                .AddField("Previous User Limit", stageBefore.UserLimit, true)
                .AddField("Current User Limit", stageAfter.UserLimit, true);

            await WriteToLogs(stageAfter.Guild, embed);
        }

        public static async Task Client_ThreadCreated(SocketThreadChannel threadChannel)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithDescription("**Thread Created**")
                .AddStringField("Channel", MentionUtils.MentionChannel(threadChannel.ParentChannel.Id))
                .AddStringField("Name", threadChannel.Name);

            await WriteToLogs(threadChannel.Guild, embed);
        }

        public static async Task Client_ThreadDeleted(Cacheable<SocketThreadChannel, ulong> threadChannelCached)
        {
            SocketThreadChannel threadChannel = await threadChannelCached.GetOrDownloadAsync();
            EmbedBuilder embed = new EmbedBuilder()
                .WithDescription("**Thread Deleted**")
                .AddStringField("Channel", MentionUtils.MentionChannel(threadChannel.ParentChannel.Id))
                .AddStringField("Name", threadChannel.Name);

            await WriteToLogs(threadChannel.Guild, embed);
        }

        public static async Task Client_ThreadMemberJoined(SocketThreadUser threadUser)
        {
            if (threadUser.IsBot)
                return;

            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(threadUser)
                .WithDescription($"**User Joined Thread**\n{threadUser.Thread}");

            await WriteToLogs(threadUser.Guild, embed);
        }

        public static async Task Client_ThreadMemberLeft(SocketThreadUser threadUser)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(threadUser)
                .WithTitle($"**User Left Thread**\n{threadUser.Thread}");

            await WriteToLogs(threadUser.Guild, embed);
        }

        public static async Task Client_ThreadUpdated(Cacheable<SocketThreadChannel, ulong> threadBeforeCached, SocketThreadChannel threadAfter)
        {
            SocketThreadChannel threadBefore = await threadBeforeCached.GetOrDownloadAsync();
            EmbedBuilder embed = new EmbedBuilder()
                .WithDescription("**Thread Updated**\n*(If nothing here appears changed, then the thread permissions were updated)*")
                .AddStringField("Previous Name", threadBefore.Name, true)
                .AddStringField("Current Name", threadAfter.Name, true)
                .AddSeparatorField()
                .AddField("Previous Lock Status", threadBefore.Locked, true)
                .AddField("Current Lock Status", threadAfter.Locked, true)
                .AddSeparatorField()
                .AddField("Previous Member Count", threadBefore.MemberCount, true)
                .AddField("Current Member Count", threadAfter.MemberCount, true)
                .AddSeparatorField()
                .AddField("Previous Position", threadBefore.Position, true)
                .AddField("Current Position", threadAfter.Position, true);

            await WriteToLogs(threadAfter.Guild, embed);
        }

        public static async Task Client_UserBanned(SocketUser user, SocketGuild guild)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(user)
                .WithDescription("**User Banned**");

            await WriteToLogs(guild, embed);
        }

        public static async Task Client_UserJoined(SocketGuildUser user)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(user)
                .WithDescription("**User Joined**");

            await WriteToLogs(user.Guild, embed);
        }

        public static async Task Client_UserLeft(SocketGuildUser user)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(user)
                .WithDescription("**User Left**");

            await WriteToLogs(user.Guild, embed);
        }

        public static async Task Client_UserUnbanned(SocketUser user, SocketGuild guild)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(user)
                .WithDescription("**User Unbanned**");

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
                .WithDescription($"**{messages.Count() - 1} Messages Purged**\nSee them [here](https://hastebin.com/{hbKey})");

            await WriteToLogs(guild, embed);
        }

        public static async Task Custom_TrackStarted(SocketGuildUser user, Uri url)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(user)
                .WithDescription($"**User Started Track**\n{url}");

            await WriteToLogs(user.Guild, embed);
        }

        public static async Task Custom_UserBullied(IGuildUser target, SocketUser actor, string nickname)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(target)
                .WithDescription($"**User Bullied**\n{actor.Mention} bullied {target.Mention} to \"{nickname}\"");

            await WriteToLogs(target.Guild as SocketGuild, embed);
        }

        public static async Task Custom_UserMuted(IGuildUser target, SocketUser actor, string duration, string reason)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(target)
                .WithDescription("**User Muted**")
                .AddField("Duration", duration)
                .AddField("Muter", actor)
                .AddStringField("Reason", reason);

            await WriteToLogs(target.Guild as SocketGuild, embed);
        }

        public static async Task Custom_UserUnmuted(IGuildUser target, SocketUser actor)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(target)
                .WithDescription($"**User Unmuted**\nby {actor.Mention}");

            await WriteToLogs(target.Guild as SocketGuild, embed);
        }
    }
}
