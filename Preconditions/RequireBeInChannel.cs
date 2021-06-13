using System;
using System.Threading.Tasks;
using Discord.Commands;

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
                : Task.FromResult(PreconditionResult.FromError($"{context.User.Mention}, you must be in the #{Name} channel."));
        }
    }
}