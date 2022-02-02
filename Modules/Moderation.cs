namespace RRBot.Modules;
[Summary("Impose an Orwellian life on the poor normies in chat, through bans, kicks, mutes, you name it.")]
[RequireStaff]
public class Moderation : ModuleBase<SocketCommandContext>
{
    [Alias("seethe")]
    [Command("ban")]
    [Summary("Ban any member.")]
    [Remarks("$ban [user] <duration> <reason>")]
    public async Task<RuntimeResult> Ban(IGuildUser user, string duration = "", [Remainder] string reason = "")
    {
        DbConfigRoles roles = await DbConfigRoles.GetById(Context.Guild.Id);
        if (user.RoleIds.Contains(roles.StaffLvl1Role) || user.RoleIds.Contains(roles.StaffLvl2Role) || user.IsBot)
            return CommandResult.FromError($"You cannot ban **{user.Sanitize()}** because they are a staff member.");

        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        if (int.TryParse(Regex.Match(duration, @"\d+").Value, out int time))
        {
            Tuple<TimeSpan, string> resolved = ResolveDuration(duration, time, $"Banned **{user.Sanitize()}**", reason);
            if (resolved.Item1 == TimeSpan.Zero)
                return CommandResult.FromError("You specified an invalid amount of time!");

            DbBan ban = await DbBan.GetById(Context.Guild.Id, user.Id);
            ban.Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(resolved.Item1.TotalSeconds);
            await user.BanAsync(reason: reason);
            await Context.User.NotifyAsync(Context.Channel, resolved.Item2);
        }
        else
        {
            await user.BanAsync();
            await Context.User.NotifyAsync(Context.Channel, $"Banned **{user.Sanitize()}**.");
        }

        dbUser.AddToStat("Bans", "1");
        return CommandResult.FromSuccess();
    }

    [Command("cancelticket")]
    [Summary("Pre-emptively cancel a user's support ticket, if they have one that is opened.")]
    [Remarks("$cancelticket [user]")]
    [RequireBeInChannel("help-requests")]
    [RequireRushReborn]
    public async Task<RuntimeResult> CancelTicket(IGuildUser user)
    {
        DbSupportTicket ticket = await DbSupportTicket.GetById(Context.Guild.Id, user.Id);
        if (string.IsNullOrWhiteSpace(ticket.Request))
            return CommandResult.FromError("That user does not have an open support ticket!");
        return await Support.CloseTicket(Context, Context.User, ticket, $"Support ticket by {user.Sanitize()} deleted successfully!");
    }

    [Command("chill")]
    [Summary("Shut chat the fuck up for a specific amount of time.")]
    [Remarks("$chill [duration]")]
    public async Task<RuntimeResult> Chill(string duration)
    {
        if (!int.TryParse(Regex.Match(duration, @"\d+").Value, out int time))
            return CommandResult.FromError("You specified an invalid amount of time!");

        Tuple<TimeSpan, string> resolved = ResolveDuration(duration, time, "Chilled the chat", "");
        if (resolved.Item1 == TimeSpan.Zero)
            return CommandResult.FromError("You specified an invalid amount of time!");
        if (resolved.Item1.TotalSeconds < Constants.CHILL_MIN_SECONDS)
            return CommandResult.FromError($"You cannot chill the chat for less than {Constants.CHILL_MIN_SECONDS} seconds.");
        if (resolved.Item1.TotalSeconds > Constants.CHILL_MAX_SECONDS)
            return CommandResult.FromError($"You cannot chill the chat for more than {Constants.CHILL_MAX_SECONDS} seconds.");

        SocketTextChannel channel = Context.Channel as SocketTextChannel;
        OverwritePermissions perms = channel.GetPermissionOverwrite(Context.Guild.EveryoneRole) ?? OverwritePermissions.InheritAll;
        if (perms.SendMessages == PermValue.Deny)
            return CommandResult.FromError("This chat is already chilled.");

        await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, perms.Modify(sendMessages: PermValue.Deny));
        DbChill chill = await DbChill.GetById(Context.Guild.Id, Context.Channel.Id);
        chill.Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(resolved.Item1.TotalSeconds);

