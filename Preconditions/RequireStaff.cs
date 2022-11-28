namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireStaffAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        DbConfig config = await MongoManager.FetchConfigAsync(context.Guild.Id);
        if (config.Roles.StaffLvl1Role == default || context.Guild.Roles.All(r => r.Id != config.Roles.StaffLvl1Role))
            return PreconditionResult.FromError("There is no staff role set or the role no longer exists. An admin needs to set it with $setstafflvl1role.");
            
        return context.User.GetRoleIds().Contains(config.Roles.StaffLvl1Role) || context.User.GetRoleIds().Contains(config.Roles.StaffLvl2Role)
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("You must be staff.");
    }
}