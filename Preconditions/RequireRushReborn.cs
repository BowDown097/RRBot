namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireRushRebornAttribute : PreconditionAttribute
{
    public const ulong RrMain = 809485099238031420;
    public const ulong RrTest = 834248227289038850;

    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        return context.Guild.Id is RrMain or RrTest
            ? Task.FromResult(PreconditionResult.FromSuccess())
            : Task.FromResult(PreconditionResult.FromError(""));
    }
}