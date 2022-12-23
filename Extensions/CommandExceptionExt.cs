namespace RRBot.Extensions;
public static class CommandExceptionExt
{
    public static async Task HandleDiscordErrors(this CommandException exception)
    {
        if (exception.Context is not SocketCommandContext context
            || exception.GetBaseException() is not HttpException { DiscordCode: DiscordErrorCode.MissingPermissions })
            return;

        List<GuildPermissions> rolePerms = context.Guild.CurrentUser.Roles.Select(r => r.Permissions).ToList();
        ChannelPermissions channelPerms = context.Guild.CurrentUser.GetPermissions(context.Channel as IGuildChannel);

        StringBuilder missingPerms = new("I need the following permission(s) for full functionality:\n");
        if (!rolePerms.Any(perm => perm.BanMembers)) 
            missingPerms.AppendLine("Ban Members");
        if (!rolePerms.Any(perm => perm.KickMembers)) 
            missingPerms.AppendLine("Kick Members");
        if (!rolePerms.Any(perm => perm.ManageChannels)) 
            missingPerms.AppendLine("Manage Channels");
        if (!rolePerms.Any(perm => perm.ManageNicknames)) 
            missingPerms.AppendLine("Manage Nicknames");
        if (!rolePerms.Any(perm => perm.ManageRoles)) 
            missingPerms.AppendLine("Manage Roles");
        if (!rolePerms.Any(perm => perm.ModerateMembers)) 
            missingPerms.AppendLine("Timeout Members");
        if (!channelPerms.AddReactions) 
            missingPerms.AppendLine("Add Reactions");
        if (!channelPerms.CreateInstantInvite) 
            missingPerms.AppendLine("Create Invites");
        if (!channelPerms.CreatePublicThreads) 
            missingPerms.AppendLine("Create Public Threads");
        if (!channelPerms.ManageMessages) 
            missingPerms.AppendLine("Manage Messages");
        if (!channelPerms.ManageThreads) 
            missingPerms.AppendLine("Manage Threads");
        if (!channelPerms.ReadMessageHistory) 
            missingPerms.AppendLine("Read Message History");
        if (!channelPerms.SendMessagesInThreads) 
            missingPerms.AppendLine("Send Messages In Threads");
        
        EmbedBuilder embed = new EmbedBuilder()
            .WithTitle("Missing Permissions")
            .WithDescription(missingPerms.ToString());
        await context.Channel.SendMessageAsync(embed: embed.Build());
    }
}