using Discord.Commands;
using RRBot.Entities;
using RRBot.Modules;
using System;
using System.Threading.Tasks;

namespace RRBot.TypeReaders
{
    public class DoubleTypeReader : TypeReader
    {
        public override async Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            DbUser user = await DbUser.GetById(context.Guild.Id, context.User.Id);
            if (context.Message.Content.StartsWith("$withdraw"))
            {
                double @double;
                if (input.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    string crypto = context.Message.Content.ToLower().Replace("$withdraw ", "").Replace(" all", "");
                    string abbreviation = Investments.ResolveAbbreviation(crypto);
                    if (abbreviation is null)
                        return TypeReaderResult.FromError(CommandError.ParseFailed, $"**{crypto}** is not a currently accepted currency!");

                    @double = (double)user[abbreviation];
                    if (@double < Constants.INVESTMENT_MIN_AMOUNT)
                        return TypeReaderResult.FromError(CommandError.Unsuccessful, $"You have no {crypto.ToUpper()}!");
                }
                else if (!double.TryParse(input, out @double))
                {
                    return TypeReaderResult.FromError(CommandError.ParseFailed, $"\"{input}\" is not a double value.");
                }

                return TypeReaderResult.FromSuccess(@double);
            }

            double cash;
            if (input.Equals("all", StringComparison.OrdinalIgnoreCase))
                cash = user.Cash;
            else if (!double.TryParse(input, out cash))
                return TypeReaderResult.FromError(CommandError.ParseFailed, $"\"{input}\" is not a double value.");

            return TypeReaderResult.FromSuccess(cash);
        }
    }
}
