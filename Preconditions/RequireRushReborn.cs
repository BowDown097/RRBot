namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireRushRebornAttribute : PreconditionAttribute
{
    public const ulong RR_MAIN = 809485099238031420;
    public const ulong RR_TEST = 834248227289038850;

    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        return context.Guild.Id is RR_MAIN or RR_TEST
            ? Task.FromResult(PreconditionResult.FromSuccess())
            : Task.FromResult(PreconditionResult.FromError(""));
    }
}