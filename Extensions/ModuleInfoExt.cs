using Discord.Commands;
using System.Linq;

namespace RRBot.Extensions
{
    public static class ModuleInfoExt
    {
        public static bool TryGetPrecondition<T>(this ModuleInfo module) where T : PreconditionAttribute => module.TryGetPrecondition<T>(out _);
        public static bool TryGetPrecondition<T>(this ModuleInfo module, out T precondition) where T : PreconditionAttribute
        {
            T possPrecond = (T)module.Preconditions.FirstOrDefault(cond => cond is T);
            precondition = possPrecond;
            return possPrecond != null;
        }
    }
}
