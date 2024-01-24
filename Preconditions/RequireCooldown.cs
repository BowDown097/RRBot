namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireCooldownAttribute(string cooldownNode, string message) : PreconditionAttribute
{
    private string CooldownNode { get; } = cooldownNode;
    private string Message { get; } = message;

    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        DbUser user = await MongoManager.FetchUserAsync(context.User.Id, context.Guild.Id);
        if (user.CocaineRecoveryTime > 0)
        {
            long recoverySecs = user.CocaineRecoveryTime - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return PreconditionResult.FromError($"You're still under recovery from an overdose! You've gotta wait {TimeSpan.FromSeconds(recoverySecs).FormatCompound()}.");
        }

        long cooldownSecs = (long)user[CooldownNode] - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (cooldownSecs > 0)
            return PreconditionResult.FromError(string.Format(Message, TimeSpan.FromSeconds(cooldownSecs).FormatCompound()));

        user[CooldownNode] = 0;
        await MongoManager.UpdateObjectAsync(user);
        return PreconditionResult.FromSuccess();
    }
}