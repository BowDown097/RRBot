namespace RRBot.Modules;
public partial class Economy
{
    private static string BuildPropsList(DbUser dbUser, params string[] properties)
    {
        StringBuilder builder = new();
        foreach (string prop in properties)
        {
            object? obj = dbUser[prop];
            if (obj is null)
                continue;

            string propS = prop.SplitPascalCase();
            switch (obj)
            {
                case System.Collections.ICollection col:
                    if (col.Count > 0) builder.AppendLine($"**{propS}**: {col.Count}");
                    break;
                case decimal d:
                    if (prop == "GamblingMultiplier" && d > 1)
                        builder.AppendLine($"**{propS}**: {obj}x");
                    else if (prop != "GamblingMultiplier" && d > 0.01m)
                        builder.AppendLine($"**{propS}**: {(prop == "Cash" ? d.ToString("C2") : d.ToString("0.####"))}");
                    break;
                case int i:
                    if (i > 0) builder.AppendLine($"**{propS}**: {obj}");
                    break;
                default:
                    builder.AppendLine($"**{propS}**: {obj}");
                    break;
            }
        }

        return builder.ToString();
    }
}