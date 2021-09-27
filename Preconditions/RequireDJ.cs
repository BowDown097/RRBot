using Discord;
using Discord.Commands;
using RRBot.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RRBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireDJAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            DbConfigRoles roles = await DbConfigRoles.GetById(context.Guild.Id);
            return (context.User as IGuildUser)?.RoleIds.Contains(roles.DJRole) == true
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("You must be a DJ!");
        }
    }
}
