namespace RRBot.Extensions;
public static class ChannelExt
{
    // used for channels that do not have a Mention property (not sure why some don't)
    public static string Mention(this IChannel channel) => MentionUtils.MentionChannel(channel.Id);
}