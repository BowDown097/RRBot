namespace RRBot.Extensions
{
    public static class GuildUserExt
    {
        public static string Sanitize(this IGuildUser user) => RRFormat.Sanitize(user);
    }
}