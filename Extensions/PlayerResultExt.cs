namespace RRBot.Extensions;
public static class PlayerResultExt
{
    public static string ErrorMessage<T>(this PlayerResult<T> playerResult) where T : class, ILavalinkPlayer
    {
        return playerResult.Status switch
        {
            PlayerRetrieveStatus.BotNotConnected => "The bot is not currently being used.",
            PlayerRetrieveStatus.UserNotInVoiceChannel => "You must be in a voice channel.",
            PlayerRetrieveStatus.VoiceChannelMismatch => "You must be in the same voice channel as the bot.",
            
            PlayerRetrieveStatus.PreconditionFailed when Equals(playerResult.Precondition, PlayerPrecondition.Playing) => "There is nothing currently playing.",
            PlayerRetrieveStatus.PreconditionFailed when Equals(playerResult.Precondition, PlayerPrecondition.QueueNotEmpty) => "The queue is empty.",

            _ => "Encountered unknown error retrieving player."
        };
    }
}