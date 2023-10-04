namespace RRBot.Systems;

public static class ExceptionHandler
{
    public static async Task HandleHttpException(HttpException ex, ShardedCommandContext context)
    {
        if (ex.DiscordCode != DiscordErrorCode.InsufficientPermissions)
            return;

        List<GuildPermissions> rolePerms = context.Guild.CurrentUser.Roles.Select(r => r.Permissions).ToList();
        ChannelPermissions channelPerms = context.Guild.CurrentUser.GetPermissions(context.Channel as IGuildChannel);

        List<string> missingPerms = new();
        if (!rolePerms.Any(perm => perm.Administrator))
        {
            if (!rolePerms.Any(perm => perm.BanMembers))
                missingPerms.Add("Ban Members");
            if (!rolePerms.Any(perm => perm.KickMembers))
                missingPerms.Add("Kick Members");
            if (!rolePerms.Any(perm => perm.ManageChannels))
                missingPerms.Add("Manage Channels");
            if (!rolePerms.Any(perm => perm.ManageNicknames))
                missingPerms.Add("Manage Nicknames");
            if (!rolePerms.Any(perm => perm.ManageRoles))
                missingPerms.Add("Manage Roles");
            if (!rolePerms.Any(perm => perm.ModerateMembers))
                missingPerms.Add("Timeout Members");
            if (!channelPerms.AddReactions)
                missingPerms.Add("Add Reactions");
            if (!channelPerms.CreateInstantInvite)
                missingPerms.Add("Create Invites");
            if (!channelPerms.CreatePublicThreads)
                missingPerms.Add("Create Public Threads");
            if (!channelPerms.ManageMessages)
                missingPerms.Add("Manage Messages");
            if (!channelPerms.ManageThreads)
                missingPerms.Add("Manage Threads");
            if (!channelPerms.ReadMessageHistory)
                missingPerms.Add("Read Message History");
            if (!channelPerms.SendMessagesInThreads)
                missingPerms.Add("Send Messages In Threads");
        }

        string description;
        if (missingPerms.Count > 0)
        {
            description = "I do not have permission to perform this action. " +
                          "Please note that I need the following permission(s) for full functionality:\n" +
                          string.Join('\n', missingPerms) +
                          "\n\nAlternatively, my role(s) may be too low in the role hierarchy to perform this action. " +
                          "Make sure that this is not the case either.";
        }
        else
        {
            description = "My role(s) appear to be too low in the role hierarchy to perform this action.";
        }

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Insufficient Permissions")
            .WithDescription(description);
        await context.Channel.SendMessageAsync(embed: embed.Build());
    }
}