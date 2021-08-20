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
            if (context.Message.Content.StartsWith("$withdraw", StringComparison.Ordinal))
            {
                double @double;
                if (input.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    DocumentReference doc = Program.database.Collection($"servers/{context.Guild.Id}/users").Document(context.User.Id.ToString());
                    DocumentSnapshot snap = await doc.GetSnapshotAsync();
                    string crypto = context.Message.Content.ToLower().Replace("$withdraw ", "").Replace(" all", "");
                    if (!snap.TryGetValue(crypto, out @double) || @double <= 0)
                        return TypeReaderResult.FromError(CommandError.ParseFailed, $"You either have no {crypto.ToUpper()} or it is an invalid currency.");
                }
                else if (!double.TryParse(input, out @double))
                {
                    return TypeReaderResult.FromError(CommandError.ParseFailed, $"\"{input}\" is not a double value.");
                }

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
                return TypeReaderResult.FromError(CommandError.ParseFailed, $"\"{input}\" is not a double value.");
            }

            return TypeReaderResult.FromSuccess(cash);
        }
    }
}
