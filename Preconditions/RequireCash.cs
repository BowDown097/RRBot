namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireCashAttribute : PreconditionAttribute
{
    public decimal Cash { get; }

    public RequireCashAttribute(double cash = 0.01) => Cash = (decimal)cash;

    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        DbUser user = await MongoManager.FetchUserAsync(context.User.Id, context.Guild.Id);
        return user.Cash >= Cash
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError(Cash == 0.01m ? "You're broke!" : $"You need **{Cash:C2}**.");
    }
}