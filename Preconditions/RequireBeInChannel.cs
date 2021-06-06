using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace RRBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RequireBeInChannelAttribute : PreconditionAttribute
    {
        public string Name { get; }

        public RequireBeInChannelAttribute(string name) => Name = name;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.Channel.Name == Name) return Task.FromResult(PreconditionResult.FromSuccess());
            return Task.FromResult(PreconditionResult.FromError($"{context.Message.Author.Mention}, you must be in the #{Name} channel."));
        }
    }
}
