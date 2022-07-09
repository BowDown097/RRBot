namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireStaffAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        DbConfigRoles roles = await DbConfigRoles.GetById(context.Guild.Id);
        return context.User.GetRoleIds().Contains(roles.StaffLvl1Role) || context.User.GetRoleIds().Contains(roles.StaffLvl2Role)
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("You must be Staff.");
    }
}