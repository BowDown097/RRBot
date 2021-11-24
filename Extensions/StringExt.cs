namespace RRBot.Extensions
{
    public static class StringExt
    {
        private static readonly Regex reg = new("([a-z,0-9](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", RegexOptions.Compiled);

        public static bool In(this string source, params string[] list)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            return list.Contains(source);
        }

        public static string SplitPascalCase(this string source) => reg.Replace(source, "$1 ");
    }
}