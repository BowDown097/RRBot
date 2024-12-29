namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireStaffLevelAttribute(int level) : PreconditionAttribute
{
    public int StaffLevel { get; } = level;

    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        if ((context.User as IGuildUser)?.GuildPermissions.Has(GuildPermission.Administrator) == true)
            return PreconditionResult.FromSuccess();

        PropertyInfo property = typeof(DbConfigRoles).GetProperty($"StaffLvl{StaffLevel}Role");
        if (property?.CanRead == false)
            throw new ArgumentException($"Role property does not exist for staff level {StaffLevel}");

        DbConfigRoles roles = await MongoManager.FetchConfigAsync<DbConfigRoles>(context.Guild.Id);
        ulong roleId = property.GetValue(roles, null) as ulong? ?? default;
        if (roleId == default || context.Guild.Roles.All(r => r.Id != roleId))
            return PreconditionResult.FromError($"There is no staff level {StaffLevel} role set or the role no longer exists. An admin needs to set it with $setstafflvl{StaffLevel}role.");

        IEnumerable<ulong> roleIds = context.User.GetRoleIds();
        bool success = StaffLevel >= 2
            ? roleIds.Any(id => id == roles.StaffLvl1Role || id == roles.StaffLvl2Role)
            : roleIds.Contains(roles.StaffLvl1Role);

        if (!success)
        {
            string roleName = context.Guild.Roles.FirstOrDefault(r => r.Id == roleId)?.Name;
            return PreconditionResult.FromError($"You must be a {roleName} or higher.");
        }

        return PreconditionResult.FromSuccess();
    }
}