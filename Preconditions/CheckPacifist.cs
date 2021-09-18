using Discord.Commands;
using RRBot.Entities;
using System;
using System.Threading.Tasks;

namespace RRBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class CheckPacifistAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            DbUser user = await DbUser.GetById(context.Guild.Id, context.User.Id);
            return user.Perks?.ContainsKey("Pacifist") == true
                ? PreconditionResult.FromError("You cannot use this command as you have the Pacifist perk.")
                : PreconditionResult.FromSuccess();
        }
    }
}
