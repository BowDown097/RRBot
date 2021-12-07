#nullable enable
namespace RRBot.Common
{
    public static class RRFormat
    {
        public static string BasicSanitize(object? value)
        {
            string? valueStr = value?.ToString();
            if (string.IsNullOrEmpty(valueStr)) return "";
            return valueStr.Replace("@everyone", "").Replace("@here", "").Replace("`", "");
        }

        public static string Sanitize(object? value)
        {
            string? valueStr = value?.ToString();
            if (string.IsNullOrEmpty(valueStr)) return "";
            return valueStr.Replace("`", "").Replace("@", "").Replace("*", "");
        }
    }
}