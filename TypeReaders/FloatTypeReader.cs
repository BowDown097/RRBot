using System;
using System.Threading.Tasks;
using Discord.Commands;
using Google.Cloud.Firestore;

namespace RRBot.TypeReaders
{
    public class FloatTypeReader : TypeReader
    {
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            float cash;
            if (input.Equals("all", StringComparison.OrdinalIgnoreCase) && !context.Message.Content.StartsWith("$rob", StringComparison.Ordinal))
            {
                DocumentReference doc = Program.database.Collection($"servers/{context.Guild.Id}/users").Document(context.User.Id.ToString());
                DocumentSnapshot snap = await doc.GetSnapshotAsync();
                cash = snap.GetValue<float>("cash");
            }
            else if (!float.TryParse(input, out cash))
            {
                return TypeReaderResult.FromError(CommandError.ParseFailed, "Input could not be parsed as a float or did not specify to select all cash.");
            }

            return TypeReaderResult.FromSuccess(cash);
        }
    }
}
