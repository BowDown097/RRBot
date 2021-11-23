namespace RRBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireRankLevelAttribute : PreconditionAttribute
    {
        public string RankLevel { get; }

        public RequireRankLevelAttribute(string level) => RankLevel = level;

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            DbConfigRanks ranks = await DbConfigRanks.GetById(context.Guild.Id);
            if (!ranks.Costs.ContainsKey(RankLevel))
                return PreconditionResult.FromError($"No rank is configured at level {RankLevel}!");

            ulong roleId = ranks.Ids[RankLevel];
            IRole role = context.Guild.GetRole(roleId);
            if (role == null)
                return PreconditionResult.FromError($"A rank is configured at level {RankLevel}, but its role no longer exists.");

            return (context.User as IGuildUser).RoleIds.Contains(roleId)
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError($"You must have the {role.Name} role.");
        }
    }
}
