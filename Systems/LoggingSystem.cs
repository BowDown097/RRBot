#pragma warning disable RCS1163, IDE0060 // both warnings fire for events, which they shouldn't
namespace RRBot.Systems;
public static class LoggingSystem
{
    private static string DescribeActions(IEnumerable<AutoModRuleAction> actions)
    {
        StringBuilder actionsBuilder = new();

        foreach (AutoModRuleAction action in actions)
        {
            string actionDesc = action.Type switch
            {
                AutoModActionType.BlockMessage => "Block flagged messages",
                AutoModActionType.SendAlertMessage => $"Send an alert containing the flagged message to {MentionUtils.MentionChannel(action.ChannelId ?? 0)}",
                AutoModActionType.Timeout => $"Timeout member for {action.TimeoutDuration?.FormatCompound()}",
                _ => "Unknown Action"
            };

            if (action.CustomMessage.IsSpecified)
                actionDesc += $" and respond with \"{action.CustomMessage}\"";

            actionsBuilder.AppendLine(actionDesc);
        }

        return actionsBuilder.ToString();
    }

    private static async Task WriteToLogs(IGuild guild, EmbedBuilder embed)
    {
        embed.Color = Color.Blue;
        embed.Timestamp = DateTime.Now;
        if (embed.Fields.Count != 0 && embed.Fields.Last().Name == "\u200b")
            embed.Fields.RemoveAt(embed.Fields.Count - 1);
        
        DbConfigChannels channels = await MongoManager.FetchConfigAsync<DbConfigChannels>(guild.Id);
        ITextChannel textChannel = await guild.GetTextChannelAsync(channels.LogsChannel);
        if (textChannel != null)
            await textChannel.SendMessageAsync(embed: embed.Build());
    }

