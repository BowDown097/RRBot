namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireItemAttribute : PreconditionAttribute
{
    public string ItemType { get; }

    public RequireItemAttribute(string itemType = "") => ItemType = itemType;

    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        DbUser user = await DbUser.GetById(context.Guild.Id, context.User.Id);
        if (user.Items.Count > 0)
        {
            return string.IsNullOrEmpty(ItemType) || user.Items.Any(item => item.EndsWith(ItemType))
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError($"You need to have a {ItemType}.");
        }

        return PreconditionResult.FromError("You have no items!");
    }
}