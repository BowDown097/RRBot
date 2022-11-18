namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireStaffAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        DbConfigRoles roles = await DbConfigRoles.GetById(context.Guild.Id);
        if (roles.StaffLvl1Role == default || context.Guild.Roles.All(r => r.Id != roles.StaffLvl1Role))
            return PreconditionResult.FromError("There is no staff role set or the role no longer exists. An admin needs to set it with $setstafflvl1role.");
            
        return context.User.GetRoleIds().Contains(roles.StaffLvl1Role) || context.User.GetRoleIds().Contains(roles.StaffLvl2Role)
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("You must be staff.");
    }
}