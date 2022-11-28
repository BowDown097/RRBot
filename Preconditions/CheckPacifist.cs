namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class CheckPacifistAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        DbUser user = await MongoManager.FetchUserAsync(context.User.Id, context.Guild.Id);
        return user.Perks.ContainsKey("Pacifist")
            ? PreconditionResult.FromError("You cannot use this command as you have the Pacifist perk.")
            : PreconditionResult.FromSuccess();
    }
}