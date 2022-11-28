namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public abstract class RequirePerkAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        DbUser user = await MongoManager.FetchUserAsync(context.User.Id, context.Guild.Id);
        return user.Perks.Count > 0
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("You have no perks.");
    }
}