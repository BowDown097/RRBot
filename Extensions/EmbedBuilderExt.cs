using Discord;

namespace RRBot.Extensions
{
    public static class EmbedBuilderExt
    {
        public static EmbedBuilder AddSeparatorField(this EmbedBuilder builder)
            => builder.AddField("\u200B", "\u200B");
    }
}