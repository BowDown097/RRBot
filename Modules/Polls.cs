using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using RRBot.Preconditions;

namespace RRBot.Modules
{
    [RequireStaff]
    public class Polls : ModuleBase<SocketCommandContext>
    {
        public static readonly Dictionary<int, string> numberEmotes = new Dictionary<int, string>
        {
            { 1, "1️⃣" },
            { 2, "2️⃣" },
            { 3, "3️⃣"},
            { 4, "4️⃣"},
            { 5, "5️⃣"},
            { 6, "6️⃣"},
            { 7, "7️⃣"},
            { 8, "8️⃣"},
            { 9, "9️⃣"},
        };

        [Command("createpoll")]
        [Summary("Create a poll.")]
        [Remarks("``$createpoll [title] [choice1|choice2|choice3|...|choice9]``")]
        public async Task<RuntimeResult> CreatePoll(string title, string choices)
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("channels");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("pollsChannel", out ulong id))
            {
                SocketTextChannel pollsChannel = Context.Guild.GetChannel(id) as SocketTextChannel;

                string[] pollChoices = choices.Split('|');
                if (pollChoices.Length > 9) return CommandResult.FromError($"{Context.User.Mention}, a maximum of 9 choices are allowed.");

                StringBuilder choicesStr = new StringBuilder();
                for (int i = 1; i <= pollChoices.Length; i++) choicesStr.AppendLine($"**[{i}]** {pollChoices[i - 1]}");

                EmbedBuilder pollEmbed = new EmbedBuilder
                {
                    Color = Color.Red,
                    Title = title,
                    Description = choicesStr.ToString()
                };

                RestUserMessage pollMsg = await pollsChannel.SendMessageAsync(embed: pollEmbed.Build());

                for (int i = 1; i <= pollChoices.Length; i++) await pollMsg.AddReactionAsync(new Emoji(numberEmotes[i]));

                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError("This server's polls channel has yet to be set.");
        }
    }
}
