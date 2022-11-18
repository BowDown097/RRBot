namespace RRBot.Extensions;
public static class ChannelExt
{
    public static IGuild GetGuild(this IChannel channel) => (channel as IGuildChannel)?.Guild;
    // used for channels that do not have a Mention property (not sure why some don't)
    public static string Mention(this IChannel channel) => MentionUtils.MentionChannel(channel.Id);
}