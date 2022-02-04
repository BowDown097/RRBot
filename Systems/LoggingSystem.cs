#pragma warning disable RCS1163, IDE0060 // both warnings fire for events, which they shouldn't
namespace RRBot.Systems;
public static class LoggingSystem
{
    private static async Task WriteToLogs(IGuild guild, EmbedBuilder embed)
    {
        embed.Color = Color.Blue;
        embed.Timestamp = DateTime.Now;
        if (embed.Fields.Count != 0 && embed.Fields.Last().Name == "\u200b")
            embed.Fields.RemoveAt(embed.Fields.Count - 1);

        DbConfigChannels channels = await DbConfigChannels.GetById(guild.Id);
        ITextChannel textChannel = await guild.GetTextChannelAsync(channels.LogsChannel);
        if (textChannel != null)
            await textChannel.SendMessageAsync(embed: embed.Build());
    }

    public static async Task Client_ChannelCreated(SocketChannel channel)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription($"**Channel Created**\n{MentionUtils.MentionChannel(channel.Id)}");

        await WriteToLogs(channel.GetGuild(), embed);
    }

    public static async Task Client_ChannelDestroyed(SocketChannel channel)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription($"**Channel Deleted**\n{MentionUtils.MentionChannel(channel.Id)}");

        await WriteToLogs(channel.GetGuild(), embed);
    }

    public static async Task Client_ChannelUpdated(SocketChannel before, SocketChannel after)
    {
        SocketTextChannel beforeText = before as SocketTextChannel;
        SocketTextChannel afterText = after as SocketTextChannel;
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription($"**Channel Updated**\n*(If nothing here appears changed, then the channel permissions were updated)*\n{MentionUtils.MentionChannel(after.Id)}")
            .AddUpdateCompField("Name", before, after)
            .AddUpdateCompField("Topic", beforeText.Topic, afterText.Topic)
            .AddUpdateCompField("Position", beforeText.Position, afterText.Position)
            .AddUpdateCompField("Member Count", before.Users.Count, after.Users.Count);

        await WriteToLogs(beforeText.Guild, embed);
    }

    public static async Task Client_GuildMemberUpdated(Cacheable<SocketGuildUser, ulong> userBeforeCached,
        SocketGuildUser userAfter)
    {
        SocketGuildUser userBefore = await userBeforeCached.GetOrDownloadAsync();
        if (userBefore.Nickname == userAfter.Nickname && userBefore.Roles.SequenceEqual(userAfter.Roles))
            return;

        string rolesBefore = string.Join(" ", userBefore.Roles.Select(r => r.Mention));
        string rolesAfter = string.Join(" ", userAfter.Roles.Select(r => r.Mention));
        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor(userAfter)
            .WithDescription($"**Member Updated**\n{userAfter.Mention}")
            .AddUpdateCompField("Nickname", userBefore.Nickname, userAfter.Nickname)
            .AddUpdateCompField("Roles", rolesBefore, rolesAfter);

        await WriteToLogs(userAfter.Guild, embed);
    }

    public static async Task Client_GuildScheduledEventCancelled(SocketGuildEvent guildEvent)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription("**Event Cancelled**")
            .RRAddField("Name", guildEvent.Name)
            .RRAddField("Description", guildEvent.Description)
            .RRAddField("Location", guildEvent.Location);

        await WriteToLogs(guildEvent.Guild, embed);
    }

    public static async Task Client_GuildScheduledEventCompleted(SocketGuildEvent guildEvent)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription("**Event Ended**")
            .RRAddField("Name", guildEvent.Name)
            .RRAddField("Description", guildEvent.Description)
            .RRAddField("Location", guildEvent.Location);

        await WriteToLogs(guildEvent.Guild, embed);
    }

    public static async Task Client_GuildScheduledEventCreated(SocketGuildEvent guildEvent)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor(guildEvent.Creator)
            .WithDescription("**Event Created**")
            .RRAddField("Name", guildEvent.Name)
            .RRAddField("Description", guildEvent.Description)
            .RRAddField("Location", guildEvent.Location)
            .RRAddField("Start Time", guildEvent.StartTime)
            .RRAddField("End Time", guildEvent.EndTime);

        await WriteToLogs(guildEvent.Guild, embed);
    }

    public static async Task Client_GuildScheduledEventStarted(SocketGuildEvent guildEvent)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription("**Event Started**")
            .RRAddField("Name", guildEvent.Name)
            .RRAddField("Description", guildEvent.Description)
            .RRAddField("Location", guildEvent.Location);

        await WriteToLogs(guildEvent.Guild, embed);
    }

    public static async Task Client_GuildScheduledEventUpdated(Cacheable<SocketGuildEvent, ulong> guildEventBeforeCached,
        SocketGuildEvent guildEventAfter)
    {
        SocketGuildEvent guildEventBefore = await guildEventBeforeCached.GetOrDownloadAsync();
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription($"**Event Updated**\n{guildEventAfter.Name}")
            .AddUpdateCompField("Name", guildEventBefore.Name, guildEventAfter.Name)
            .AddUpdateCompField("Description", guildEventBefore.Description, guildEventAfter.Description)
            .AddUpdateCompField("Location", guildEventBefore.Location, guildEventAfter.Location)
            .AddUpdateCompField("Start Time", guildEventBefore.StartTime, guildEventAfter.StartTime)
            .AddUpdateCompField("End Time", guildEventBefore.EndTime, guildEventAfter.EndTime);

        await WriteToLogs(guildEventAfter.Guild, embed);
    }

    public static async Task Client_GuildScheduledEventUserAdd(Cacheable<SocketUser, RestUser, IUser, ulong> userCached,
        SocketGuildEvent guildEvent)
    {
        IUser user = await userCached.GetOrDownloadAsync();
        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor(user)
            .WithDescription($"**User Joined Event**\n{guildEvent.Name}");

        await WriteToLogs(guildEvent.Guild, embed);
    }

    public static async Task Client_GuildScheduledEventUserRemove(Cacheable<SocketUser, RestUser, IUser, ulong> userCached,
        SocketGuildEvent guildEvent)
    {
        IUser user = await userCached.GetOrDownloadAsync();
        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor(user)
            .WithDescription($"**User Left Event**\n{guildEvent.Name}");

        await WriteToLogs(guildEvent.Guild, embed);
    }

    public static async Task Client_GuildStickerCreated(SocketCustomSticker sticker)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription("**Sticker Created**")
            .WithImageUrl(sticker.GetStickerUrl())
            .RRAddField("Name", sticker.Name)
            .RRAddField("Description", sticker.Description);

        if (sticker.Author != null) // sticker.Author can randomly be null :(
            embed.WithAuthor(sticker.Author);

        await WriteToLogs(sticker.Guild, embed);
    }

    public static async Task Client_GuildStickerDeleted(SocketCustomSticker sticker)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription("**Sticker Deleted**")
            .WithImageUrl(sticker.GetStickerUrl())
            .RRAddField("Name", sticker.Name)
            .RRAddField("Description", sticker.Description);

        if (sticker.Author != null) // sticker.Author can randomly be null :(
            embed.WithAuthor(sticker.Author);

        await WriteToLogs(sticker.Guild, embed);
    }

    public static async Task Client_GuildUpdated(SocketGuild guildBefore, SocketGuild guildAfter)
    {
        if (guildBefore.Name == guildAfter.Name && guildBefore.Description == guildAfter.Description)
            return;

        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription("**Guild Updated**")
            .AddUpdateCompField("Name", guildBefore.Name, guildAfter.Name)
            .AddUpdateCompField("Description", guildBefore.Description, guildAfter.Description);

        await WriteToLogs(guildAfter, embed);
    }

    public static async Task Client_InviteCreated(SocketInvite invite)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor(invite.Inviter)
            .WithDescription("**Invite Created**")
            .RRAddField("URL", invite.Url)
            .RRAddField("Channel", MentionUtils.MentionChannel(invite.ChannelId))
            .RRAddField("Max Age", invite.MaxAge)
            .RRAddField("Max Uses", invite.MaxUses);

        await WriteToLogs(invite.Guild, embed);
    }

    public static async Task Client_InviteDeleted(SocketGuildChannel channel, string code)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription("**Invite Deleted**")
            .RRAddField("Channel", MentionUtils.MentionChannel(channel.Id))
            .RRAddField("Code", code);

        await WriteToLogs(channel.Guild, embed);
    }

    public static async Task Client_MessageDeleted(Cacheable<IMessage, ulong> msgCached, Cacheable<IMessageChannel, ulong> channelCached)
    {
        IMessage msg = await msgCached.GetOrDownloadAsync();
        IMessageChannel channel = await channelCached.GetOrDownloadAsync();
        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor(msg.Author)
            .WithDescription($"**Message Deleted in {MentionUtils.MentionChannel(channel.Id)}**\n{msg.Content}")
            .WithFooter($"ID: {msg.Id}");

        foreach (Embed msgEmbed in msg.Embeds.Where(e => !string.IsNullOrWhiteSpace(e.Title) && !string.IsNullOrWhiteSpace(e.Description)))
        {
            embed.RRAddField("Embed Title", msgEmbed.Title)
                .RRAddField("Embed Description", msgEmbed.Description);
        }

        await WriteToLogs(channel.GetGuild(), embed);
    }

    public static async Task Client_MessageUpdated(Cacheable<IMessage, ulong> msgBeforeCached, SocketMessage msgAfter, ISocketMessageChannel channel)
    {
        IMessage msgBefore = await msgBeforeCached.GetOrDownloadAsync();
        if (msgBefore.Content == msgAfter.Content)
            return;

        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor(msgAfter.Author)
            .WithDescription($"**Message Updated in {MentionUtils.MentionChannel(channel.Id)}**\n[Jump]({msgAfter.GetJumpUrl()})")
            .RRAddField("Previous Content", msgBefore.Content);

        foreach (Embed msgEmbed in msgBefore.Embeds.Where(e => !string.IsNullOrWhiteSpace(e.Title) && !string.IsNullOrWhiteSpace(e.Description)))
        {
            embed.RRAddField("Embed Title", msgEmbed.Title)
                .RRAddField("Embed Description", msgEmbed.Description);
        }

        embed.RRAddField("Current Content", msgAfter.Content);
        foreach (Embed msgEmbed in msgAfter.Embeds.Where(e => !string.IsNullOrWhiteSpace(e.Title) && !string.IsNullOrWhiteSpace(e.Description)))
        {
            embed.RRAddField("Embed Title", msgEmbed.Title)
                .RRAddField("Embed Description", msgEmbed.Description);
        }

        await WriteToLogs(channel.GetGuild(), embed);
    }

    public static async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> msgCached,
        Cacheable<IMessageChannel, ulong> channelCached, SocketReaction reaction)
    {
        IUserMessage msg = await msgCached.GetOrDownloadAsync();

        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor(reaction.User.Value)
            .WithDescription($"**Reaction Added in {MentionUtils.MentionChannel(reaction.Channel.Id)}**")
            .RRAddField("Emoji", reaction.Emote.Name)
            .RRAddField("Message", $"[Jump]({msg.GetJumpUrl()})");

        if (reaction.Emote is Emote emote)
            embed.WithImageUrl(emote.Url + "?size=40");

        await WriteToLogs(reaction.Channel.GetGuild(), embed);
    }

    public static async Task Client_ReactionRemoved(Cacheable<IUserMessage, ulong> msgCached,
        Cacheable<IMessageChannel, ulong> channelCached, SocketReaction reaction)
    {
        IUserMessage msg = await msgCached.GetOrDownloadAsync();

        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor(reaction.User.Value)
            .WithDescription($"**Reaction Removed in {MentionUtils.MentionChannel(reaction.Channel.Id)}**")
            .RRAddField("Emoji", reaction.Emote.Name)
            .RRAddField("Message", $"[Jump]({msg.GetJumpUrl()})");

        if (reaction.Emote is Emote emote)
            embed.WithImageUrl(emote.Url + "?size=40");

        await WriteToLogs(reaction.Channel.GetGuild(), embed);
    }

    public static async Task Client_RoleCreated(SocketRole role)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription("**Role Created**")
            .RRAddField("Name", role)
            .RRAddField("Color", $"{role.Color} ({role.Color.R}, {role.Color.G}, {role.Color.B})");

        await WriteToLogs(role.Guild, embed);
    }

    public static async Task Client_RoleDeleted(SocketRole role)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription($"**Role Deleted**\n{role}");

        await WriteToLogs(role.Guild, embed);
    }

    public static async Task Client_RoleUpdated(SocketRole roleBefore, SocketRole roleAfter)
    {
        if (roleBefore.Name == roleAfter.Name && roleBefore.Color == roleAfter.Color)
            return;

        string colorBefore = $"{roleBefore.Color} ({roleBefore.Color.R}, {roleBefore.Color.G}, {roleBefore.Color.B})";
        string colorAfter = $"{roleAfter.Color} ({roleAfter.Color.R}, {roleAfter.Color.G}, {roleAfter.Color.B})";
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription($"**Role Updated**\n{roleAfter.Mention}")
            .AddUpdateCompField("Name", roleBefore, roleAfter)
            .AddUpdateCompField("Color", colorBefore, colorAfter);

        await WriteToLogs(roleAfter.Guild, embed);
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
            .RRAddField("Channel", MentionUtils.MentionChannel(stage.Id))
            .RRAddField("Topic", stage.Topic);

        await WriteToLogs(stage.Guild, embed);
    }

    public static async Task Client_StageStarted(SocketStageChannel stage)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription("**Stage Started**")
            .RRAddField("Channel", MentionUtils.MentionChannel(stage.Id))
            .RRAddField("Topic", stage.Topic);

        await WriteToLogs(stage.Guild, embed);
    }

    public static async Task Client_StageUpdated(SocketStageChannel stageBefore, SocketStageChannel stageAfter)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription($"**Stage Updated**\n*(If nothing here appears changed, then the channel permissions were updated)*\n{stageAfter}")
            .AddUpdateCompField("Channel Name", stageBefore, stageAfter)
            .AddUpdateCompField("Topic", stageBefore.Topic, stageAfter.Topic)
            .AddUpdateCompField("Discoverability Status", stageBefore.IsDiscoverableDisabled, stageAfter.IsDiscoverableDisabled)
            .AddUpdateCompField("User Limit", stageBefore.UserLimit, stageAfter.UserLimit);

        await WriteToLogs(stageAfter.Guild, embed);
    }

    public static async Task Client_ThreadCreated(SocketThreadChannel threadChannel)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription("**Thread Created**")
            .RRAddField("Channel", MentionUtils.MentionChannel(threadChannel.ParentChannel.Id))
            .RRAddField("Name", threadChannel.Name);

        await WriteToLogs(threadChannel.Guild, embed);
    }

    public static async Task Client_ThreadDeleted(Cacheable<SocketThreadChannel, ulong> threadChannelCached)
    {
        SocketThreadChannel threadChannel = await threadChannelCached.GetOrDownloadAsync();
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription("**Thread Deleted**")
            .RRAddField("Channel", MentionUtils.MentionChannel(threadChannel.ParentChannel.Id))
            .RRAddField("Name", threadChannel.Name);

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
            .WithDescription($"**User Left Thread**\n{threadUser.Thread}");

        await WriteToLogs(threadUser.Guild, embed);
    }

    public static async Task Client_ThreadUpdated(Cacheable<SocketThreadChannel, ulong> threadBeforeCached, SocketThreadChannel threadAfter)
    {
        SocketThreadChannel threadBefore = await threadBeforeCached.GetOrDownloadAsync();
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription($"**Thread Updated**\n*(If nothing here appears changed, then the thread permissions were updated)*{threadAfter}")
            .AddUpdateCompField("Name", threadBefore, threadAfter)
            .AddUpdateCompField("Lock Status", threadBefore.IsLocked, threadAfter.IsLocked)
            .AddUpdateCompField("Member Count", threadBefore.MemberCount, threadAfter.MemberCount)
            .AddUpdateCompField("Position", threadBefore.Position, threadAfter.Position);

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

    public static async Task Client_UserLeft(SocketGuild guild, SocketUser user)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor(user)
            .WithDescription("**User Left**");

        await WriteToLogs(guild, embed);
    }

    public static async Task Client_UserUnbanned(SocketUser user, SocketGuild guild)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor(user)
            .WithDescription("**User Unbanned**");

        await WriteToLogs(guild, embed);
    }

    public static async Task Custom_MessagesPurged(IEnumerable<IMessage> messages, SocketGuild guild)
    {
        StringBuilder msgLogs = new();
        foreach (IMessage message in messages)
            msgLogs.AppendLine($"{message.Author} @ {message.Timestamp}: {message.Content}");

        using HttpClient client = new();
        HttpContent content = new StringContent(msgLogs.ToString());
        HttpResponseMessage response = await client.PostAsync("https://hastebin.com/documents", content);
        string hbKey = JObject.Parse(await response.Content.ReadAsStringAsync())["key"].ToString();

        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription($"**{messages.Count() - 1} Messages Purged**\nSee them [here](https://hastebin.com/{hbKey})");

        await WriteToLogs(guild, embed);
    }

    public static async Task Custom_TrackStarted(SocketGuildUser user, string url)
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

        await WriteToLogs(target.Guild, embed);
    }

    public static async Task Custom_UserMemeBanned(IGuildUser target, SocketUser actor)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor(target)
            .WithDescription("**User Meme Banned**\nL rip bozo")
            .RRAddField("Banner", actor);

        await WriteToLogs(target.Guild, embed);
    }

    public static async Task Custom_UserMuted(IGuildUser target, SocketUser actor, string duration, string reason)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor(target)
            .WithDescription("**User Muted**")
            .RRAddField("Duration", duration)
            .RRAddField("Muter", actor)
            .RRAddField("Reason", reason);

        await WriteToLogs(target.Guild, embed);
    }

    public static async Task Custom_UserUnmuted(IGuildUser target, SocketUser actor)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor(target)
            .WithDescription($"**User Unmuted**\nby {actor.Mention}");

        await WriteToLogs(target.Guild, embed);
    }
}
