namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireRankLevelAttribute(int level) : PreconditionAttribute
{
    public int RankLevel { get; } = level;

    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        DbConfigRanks ranks = await MongoManager.FetchConfigAsync<DbConfigRanks>(context.Guild.Id);
        if (!ranks.Costs.ContainsKey(RankLevel))
            return PreconditionResult.FromError($"No rank is configured at level {RankLevel}. An admin needs to set it with $addrank.");

        ulong roleId = ranks.Ids[RankLevel];
        IRole role = context.Guild.GetRole(roleId);
        if (role is null)
            return PreconditionResult.FromError($"A rank is configured at level {RankLevel}, but its role no longer exists.");

        return ((IGuildUser)context.User).RoleIds.Contains(roleId)
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError($"You must have the {role.Name} role.");
    }
}