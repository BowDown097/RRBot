namespace RRBot.Extensions;
public static class ChannelExt
{
    public static IGuild GetGuild(this IChannel channel) => (channel as IGuildChannel)?.Guild;
}