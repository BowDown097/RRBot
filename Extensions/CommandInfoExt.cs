using Discord.Commands;
using System.Linq;

namespace RRBot.Extensions
{
    public static class CommandInfoExt
    {
        public static bool TryGetPrecondition<T>(this CommandInfo command) where T : PreconditionAttribute => command.TryGetPrecondition<T>(out _);
        public static bool TryGetPrecondition<T>(this CommandInfo command, out T precondition) where T : PreconditionAttribute
        {
            T possPrecond = (T)command.Preconditions.FirstOrDefault(cond => cond is T);
            precondition = possPrecond;
            return possPrecond != null;
        }
    }
}
