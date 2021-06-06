using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace RRBot
{
    public class EmoteTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            if (context.Guild.Emotes.Any(e => e.ToString() == input) && Emote.TryParse(input, out Emote result)) return Task.FromResult(TypeReaderResult.FromSuccess(result as IEmote));

            try
            {
                return Task.FromResult(TypeReaderResult.FromSuccess(new Emoji(input) as IEmote));
            }
            catch (Exception)
            {
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Input could not be parsed as an IEmote."));
            }
        }
    }
}
