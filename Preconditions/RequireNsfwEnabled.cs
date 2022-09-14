namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireNsfwEnabledAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        DbConfigOptionals modules = await DbConfigOptionals.GetById(context.Guild.Id);
        return modules.NsfwEnabled
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("NSFW commands are disabled.");
    }
}