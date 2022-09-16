namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireCooldownAttribute : PreconditionAttribute
{
    private string CooldownNode { get; set; }
    private string Message { get; set; }

    public RequireCooldownAttribute(string cooldownNode, string message)
    {
        CooldownNode = cooldownNode;
        Message = message;
    }

    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        DbUser user = await DbUser.GetById(context.Guild.Id, context.User.Id);
        if (user.CocaineRecoveryTime > 0)
        {
            long recoverySecs = user.CocaineRecoveryTime - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return PreconditionResult.FromError($"You're still under recovery from an overdose! You've gotta wait {TimeSpan.FromSeconds(recoverySecs).FormatCompound()}.");
        }

        long cooldownSecs = (long)user[CooldownNode] - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (cooldownSecs > 0)
            return PreconditionResult.FromError(string.Format(Message, TimeSpan.FromSeconds(cooldownSecs).FormatCompound()));

        user[CooldownNode] = 0;
        return PreconditionResult.FromSuccess();
    }
}