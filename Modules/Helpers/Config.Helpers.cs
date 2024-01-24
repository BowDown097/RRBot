namespace RRBot.Modules;
public partial class Config
{
    private static string Pair(string descriptor, object obj)
    {
        return obj is string s
            ? $"{descriptor}: {(!string.IsNullOrWhiteSpace(s) ? s : "N/A")}"
            : $"{descriptor}: {obj ?? "N/A"}";
    }
}