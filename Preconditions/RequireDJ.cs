namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireDjAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        DbConfigRoles roles = await DbConfigRoles.GetById(context.Guild.Id);
        return context.User.GetRoleIds().Contains(roles.DjRole)
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("You must be a DJ.");
    }
}