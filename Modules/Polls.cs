namespace RRBot.Modules
{
    [Summary("Self explanatory.")]
    [RequireStaff]
    public class Polls : ModuleBase<SocketCommandContext>
    {
        [Command("createpoll")]
        [Summary("Create a poll.")]
        [Remarks("$createpoll [title] [choice1|choice2|choice3|...|choice9]")]
        public async Task<RuntimeResult> CreatePoll(string title, string choices)
        {
            DbConfigChannels channels = await DbConfigChannels.GetById(Context.Guild.Id);
            if (Context.Guild.TextChannels.Any(channel => channel.Id == channels.PollsChannel))
            {
                SocketTextChannel pollsChannel = Context.Guild.GetTextChannel(channels.PollsChannel);

                string[] pollChoices = choices.Split('|');
                if (pollChoices.Length > 9)
                    return CommandResult.FromError("A maximum of 9 choices are allowed.");

                StringBuilder choicesStr = new();
                for (int i = 1; i <= pollChoices.Length; i++)
                    choicesStr.AppendLine($"**[{i}]** {pollChoices[i - 1]}");

                EmbedBuilder pollEmbed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithTitle(title)
                    .WithDescription(choicesStr.ToString());

                RestUserMessage pollMsg = await pollsChannel.SendMessageAsync(embed: pollEmbed.Build());
                for (int i = 1; i <= pollChoices.Length; i++)
                    await pollMsg.AddReactionAsync(new Emoji(Constants.POLL_EMOTES[i]));

                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError("This server's polls channel has yet to be set or no longer exists.");
        }
    }
}
