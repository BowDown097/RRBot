using Discord;
#nullable enable

namespace RRBot.Extensions
{
    public static class EmbedBuilderExt
    {
        public static EmbedBuilder AddSeparatorField(this EmbedBuilder builder)
            => builder.AddField("\u200b", "\u200b");

        public static EmbedBuilder RRAddField(this EmbedBuilder builder, string name, object? value,
            bool inline = false, string defaultValue = "N/A")
        {
            string? valueStr = value?.ToString();
            string description = !string.IsNullOrWhiteSpace(valueStr) ? valueStr : defaultValue;
            return builder.AddField(name, description, inline);
        }
    }
}