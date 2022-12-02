namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireAdministratorAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        IGuildUser guildUser = context.User as IGuildUser;
        DbConfigRoles roles = await MongoManager.FetchConfigAsync<DbConfigRoles>(context.Guild.Id);
        return guildUser.RoleIds.Contains(roles.StaffLvl2Role) || guildUser.GuildPermissions.Has(GuildPermission.Administrator)
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("You must be an admin.");
    }
}