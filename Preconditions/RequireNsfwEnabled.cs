using Discord.Commands;
using RRBot.Entities;
using System;
using System.Threading.Tasks;

namespace RRBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireNsfwEnabledAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            DbConfigModules modules = await DbConfigModules.GetById(context.Guild.Id);
            return modules.NSFWEnabled
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("NSFW commands are disabled!");
        }
    }
}
