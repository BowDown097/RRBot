#nullable enable
namespace RRBot.Extensions;
public static class EmbedBuilderExt
{
    public static EmbedBuilder AddSeparatorField(this EmbedBuilder builder)
        => builder.AddField("\u200b", "\u200b");

    public static EmbedBuilder AddUpdateCompField(this EmbedBuilder builder, string name,
        object? value1, object? value2, string defaultValue = "N/A")
    {
        string? value1Str = value1?.ToString();
        string? value2Str = value2?.ToString();

        if (value1Str != value2Str)
        {
            string desc1 = !string.IsNullOrWhiteSpace(value1Str) ? value1Str : defaultValue;
            string desc2 = !string.IsNullOrWhiteSpace(value2Str) ? value2Str : defaultValue;
            builder.AddField($"Previous {name}", desc1, true).AddField($"Current {name}", desc2, true).AddSeparatorField();
        }

        return builder;
    }

    public static EmbedBuilder RRAddField(this EmbedBuilder builder, string name, object? value,
        bool inline = false, string defaultValue = "N/A")
    {
        string? valueStr = value?.ToString();
        string description = !string.IsNullOrWhiteSpace(valueStr) ? valueStr : defaultValue;
        return builder.AddField(name, description, inline);
    }
}