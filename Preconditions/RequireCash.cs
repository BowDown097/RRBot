using System;
using System.Threading.Tasks;
using Discord.Commands;
using Google.Cloud.Firestore;

namespace RRBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireCashAttribute : PreconditionAttribute
    {
        public float Amount { get; }

        public RequireCashAttribute(float amount = 1) => Amount = amount;

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            DocumentReference doc = Program.database.Collection($"servers/{context.Guild.Id}/users").Document(context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            float cash = snap.GetValue<float>("cash");
            if (cash > 0)
            {
                return cash >= Amount
                    ? PreconditionResult.FromSuccess()
                    : PreconditionResult.FromError($"{context.User.Mention}, you must have at least **${Amount}**.");
            }

            return PreconditionResult.FromError($"{context.User.Mention}, you're broke!");
        }
    }
}
