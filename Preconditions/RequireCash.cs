using Discord.Commands;
using Google.Cloud.Firestore;
using System;
using System.Threading.Tasks;

namespace RRBot.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireCashAttribute : PreconditionAttribute
    {
        public double Amount { get; }

        public RequireCashAttribute(double amount = 0.01) => Amount = amount;

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            DocumentReference doc = Program.database.Collection($"servers/{context.Guild.Id}/users").Document(context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            double cash = snap.GetValue<double>("cash");
            return cash >= Amount
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("You're broke!");
        }
    }
}
