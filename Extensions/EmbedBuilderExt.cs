#nullable enable
namespace RRBot.Extensions;
public static class EmbedBuilderExt
{
    public static EmbedBuilder AddField(this EmbedBuilder builder, string name, object value, bool condition,
        bool inline = false)
    {
        if (condition)
            builder.AddField(name, value, inline);
        return builder;
    }

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
            builder
                .AddField($"Previous {name}", desc1, true).AddField($"Current {name}", desc2, true)
                .AddSeparatorField();
        }

        return builder;
    }

    public static EmbedBuilder RrAddField(this EmbedBuilder builder, string name, object? value,
        bool inline = false, bool showIfNotAvailable = true, string defaultValue = "N/A")
    {
        string? valueStr = value?.ToString();
        if (!string.IsNullOrWhiteSpace(valueStr) || showIfNotAvailable)
            builder.AddField(name, string.IsNullOrEmpty(valueStr) ? defaultValue : valueStr, inline);
        return builder;
    }

    public static EmbedBuilder WithElidedDescription(this EmbedBuilder builder, string description)
        => builder.WithDescription(description.Elide(4096));
}