using System;
using System.Threading.Tasks;
using Discord.Commands;
using Google.Cloud.Firestore;

namespace RRBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RequireCashAttribute : PreconditionAttribute
    {
        public float Amount { get; }

        public RequireCashAttribute(float amount = 1) => Amount = amount;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            DocumentReference doc = Program.database.Collection($"servers/{context.Guild.Id}/users").Document(context.Message.Author.Id.ToString());
            DocumentSnapshot snap = doc.GetSnapshotAsync().Result;

            if (snap.TryGetValue("cash", out float cash))
            {
                return cash >= Amount
                    ? Task.FromResult(PreconditionResult.FromSuccess())
                    : Task.FromResult(PreconditionResult.FromError($"{context.Message.Author.Mention}, you must have at least **${Amount}**."));
            }

            return Task.FromResult(PreconditionResult.FromError($"{context.Message.Author.Mention}, you're broke!"));
        }
    }
}
