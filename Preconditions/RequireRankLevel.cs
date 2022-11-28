namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireRankLevelAttribute : PreconditionAttribute
{
    public int RankLevel { get; }

    public RequireRankLevelAttribute(int level) => RankLevel = level;

    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        DbConfig config = await MongoManager.FetchConfigAsync(context.Guild.Id);
        if (!config.Ranks.Costs.ContainsKey(RankLevel))
            return PreconditionResult.FromError($"No rank is configured at level {RankLevel}. An admin needs to set it with $addrank.");

        ulong roleId = config.Ranks.Ids[RankLevel];
        IRole role = context.Guild.GetRole(roleId);
        if (role == null)
            return PreconditionResult.FromError($"A rank is configured at level {RankLevel}, but its role no longer exists.");

        return context.User.GetRoleIds().Contains(roleId)
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError($"You must have the {role.Name} role.");
    }
}