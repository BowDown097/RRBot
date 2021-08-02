using Discord.Commands;
using Google.Cloud.Firestore;
using System;
using System.Threading.Tasks;

namespace RRBot.TypeReaders
{
    public class DoubleTypeReader : TypeReader
    {
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            if (context.Message.Content.StartsWith("$invest", StringComparison.Ordinal) || context.Message.Content.StartsWith("$withdraw", StringComparison.Ordinal))
            {
                if (!double.TryParse(input, out double @double)) return TypeReaderResult.FromError(CommandError.ParseFailed, "Input could not be parsed as a double.");
                return TypeReaderResult.FromSuccess(@double);
            }

            double cash;
            if (input.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                DocumentReference doc = Program.database.Collection($"servers/{context.Guild.Id}/users").Document(context.User.Id.ToString());
                DocumentSnapshot snap = await doc.GetSnapshotAsync();
                cash = snap.GetValue<double>("cash");
            }
            else if (!double.TryParse(input, out cash))
            {
                return TypeReaderResult.FromError(CommandError.ParseFailed, "Input could not be parsed as a double or did not specify to select all cash.");
            }

            return TypeReaderResult.FromSuccess(cash);
        }
    }
}
