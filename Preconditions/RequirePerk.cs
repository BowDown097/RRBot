namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePerkAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        DbUser user = await DbUser.GetById(context.Guild.Id, context.User.Id);
        return user.Perks.Count > 0
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("You have no perks.");
    }
}