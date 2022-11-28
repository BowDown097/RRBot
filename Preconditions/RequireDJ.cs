namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireDjAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        DbConfig config = await MongoManager.FetchConfigAsync(context.Guild.Id);
        if (config.Roles.DjRole == default || context.Guild.Roles.All(r => r.Id != config.Roles.DjRole))
            return PreconditionResult.FromError("There is no DJ role set or the role no longer exists. An admin needs to set it with $setdjrole.");

        return context.User.GetRoleIds().Contains(config.Roles.DjRole)
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("You must be a DJ.");
    }
}