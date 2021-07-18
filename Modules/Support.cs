using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using RRBot.Extensions;
using RRBot.Preconditions;
using RRBot.Systems;

namespace RRBot.Modules
{
    [RequireBeInChannel("help-requests")]
    [RequireRushReborn]
    public class Support : ModuleBase<SocketCommandContext>
    {
        public Logger Logger { get; set; }
        public static readonly Random random = new Random();

        [Command("end")]
        [Summary("End your currently active support ticket or a support ticket that you have been assigned to (if there is one).")]
        [Remarks("``$end <user>``")]
        public async Task<RuntimeResult> End(IGuildUser user = null)
        {
            ulong targetUserId = user == null ? Context.User.Id : user.Id;

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/supportTickets").Document(targetUserId.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (!snap.Exists)
            {
                return CommandResult.FromError(user == null
                    ? $"{Context.User.Mention}, you have yet to open a support ticket. If you wish to open one, you can use ``$support``."
                    : $"{Context.User.Mention}, that user does not have a currently active support ticket.");
            }

            IGuildUser helper = Context.Guild.GetUser(snap.GetValue<ulong>("helper"));
            if (user == null)
            {
                await doc.DeleteAsync();
                await Context.User.NotifyAsync(Context.Channel, $"Your support ticket with **{helper.ToString()}** has been closed.");
            }
            else if (helper.Id == Context.User.Id)
            {
                await doc.DeleteAsync();
                await Context.User.NotifyAsync(Context.Channel, $"Your support ticket with **{user.ToString()}** has been closed.");
            }
            else
            {
                await Context.User.NotifyAsync(Context.Channel, "That user has created a support ticket, but you are not assigned as the helper.");
            }

            return CommandResult.FromSuccess();
        }

        [Alias("askforhelp")]
        [Command("support")]
        [Summary("Ask for help from a Helper.")]
        [Remarks("``$support [request]``")]
        public async Task<RuntimeResult> GetSupport([Remainder] string request)
        {
            CollectionReference tickets = Program.database.Collection($"servers/{Context.Guild.Id}/supportTickets");
            DocumentReference doc = tickets.Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
            {
                IGuildUser dbHelper = Context.Guild.GetUser(snap.GetValue<ulong>("helper"));
                return CommandResult.FromError($"{Context.User.Mention}, you already have a support ticket open with **{dbHelper.ToString()}**.\n" +
                    "If they have taken an extraordinarily long time to respond, or if the issue has been solved by yourself or someone else, you can use ``$end``.");
            }

            IEnumerable<SocketGuildUser> helpers = Context.Guild.Roles.FirstOrDefault(role => role.Name == "Helper").Members.Where(user => user.Id != Context.User.Id);
            SocketGuildUser helperUser = helpers.ElementAt(random.Next(0, helpers.Count()));
            await doc.SetAsync(new { expiration = DateTimeOffset.UtcNow.ToUnixTimeSeconds(7200), helper = helperUser.Id });

            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Red,
                Title = $"Support Ticket #{await tickets.ListDocumentsAsync().CountAsync()}",
                Description = $"Issuer: {Context.User.Mention}\nHelper: {helperUser.Mention}\nRequest: {request}"
            };

            await ReplyAsync($"{helperUser.Mention}, someone needs some help!", embed: embed.Build());
            return CommandResult.FromSuccess();
        }
    }
}
