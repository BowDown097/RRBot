namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireCashAttribute : PreconditionAttribute
{
    public double Cash { get; set; }

    public RequireCashAttribute(double cash = 0.01) => Cash = cash;

    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        DbUser user = await DbUser.GetById(context.Guild.Id, context.User.Id);
        return user.Cash >= Cash
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError(Cash == 0.01 ? "You're broke!" : $"You need **{Cash:C2}**!");
    }
}