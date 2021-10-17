using Discord;

namespace RRBot.Extensions
{
    public static class EmbedBuilderExt
    {
        public static EmbedBuilder AddStringField(this EmbedBuilder builder, string name, string value, bool inline = false)
            => builder.AddField(name, !string.IsNullOrWhiteSpace(value) ? value : "N/A", inline);
        public static EmbedBuilder AddSeparatorField(this EmbedBuilder builder)
            => builder.AddField("\u200B", "\u200B");
    }
}