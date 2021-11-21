namespace RRBot.Entities.Commands
{
    public class CommandResult : RuntimeResult
    {
        public CommandResult(CommandError? error, string reason) : base(error, reason) { }
        public static CommandResult FromError(string reason) => new(CommandError.Unsuccessful, reason);
        public static CommandResult FromSuccess() => new(null, "");
    }
}