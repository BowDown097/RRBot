using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using RRBot.Preconditions;

namespace RRBot.Modules
{
    public class Debate : ModuleBase<SocketCommandContext>
    {
        public static bool ongoingDebate, endingDebate;

        [Command("startdebate")]
        [Alias("debate")]
        [Summary("Start up a debate in #debate. Debate Team will be pinged.")]
        [Remarks("``$startdebate [topic]``")]
        [RequireBeInChannel("debate")]
        [RequireRole("debateRole")]
        public async Task<RuntimeResult> StartDebate([Remainder] string topic = "")
        {
            if (string.IsNullOrEmpty(topic)) return CommandResult.FromError($"{Context.User.Mention} give me a topic!");
            if (ongoingDebate) return CommandResult.FromError($"{Context.User.Mention} there is an ongoing debate! If you believe there isn't one, do ``$enddebate``.");

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("roles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("debateRole", out ulong id))
            {
                await ReplyAsync($"{Context.Guild.GetRole(id).Mention} {Context.User.Mention} has called for a DEBATE on " +
                    $"{topic.Replace("@everyone", "fuckyou")}. Begin!");
                ongoingDebate = true;
                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError("This server's debate role has yet to be set.");
        }

        [Command("enddebate")]
        [Summary("Call for an end to the ongoing debate in #debate by vote. Chat will be purged!")]
        [Remarks("``$enddebate``")]
        [RequireBeInChannel("debate")]
        [RequireRole("debateRole")]
        public async Task<RuntimeResult> EndDebate()
        {
            if (!ongoingDebate) return CommandResult.FromError($"{Context.User.Mention} there is not an ongoing debate! If you want to start one, do ``$startdebate [topic]``.");
            if (endingDebate) return CommandResult.FromError($"{Context.User.Mention} a request has already been made to end this debate.");

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("roles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("debateRole", out ulong id))
            {
                endingDebate = true;
                IUserMessage endMsg = await ReplyAsync($"{Context.Guild.GetRole(id).Mention} {Context.User.Mention} is calling for an end to this chaos!" +
                $" React with {new Emoji("\uD83D\uDC4D")} if you wish to as well. Three votes is a win!");
                    
                Global.RunInBackground(() =>
                {
                    // are we waiting?
                    bool waiting = true;
                    while (waiting)
                    {
                        var reactors = endMsg.GetReactionUsersAsync(new Emoji("\uD83D\uDC4D"), 5).FlattenAsync().Result as ICollection<IUser>;
                        waiting = reactors.Count < 3;
                        Thread.Sleep(TimeSpan.FromSeconds(5));
                    }

                    // we aren't waiting anymore, time to end!
                    ReplyAsync("Alright. The debate is over! Chat will be purged shortly.");
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                    var msgs = Context.Channel.GetMessagesAsync(1000).FlattenAsync().Result;
                    msgs = msgs.Where(msg => (DateTimeOffset.UtcNow - msg.Timestamp).TotalDays <= 14);
                    (Context.Channel as SocketTextChannel).DeleteMessagesAsync(msgs);
                    ongoingDebate = endingDebate = false;
                });

                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError("This server's debate role has yet to be set.");
        }
    }
}
