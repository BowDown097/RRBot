using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace RRBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireBeInChannelAttribute : PreconditionAttribute
    {
        public string Name { get; }

        public RequireBeInChannelAttribute(string name) => Name = name;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            return context.Channel.Name == Name
                ? Task.FromResult(PreconditionResult.FromSuccess())
                : Task.FromResult(PreconditionResult.FromError($"You must be in the #{Name} channel."));
        }
    }
}