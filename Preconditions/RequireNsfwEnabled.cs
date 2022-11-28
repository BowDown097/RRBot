namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireNsfwEnabledAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        DbConfig config = await MongoManager.FetchConfigAsync(context.Guild.Id);
        return config.Miscellaneous.NsfwEnabled || !config.Miscellaneous.DisabledModules.Contains("Nsfw")
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("NSFW commands are disabled.");
    }
}