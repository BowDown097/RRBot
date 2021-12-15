namespace RRBot.Modules;
[Summary("Impose an Orwellian life on the poor normies in chat, through bans, kicks, mutes, you name it.")]
[RequireStaff]
public class Moderation : ModuleBase<SocketCommandContext>
{
    private Tuple<TimeSpan, string> ResolveDuration(string duration, int time, string action)
    {
        char suffix = char.ToLowerInvariant(duration[^1]);
        return suffix switch
        {
            's' => new(TimeSpan.FromSeconds(time), $"**{Context.User}** has {action} for {time} second(s)"),
            'm' => new(TimeSpan.FromMinutes(time), $"**{Context.User}** has {action} for {time} minute(s)"),
            'h' => new(TimeSpan.FromHours(time), $"**{Context.User}** has {action} for {time} hour(s)"),
            'd' => new(TimeSpan.FromDays(time), $"**{Context.User}** has {action} for {time} day(s)"),
            _ => new(TimeSpan.Zero, null),
        };
    }

    [Alias("seethe")]
    [Command("ban")]
    [Summary("Ban any member.")]
    [Remarks("$ban [user] <duration> <reason>")]
    public async Task<RuntimeResult> Ban(IGuildUser user, string duration = "", [Remainder] string reason = "")
    {
        if (user.IsBot)
            return CommandResult.FromError("Nope.");

        DbConfigRoles roles = await DbConfigRoles.GetById(Context.Guild.Id);
        if (user.RoleIds.Contains(roles.StaffLvl1Role) || user.RoleIds.Contains(roles.StaffLvl2Role))
            return CommandResult.FromError($"You cannot ban **{user.Sanitize()}** because they are a staff member.");

        DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
        if (int.TryParse(Regex.Match(duration, @"\d+").Value, out int time))
        {
            Tuple<TimeSpan, string> resolved = ResolveDuration(duration, time, $"banned **{user.Sanitize()}**");
            if (resolved.Item1 == TimeSpan.Zero)
                return CommandResult.FromError("You specified an invalid amount of time!");

            DbBan ban = await DbBan.GetById(Context.Guild.Id, user.Id);
            ban.Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(resolved.Item1.TotalSeconds);
            await LoggingSystem.Client_UserBanned(user as SocketUser, user.Guild as SocketGuild);
            await user.BanAsync(reason: reason);
            dbUser.AddToStat("Bans", "1");

            string response = resolved.Item2;
            response += string.IsNullOrWhiteSpace(reason) ? "." : $" for '{reason}'";
            await ReplyAsync(response);
            return CommandResult.FromSuccess();
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            await user.BanAsync(reason: reason);
            dbUser.AddToStat("Bans", "1");
            return CommandResult.FromSuccess();
        }

        return CommandResult.FromError("You specified an invalid amount of time!");
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
        if (int.TryParse(Regex.Match(duration, @"\d+").Value, out int time))
        {
            Tuple<TimeSpan, string> resolved = ResolveDuration(duration, time, "chilled the chat");
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

            await ReplyAsync(resolved.Item2 + ".");
            return CommandResult.FromSuccess();
        }

        return CommandResult.FromError("You specified an invalid amount of time!");
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

        string response = $"**{Context.User}** has kicked **{user.Sanitize()}**";
        response += string.IsNullOrWhiteSpace(reason) ? "." : $"for '{reason}'";
        await ReplyAsync(response);
        return CommandResult.FromSuccess();
    }

