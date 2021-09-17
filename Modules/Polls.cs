using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using RRBot.Preconditions;
using System.Text;
using System.Threading.Tasks;

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
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("channels");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("pollsChannel", out ulong id))
            {
                SocketTextChannel pollsChannel = Context.Guild.GetChannel(id) as SocketTextChannel;

                string[] pollChoices = choices.Split('|');
                if (pollChoices.Length > 9)
                    return CommandResult.FromError("A maximum of 9 choices are allowed.");

                StringBuilder choicesStr = new();
                for (int i = 1; i <= pollChoices.Length; i++)
                    choicesStr.AppendLine($"**[{i}]** {pollChoices[i - 1]}");

                EmbedBuilder pollEmbed = new()
                {
                    Color = Color.Red,
                    Title = title,
                    Description = choicesStr.ToString()
                };

                RestUserMessage pollMsg = await pollsChannel.SendMessageAsync(embed: pollEmbed.Build());
                for (int i = 1; i <= pollChoices.Length; i++)
                    await pollMsg.AddReactionAsync(new Emoji(Constants.POLL_EMOTES[i]));

                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError("This server's polls channel has yet to be set.");
        }
    }
}
