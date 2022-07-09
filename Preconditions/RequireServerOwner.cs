namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireServerOwnerAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        return Task.FromResult(context.User.Id == context.Guild.OwnerId
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("You must be the server owner."));
    }
}