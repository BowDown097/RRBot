namespace RRBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireStaffAttribute : PreconditionAttribute
    {
        public override string ErrorMessage { get; set; }

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            DbConfigRoles roles = await DbConfigRoles.GetById(context.Guild.Id);
            return (context.User as IGuildUser)?.RoleIds.Contains(roles.StaffLvl1Role) == true
            || (context.User as IGuildUser)?.RoleIds.Contains(roles.StaffLvl2Role) == true
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("You must be Staff!");
        }
    }
}