        await Context.User.NotifyAsync(Context.Channel, resolved.Item2);
        return CommandResult.FromSuccess();
    }

    [Alias("cope")]
    [Command("kick")]
    [Summary("Kick any member.")]
    [Remarks("$kick [user] <reason>")]
    public async Task<RuntimeResult> Kick(IGuildUser user, [Remainder] string reason = "")
    {
        if (user.IsBot)
            return CommandResult.FromError("Nope.");

        DbConfigRoles roles = await DbConfigRoles.GetById(Context.Guild.Id);
        if (user.RoleIds.Contains(roles.StaffLvl1Role) || user.RoleIds.Contains(roles.StaffLvl2Role))
            return CommandResult.FromError($"You cannot kick **{user.Sanitize()}** because they are a staff member.");

        await user.KickAsync(reason);
        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        dbUser.AddToStat("Kicks", "1");

        string response = $"Kicked **{user.Sanitize()}**";
        if (!string.IsNullOrWhiteSpace(reason))
            response += $"for \"{reason}\"";
        await Context.User.NotifyAsync(Context.Channel, response + ".");
        return CommandResult.FromSuccess();
    }

    [Command("memeban", RunMode = RunMode.Async)]
    [Summary("Meme bans a member and DMs an invite back to the server.")]
    [Remarks("$memeban [user]")]
    public async Task<RuntimeResult> MemeBan(IGuildUser user)
    {
        if (user.IsBot)
            return CommandResult.FromError("Nope.");

        DbConfigRoles roles = await DbConfigRoles.GetById(Context.Guild.Id);
        if (user.RoleIds.Contains(roles.StaffLvl1Role) || user.RoleIds.Contains(roles.StaffLvl2Role))
            return CommandResult.FromError($"You cannot meme ban **{user.Sanitize()}** because they are a staff member.");

        IInviteMetadata invite = await Context.Guild.DefaultChannel.CreateInviteAsync(null, 1);
        try
        {
            IDMChannel dm = await user.CreateDMChannelAsync();
            await dm.SendMessageAsync($"You got meme banned by {Context.User} LMAO! Here's an invite back to the server: {invite.Url}");
            await dm.SendMessageAsync("https://tenor.com/view/rip-pack-bozo-dead-gif-20309754");
        }
        catch (HttpException ex) when (ex.HttpCode == HttpStatusCode.Forbidden)
        {
            await user.NotifyAsync(Context.Channel, $"I couldn't DM you, so I hope this reaches you! You're getting MEME BANNED in 30 seconds lmao. If you aren't able to snag an invite, here's one: {invite.Url}");
            await Task.Delay(TimeSpan.FromSeconds(30));
        }

        await user.KickAsync();
        await LoggingSystem.Custom_UserMemeBanned(user, Context.User);
        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        dbUser.AddToStat("Meme Bans", "1");

        await Context.User.NotifyAsync(Context.Channel, $"Meme banned **{user.Sanitize()}**!");
        await ReplyAsync("https://tenor.com/view/rip-pack-bozo-dead-gif-20309754");
        return CommandResult.FromSuccess();
    }

    [Alias("1984")]
    [Command("mute")]
    [Summary("Mute any member for any amount of time with any reason.")]
    [Remarks("$mute [user] [duration] <reason>")]
    public async Task<RuntimeResult> Mute(IGuildUser user, string duration, [Remainder] string reason = "")
    {
        if (!int.TryParse(Regex.Match(duration, @"\d+").Value, out int time))
            return CommandResult.FromError("You specified an invalid amount of time!");
        if (user.IsBot)
            return CommandResult.FromError("Nope.");

        DbConfigRoles roles = await DbConfigRoles.GetById(Context.Guild.Id);
        if (user.TimedOutUntil.GetValueOrDefault() > DateTimeOffset.UtcNow || user.RoleIds.Contains(roles.StaffLvl1Role) || user.RoleIds.Contains(roles.StaffLvl2Role))
            return CommandResult.FromError($"You cannot mute **{user.Sanitize()}** because they are either already muted or a staff member.");

        Tuple<TimeSpan, string> resolved = ResolveDuration(duration, time, $"Muted **{user.Sanitize()}**", reason);
        if (resolved.Item1 == TimeSpan.Zero)
            return CommandResult.FromError("You specified an invalid amount of time!");
        if (resolved.Item1 > TimeSpan.FromDays(28))
            return CommandResult.FromError("You cannot mute for more than 28 days!");

        await user.SetTimeOutAsync(resolved.Item1);
        await LoggingSystem.Custom_UserMuted(user, Context.User, duration, reason);

        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        dbUser.AddToStat("Mutes", "1");
        await dbUser.UnlockAchievement("Literally 1984", "Get muted.", user, Context.Channel);

        await Context.User.NotifyAsync(Context.Channel, resolved.Item2);
        return CommandResult.FromSuccess();
    }

    [Alias("clear")]
    [Command("purge", RunMode = RunMode.Async)]
    [Summary("Purge any amount of messages (Note: messages that are two weeks old or older will fail to delete).")]
    [Remarks("$purge [count] <user>")]
    public async Task<RuntimeResult> Purge(int count, IGuildUser user = null)
    {
        if (count <= 0)
            return CommandResult.FromError("You want me to delete NO messages? Are you dense?");

        IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync(count + 1).FlattenAsync();
        messages = user == null
            ? messages.Where(msg => (DateTimeOffset.UtcNow - msg.Timestamp).TotalDays <= 14)
            : messages.Where(msg => msg.Author.Id == user.Id && (DateTimeOffset.UtcNow - msg.Timestamp).TotalDays <= 14);

        if (!messages.Any())
            return CommandResult.FromError("There are no messages to delete given your input.");

        await (Context.Channel as SocketTextChannel)?.DeleteMessagesAsync(messages);
        await LoggingSystem.Custom_MessagesPurged(messages, Context.Guild);
        return CommandResult.FromSuccess();
    }

    [Command("unban")]
    [Summary("Unban any currently banned member.")]
    [Remarks("$unban [userId]")]
    public async Task<RuntimeResult> Unban(ulong userId)
    {
        RestBan ban = await Context.Guild.GetBanAsync(userId);
        if (ban == null)
            return CommandResult.FromError("That user is not currently banned.");

        await Context.Guild.RemoveBanAsync(ban.User);
        await Context.User.NotifyAsync(Context.Channel, $"Unbanned **{ban.User.Sanitize()}**.");
        return CommandResult.FromSuccess();
    }

    [Alias("thaw")]
    [Command("unchill")]
    [Summary("Let chat talk now.")]
    [Remarks("$unchill")]
    public async Task<RuntimeResult> Unchill()
    {
        SocketTextChannel channel = Context.Channel as SocketTextChannel;
        OverwritePermissions perms = channel.GetPermissionOverwrite(Context.Guild.EveryoneRole) ?? OverwritePermissions.InheritAll;
        if (perms.SendMessages != PermValue.Deny)
            return CommandResult.FromError("This chat is not chilled.");

        await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, perms.Modify(sendMessages: PermValue.Inherit));
        await Context.User.NotifyAsync(Context.Channel, "Unchilled the chat early.");
        return CommandResult.FromSuccess();
    }

    [Alias("1985")]
    [Command("unmute")]
    [Summary("Unmute any member.")]
    [Remarks("$unmute [user]")]
    public async Task<RuntimeResult> Unmute(IGuildUser user)
    {
        if (user.TimedOutUntil.GetValueOrDefault() < DateTimeOffset.UtcNow)
            return CommandResult.FromError($"**{user.Sanitize()}** is not muted.");

        await user.RemoveTimeOutAsync();
        await LoggingSystem.Custom_UserUnmuted(user, Context.User);
        await Context.User.NotifyAsync(Context.Channel, $"Unmuted **{user.Sanitize()}**.");
        return CommandResult.FromSuccess();
    }

    private Tuple<TimeSpan, string> ResolveDuration(string duration, int time, string action, string reason)
    {
        TimeSpan ts = char.ToLower(duration[^1]) switch
        {
            's' => TimeSpan.FromSeconds(time),
            'm' => TimeSpan.FromMinutes(time),
            'h' => TimeSpan.FromHours(time),
            'd' => TimeSpan.FromDays(time),
            _ => TimeSpan.Zero
        };

        string response = $"{action} for {ts.FormatCompound()}";
        if (!string.IsNullOrWhiteSpace(reason))
            response += $"for \"{reason}\"";
        response += ".";

        return new(ts, response);
    }
}
