namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireToolAttribute : PreconditionAttribute
{
    public string ToolType { get; }

    public RequireToolAttribute(string toolType = "") => ToolType = toolType;

    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        DbUser user = await DbUser.GetById(context.Guild.Id, context.User.Id);
        if (user.Tools.Count > 0)
        {
            return string.IsNullOrEmpty(ToolType) || user.Tools.Any(t => t.EndsWith(ToolType))
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError($"You need to have a {ToolType}.");
        }

        return PreconditionResult.FromError("You have no tools!");
    }
}