    [Alias("1984")]
    [Command("mute")]
    [Summary("Mute any member for any amount of time with any reason.")]
    [Remarks("$mute [user] [duration] <reason>")]
    public async Task<RuntimeResult> Mute(IGuildUser user, string duration, [Remainder] string reason = "")
    {
        if (user.IsBot)
            return CommandResult.FromError("Nope.");

        DbConfigRoles roles = await DbConfigRoles.GetById(Context.Guild.Id);
        if (roles.MutedRole != 0 && roles.StaffLvl1Role != 0 && roles.StaffLvl2Role != 0)
        {
            if (user.RoleIds.Contains(roles.MutedRole) || user.RoleIds.Contains(roles.StaffLvl1Role) || user.RoleIds.Contains(roles.StaffLvl2Role))
                return CommandResult.FromError($"You cannot mute **{user.Sanitize()}** because they are either already muted or a staff member.");

            if (int.TryParse(Regex.Match(duration, @"\d+").Value, out int time))
            {
                Tuple<TimeSpan, string> resolved = ResolveDuration(duration, time, $"muted **{user.Sanitize()}**");
                if (resolved.Item1 == TimeSpan.Zero)
                    return CommandResult.FromError("You specified an invalid amount of time!");

                DbMute mute = await DbMute.GetById(Context.Guild.Id, user.Id);
                mute.Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(resolved.Item1.TotalSeconds);
                await LoggingSystem.Custom_UserMuted(user, Context.User, duration, reason);
                await user.AddRoleAsync(roles.MutedRole);

                DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
                dbUser.AddToStat("Mutes", "1");
                await dbUser.UnlockAchievement("Literally 1984", "Get muted.", user as SocketUser, Context.Channel);

                string response = resolved.Item2;
                response += string.IsNullOrWhiteSpace(reason) ? "." : $" for '{reason}'";
                await ReplyAsync(response);
                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError("You specified an invalid amount of time!");
        }

        return CommandResult.FromError("This server's staff and/or muted role(s) have yet to be set.");
    }

    [Alias("clear")]
    [Command("purge")]
    [Summary("Purge any amount of messages (Note: messages that are two weeks old or older will fail to delete).")]
    [Remarks("$purge [count] <user>")]
    public async Task<RuntimeResult> Purge(int count, IGuildUser user = null)
    {
        if (count <= 0)
            return CommandResult.FromError("Count must be more than zero.");

        IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync(count + 1).FlattenAsync();
        messages = messages.Where(msg => (DateTimeOffset.UtcNow - msg.Timestamp).TotalDays <= 14);
        if (user != null)
            messages = messages.Where(msg => msg.Author.Id == user.Id);

        if (!messages.Any())
            return CommandResult.FromError("No messages were deleted.");

        await (Context.Channel as SocketTextChannel)?.DeleteMessagesAsync(messages);
        await LoggingSystem.Custom_MessagesPurged(messages, Context.Guild);

        if (messages.Any(msg => (DateTimeOffset.UtcNow - msg.Timestamp).TotalDays > 14))
            await Context.User.NotifyAsync(Context.Channel, "Warning: Some messages were found to be older than 2 weeks and can't be deleted.");
        return CommandResult.FromSuccess();
    }

    [Command("unban")]
    [Summary("Unban any currently banned member.")]
    [Remarks("$unban [user]")]
    public async Task<RuntimeResult> Unban(IUser user)
    {
        IReadOnlyCollection<RestBan> bans = await Context.Guild.GetBansAsync();
        if (!bans.Any(ban => ban.User.Id == user.Id))
            return CommandResult.FromError("That user is not currently banned.");

        string userString = bans.FirstOrDefault(ban => ban.User.Id == user.Id).User.ToString();
        await ReplyAsync($"**{Context.User}** has unbanned **{userString}**.");

        await Context.Guild.RemoveBanAsync(user.Id);
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
        await ReplyAsync($"**{Context.User}** took one for the team and unchilled early.");
        return CommandResult.FromSuccess();
    }

    [Alias("1985")]
    [Command("unmute")]
    [Summary("Unmute any member.")]
    [Remarks("$unmute [user]")]
    public async Task<RuntimeResult> Unmute(IGuildUser user)
    {
        if (user.IsBot)
            return CommandResult.FromError("Nope.");

        DbConfigRoles roles = await DbConfigRoles.GetById(Context.Guild.Id);
        if (user.RoleIds.Contains(roles.MutedRole))
        {
            await LoggingSystem.Custom_UserUnmuted(user, Context.User);
            await user.RemoveRoleAsync(roles.MutedRole);
            await ReplyAsync($"**{Context.User}** has unmuted **{user.Sanitize()}**.");
            return CommandResult.FromSuccess();
        }

        return CommandResult.FromError("That user is not muted or the server's muted role has yet to be set.");
    }
}
