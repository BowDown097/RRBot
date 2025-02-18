namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireDjAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        DbConfigRoles roles = await MongoManager.FetchConfigAsync<DbConfigRoles>(context.Guild.Id);
        if (roles.DjRole == default || context.Guild.Roles.All(r => r.Id != roles.DjRole))
            return PreconditionResult.FromError("There is no DJ role set or the role no longer exists. An admin needs to set it with $setdjrole.");

        return ((IGuildUser)context.User).RoleIds.Contains(roles.DjRole)
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("You must be a DJ.");
    }
}