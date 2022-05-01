namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireCooldownAttribute : PreconditionAttribute
{
    public string CooldownNode { get; set; }
    public string Message { get; set; }

    public RequireCooldownAttribute(string cooldownNode, string message)
    {
        CooldownNode = cooldownNode;
        Message = message;
    }

    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        DbUser user = await DbUser.GetById(context.Guild.Id, context.User.Id);
        long cooldown = (long)user[CooldownNode];
        long cooldownSecs = cooldown - DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (cooldownSecs > 0)
            return PreconditionResult.FromError(string.Format(Message, TimeSpan.FromSeconds(cooldownSecs).FormatCompound()));

        user[CooldownNode] = 0;
        return PreconditionResult.FromSuccess();
    }
}