    public static async Task Client_AutoModRuleCreated(SocketAutoModRule rule)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor(rule.Creator)
            .WithDescription("**AutoMod Rule Created**")
            .RrAddField("Name", rule.Name)
            .RrAddField("Enabled", rule.Enabled)
            .RrAddField("Trigger", Enum.GetName(rule.TriggerType).SplitPascalCase())
            .RrAddField("Keywords", string.Join(", ", rule.KeywordFilter))
            .RrAddField("Regex Patterns", string.Join(", ", rule.RegexPatterns.Select(StringCleaner.Sanitize)))
            .RrAddField("Mention Limit", rule.MentionTotalLimit)
            .RrAddField("Presets", string.Join(", ", rule.Presets.Select(p => Enum.GetName(p).SplitPascalCase())))
            .RrAddField("Whitelist", string.Join(", ", rule.AllowList))
            .RrAddField("Exempt Channels", string.Join(", ", rule.ExemptChannels.Select(c => c.Mention())))
            .RrAddField("Exempt Roles", string.Join(", ", rule.ExemptRoles.Select(r => r.Mention)))
            .RrAddField("Actions", DescribeActions(rule.Actions));
        await WriteToLogs(rule.Guild, embed);
    }

    public static async Task Client_AutoModRuleDeleted(SocketAutoModRule rule)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription("**AutoMod Rule Deleted**")
            .AddField("Name", rule.Name);
        await WriteToLogs(rule.Guild, embed);
    }

    public static async Task Client_AutoModRuleUpdated(Cacheable<SocketAutoModRule, ulong> beforeCached,
        SocketAutoModRule after)
    {
        SocketAutoModRule before = await beforeCached.GetOrDownloadAsync();
        string triggerBefore = Enum.GetName(before.TriggerType).SplitPascalCase();
        string triggerAfter = Enum.GetName(after.TriggerType).SplitPascalCase();
        string keywordsBefore = string.Join(", ", before.KeywordFilter);
        string keywordsAfter = string.Join(", ", after.KeywordFilter);
        string patternsBefore = string.Join(", ", before.RegexPatterns.Select(StringCleaner.Sanitize));
        string patternsAfter = string.Join(", ", after.RegexPatterns.Select(StringCleaner.Sanitize));
        string presetsBefore = string.Join(", ", before.Presets.Select(p => Enum.GetName(p).SplitPascalCase()));
        string presetsAfter = string.Join(", ", after.Presets.Select(p => Enum.GetName(p).SplitPascalCase()));
        string whitelistBefore = string.Join(", ", before.AllowList);
        string whitelistAfter = string.Join(", ", after.AllowList);
        string exemptChannelsBefore = string.Join(", ", before.ExemptChannels.Select(c => c.Mention()));
        string exemptChannelsAfter = string.Join(", ", after.ExemptChannels.Select(c => c.Mention()));
        string exemptRolesBefore = string.Join(", ", before.ExemptRoles.Select(r => r.Mention));
        string exemptRolesAfter = string.Join(", ", after.ExemptRoles.Select(r => r.Mention));
        
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription("**AutoMod Rule Updated**")
            .AddUpdateCompField("Name", before.Name, after.Name)
            .AddUpdateCompField("Enabled", before.Enabled, after.Enabled)
            .AddUpdateCompField("Trigger", triggerBefore, triggerAfter)
            .AddUpdateCompField("Keywords", keywordsBefore, keywordsAfter)
            .AddUpdateCompField("Regex Patterns", patternsBefore, patternsAfter)
            .AddUpdateCompField("Mention Limit", before.MentionTotalLimit, after.MentionTotalLimit)
            .AddUpdateCompField("Presets", presetsBefore, presetsAfter)
            .AddUpdateCompField("Whitelist", whitelistBefore, whitelistAfter)
            .AddUpdateCompField("Exempt Channels", exemptChannelsBefore, exemptChannelsAfter)
            .AddUpdateCompField("Exempt Roles", exemptRolesBefore, exemptRolesAfter)
            .AddUpdateCompField("Actions", DescribeActions(before.Actions), DescribeActions(after.Actions));
        await WriteToLogs(after.Guild, embed);
    }

    public static async Task Client_ChannelCreated(SocketChannel channel)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription($"**Channel Created**\n{channel.Mention()}");

        await WriteToLogs(channel.GetGuild(), embed);
    }

    public static async Task Client_ChannelDestroyed(SocketChannel channel)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription($"**Channel Deleted**\n{channel.Mention()}");

        await WriteToLogs(channel.GetGuild(), embed);
    }

    public static async Task Client_ChannelUpdated(SocketChannel before, SocketChannel after)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription($"**Channel Updated**\n*(If nothing here appears changed, then the channel permissions were updated)*\n{after.Mention()}")
            .AddUpdateCompField("Name", before, after)
            .AddUpdateCompField("Member Count", before.Users.Count, after.Users.Count);

        if (before is SocketTextChannel beforeText && after is SocketTextChannel afterText)
        {
            if (beforeText.Position != afterText.Position) // we don't care about position changes those are boring
                return;
            embed.AddUpdateCompField("Topic", beforeText.Topic, afterText.Topic)
                .AddUpdateCompField("Slow Mode Interval", beforeText.SlowModeInterval, afterText.SlowModeInterval);
        }

        await WriteToLogs((before as SocketGuildChannel)?.Guild, embed);
    }

    public static async Task Client_GuildMemberUpdated(Cacheable<SocketGuildUser, ulong> userBeforeCached,
        SocketGuildUser userAfter)
    {
        // this event gets hit a lot, so we need to run it in a separate task so the gateway task doesn't get blocked
        await Task.Run(async () =>
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
        });
    }

    public static async Task Client_GuildScheduledEventCancelled(SocketGuildEvent guildEvent)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription("**Event Cancelled**")
            .RrAddField("Name", guildEvent.Name)
            .RrAddField("Description", guildEvent.Description)
            .RrAddField("Location", guildEvent.Location);

        await WriteToLogs(guildEvent.Guild, embed);
    }

    public static async Task Client_GuildScheduledEventCompleted(SocketGuildEvent guildEvent)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription("**Event Ended**")
            .RrAddField("Name", guildEvent.Name)
            .RrAddField("Description", guildEvent.Description)
            .RrAddField("Location", guildEvent.Location);

        await WriteToLogs(guildEvent.Guild, embed);
    }

    public static async Task Client_GuildScheduledEventCreated(SocketGuildEvent guildEvent)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor(guildEvent.Creator)
            .WithDescription("**Event Created**")
            .RrAddField("Name", guildEvent.Name)
            .RrAddField("Description", guildEvent.Description)
            .RrAddField("Location", guildEvent.Location)
            .RrAddField("Start Time", guildEvent.StartTime)
            .RrAddField("End Time", guildEvent.EndTime);

        await WriteToLogs(guildEvent.Guild, embed);
    }

    public static async Task Client_GuildScheduledEventStarted(SocketGuildEvent guildEvent)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription("**Event Started**")
            .RrAddField("Name", guildEvent.Name)
            .RrAddField("Description", guildEvent.Description)
            .RrAddField("Location", guildEvent.Location);

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
            .RrAddField("Name", sticker.Name)
            .RrAddField("Description", sticker.Description);

        if (sticker.Author != null) // sticker.Author can randomly be null :(
            embed.WithAuthor(sticker.Author);

        await WriteToLogs(sticker.Guild, embed);
    }

    public static async Task Client_GuildStickerDeleted(SocketCustomSticker sticker)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription("**Sticker Deleted**")
            .WithImageUrl(sticker.GetStickerUrl())
            .RrAddField("Name", sticker.Name)
            .RrAddField("Description", sticker.Description);

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
            .RrAddField("URL", invite.Url)
            .RrAddField("Channel", invite.Channel.Mention())
            .RrAddField("Expires At", invite.ExpiresAt != null ? $"<t:{invite.ExpiresAt}>" : "")
            .RrAddField("Max Age", invite.MaxAge)
            .RrAddField("Max Uses", invite.MaxUses);

        await WriteToLogs(invite.Guild, embed);
    }

    public static async Task Client_InviteDeleted(SocketGuildChannel channel, string code)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription("**Invite Deleted**")
            .RrAddField("Channel", channel.Mention())
            .RrAddField("Code", code);

        await WriteToLogs(channel.Guild, embed);
    }

    public static async Task Client_MessageDeleted(Cacheable<IMessage, ulong> msgCached, Cacheable<IMessageChannel, ulong> channelCached)
    {
        IMessage msg = await msgCached.GetOrDownloadAsync();
        IMessageChannel channel = await channelCached.GetOrDownloadAsync();
        if (channel == null || msg == null) // they're not cached sometimes :(
            return;

        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor(msg.Author)
            .WithDescription($"**Message Deleted in {channel.Mention()}**\n{msg.Content}")
            .WithFooter($"ID: {msg.Id}");

        foreach (Embed msgEmbed in msg.Embeds.Where(e => !string.IsNullOrWhiteSpace(e.Title) && !string.IsNullOrWhiteSpace(e.Description)).Cast<Embed>())
        {
            embed.RrAddField("Embed Title", msgEmbed.Title)
                .RrAddField("Embed Description", msgEmbed.Description);
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
            .WithDescription($"**Message Updated in {channel.Mention()}**\n[Jump]({msgAfter.GetJumpUrl()})")
            .RrAddField("Previous Content", msgBefore.Content);

        foreach (Embed msgEmbed in msgBefore.Embeds.Where(e => !string.IsNullOrWhiteSpace(e.Title) && !string.IsNullOrWhiteSpace(e.Description)).Cast<Embed>())
        {
            embed.RrAddField("Embed Title", msgEmbed.Title)
                .RrAddField("Embed Description", msgEmbed.Description);
        }

        embed.RrAddField("Current Content", msgAfter.Content);
        foreach (Embed msgEmbed in msgAfter.Embeds.Where(e => !string.IsNullOrWhiteSpace(e.Title) && !string.IsNullOrWhiteSpace(e.Description)))
        {
            embed.RrAddField("Embed Title", msgEmbed.Title)
                .RrAddField("Embed Description", msgEmbed.Description);
        }

        await WriteToLogs(channel.GetGuild(), embed);
    }

    public static async Task Client_RoleCreated(SocketRole role)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription("**Role Created**")
            .RrAddField("Name", role)
            .RrAddField("Color", $"{role.Color} ({role.Color.R}, {role.Color.G}, {role.Color.B})");

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
            .WithDescription($"**Speaker Added to Stage**\n{stage.Mention}");

        await WriteToLogs(user.Guild, embed);
    }

    public static async Task Client_SpeakerRemoved(SocketStageChannel stage, SocketGuildUser user)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor(user)
            .WithDescription($"**Speaker Removed from Stage**\n{stage.Mention}");

        await WriteToLogs(user.Guild, embed);
    }

    public static async Task Client_StageEnded(SocketStageChannel stage)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription("**Stage Ended**")
            .RrAddField("Channel", stage.Mention)
            .RrAddField("Topic", stage.Topic);

        await WriteToLogs(stage.Guild, embed);
    }

    public static async Task Client_StageStarted(SocketStageChannel stage)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription("**Stage Started**")
            .RrAddField("Channel", stage.Mention)
            .RrAddField("Topic", stage.Topic);

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
            .RrAddField("Channel", threadChannel.ParentChannel.Mention())
            .RrAddField("Name", threadChannel.Name)
            .RrAddField("Owner", threadChannel.Owner.Mention);

        await WriteToLogs(threadChannel.Guild, embed);
    }

    public static async Task Client_ThreadDeleted(Cacheable<SocketThreadChannel, ulong> threadChannelCached)
    {
        SocketThreadChannel threadChannel = await threadChannelCached.GetOrDownloadAsync();
        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription("**Thread Deleted**")
            .RrAddField("Channel", threadChannel.ParentChannel.Mention())
            .RrAddField("Name", threadChannel.Name);

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

    public static async Task Client_UserVoiceStateUpdated(SocketUser user, SocketVoiceState voiceStateOrig, SocketVoiceState voiceState)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor(user)
            .WithDescription(voiceState.VoiceChannel == null
                ? $"{user}\nIn: {voiceStateOrig.VoiceChannel}"
                : $"{user}\nIn: {voiceState.VoiceChannel}");

        if (voiceStateOrig.VoiceChannel == null)
            embed.WithTitle("User Joined Voice Channel");
        else if (voiceState.VoiceChannel == null)
            embed.WithTitle("User Left Voice Channel");
        else if (!voiceStateOrig.IsDeafened && voiceState.IsDeafened)
            embed.WithTitle("User Server Deafened");
        else if (voiceStateOrig.IsDeafened && !voiceState.IsDeafened)
            embed.WithTitle("User Server Undeafened");
        else if (!voiceStateOrig.IsMuted && voiceState.IsMuted)
            embed.WithTitle("User Server Muted");
        else if (voiceStateOrig.IsMuted && !voiceState.IsMuted)
            embed.WithTitle("User Server Unmuted");
        else if (!voiceStateOrig.IsSelfDeafened && voiceState.IsSelfDeafened)
            embed.WithTitle("User Self Deafened");
        else if (voiceStateOrig.IsSelfDeafened && !voiceState.IsSelfDeafened)
            embed.WithTitle("User Self Undeafened");
        else if (!voiceStateOrig.IsSelfMuted && voiceState.IsSelfMuted)
            embed.WithTitle("User Self Muted");
        else if (voiceStateOrig.IsSelfMuted && !voiceState.IsSelfMuted)
            embed.WithTitle("User Self Unmuted");
        else if (!voiceStateOrig.IsStreaming && voiceState.IsStreaming)
            embed.WithTitle("User Started Streaming");
        else if (voiceStateOrig.IsStreaming && !voiceState.IsStreaming)
            embed.WithTitle("User Stopped Streaming");
        else if (voiceStateOrig.VoiceChannel.Id != voiceState.VoiceChannel.Id)
            embed.WithTitle("User Moved Voice Channels").WithDescription($"{user}\nOriginal: {voiceStateOrig.VoiceChannel}\nCurrent: {voiceState.VoiceChannel}");
        else
            embed.WithTitle("User Voice Status Changed");

        await WriteToLogs(user.GetGuild(), embed);
    }

    public static async Task Custom_MessagesPurged(IEnumerable<IMessage> messages, SocketGuild guild)
    {
        StringBuilder msgLogs = new();
        List<IMessage> messageList = messages.ToList();
        foreach (IMessage message in messageList)
            msgLogs.AppendLine($"{message.Author} @ {message.Timestamp}: {message.Content}");

        using HttpClient client = new();
        HttpContent content = new StringContent(msgLogs.ToString());
        HttpResponseMessage response = await client.PostAsync("https://hastebin.com/documents", content);
        string hbKey = JObject.Parse(await response.Content.ReadAsStringAsync())["key"]?.ToString();

        EmbedBuilder embed = new EmbedBuilder()
            .WithDescription($"**{messageList.Count() - 1} Messages Purged**\nSee them [here](https://hastebin.com/{hbKey})");

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
            .RrAddField("Banner", actor);

        await WriteToLogs(target.Guild, embed);
    }

    public static async Task Custom_UserMuted(IGuildUser target, SocketUser actor, string duration, string reason)
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithAuthor(target)
            .WithDescription("**User Muted**")
            .RrAddField("Duration", duration)
            .RrAddField("Muter", actor)
            .RrAddField("Reason", reason);

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