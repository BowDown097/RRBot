namespace RRBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireCashAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            DbUser user = await DbUser.GetById(context.Guild.Id, context.User.Id);
            return user.Cash >= 0.01
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("You're broke!");
        }
    }
}
