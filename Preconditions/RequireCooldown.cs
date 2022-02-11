namespace RRBot.Preconditions;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireCooldownAttribute : PreconditionAttribute
{
    public string CooldownNode { get; set; }
    public string Message { get; set; }

    public RequireCooldownAttribute(string cooldownNode, string message)
    {
        CooldownNode = cooldownNode;
        Message = message;
    }

    public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        DbUser user = await DbUser.GetById(context.Guild.Id, context.User.Id);
        long cooldown = (long)user[CooldownNode] - DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // speed demon cooldown reducer
        if (user.Perks.ContainsKey("Speed Demon"))
            cooldown = (long)(cooldown * 0.85);
        // 4th rank cooldown reducer
        DbConfigRanks ranks = await DbConfigRanks.GetById(context.Guild.Id);
        if (context.User.GetRoleIds().Contains(ranks.Ids.Select(k => k.Value).LastOrDefault()))
            cooldown = (long)(cooldown * 0.75);

        long newCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(cooldown);
        if (newCooldown > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            return PreconditionResult.FromError(string.Format($"{Message}",
                    TimeSpan.FromSeconds(cooldown).FormatCompound()));
        }

        user[CooldownNode] = 0;
        return PreconditionResult.FromSuccess();
    }
}