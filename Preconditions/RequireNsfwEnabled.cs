namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireNsfwEnabledAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        DbConfigMisc misc = await MongoManager.FetchConfigAsync<DbConfigMisc>(context.Guild.Id);
        return misc.NsfwEnabled || !misc.DisabledModules.Contains("Nsfw")
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("​NSFW commands are disabled.");
    }
}