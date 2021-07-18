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

        public static async Task CloseTicket(ISocketMessageChannel helpRequests, SocketUser user, DocumentReference doc, DocumentSnapshot snap, string response)
        {
            IUserMessage message = await helpRequests.GetMessageAsync(snap.GetValue<ulong>("message")) as IUserMessage;
            EmbedBuilder embed = message.Embeds.FirstOrDefault().ToEmbedBuilder();
            embed.Description += "\nStatus: Closed";
            await message.ModifyAsync(msg => msg.Embed = embed.Build());

            await doc.DeleteAsync();
            await user.NotifyAsync(helpRequests, response);
        }

        [Command("close")]
        [Summary("Close your currently active support ticket or a support ticket that you have been assigned to (if there is one).")]
        [Remarks("``$close <user>``")]
        public async Task<RuntimeResult> Close(IGuildUser user = null)
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
                await CloseTicket(Context.Channel, Context.User, doc, snap, $"Your support ticket with **{helper.ToString()}** has been closed.");
            else if (helper.Id == Context.User.Id)
                await CloseTicket(Context.Channel, Context.User, doc, snap, $"Your support ticket with **{user.ToString()}** has been closed.");
            else
                await Context.User.NotifyAsync(Context.Channel, "That user has created a support ticket, but you are not assigned as the helper.");

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

            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Red,
                Title = $"Support Ticket #{await tickets.ListDocumentsAsync().CountAsync() + 1}",
                Description = $"Issuer: {Context.User.Mention}\nHelper: {helperUser.Mention}\nRequest: {request}"
            };

            IUserMessage userMessage = await ReplyAsync($"{helperUser.Mention}, someone needs some help!", embed: embed.Build());
            await doc.SetAsync(new { helper = helperUser.Id, issuer = Context.User.Id, message = userMessage.Id, req = request });
            return CommandResult.FromSuccess();
        }

        [Command("tickets")]
        [Summary("Check the amount of currently open support tickets.")]
        [Remarks("``$tickets``")]
        public async Task Tickets()
        {
            CollectionReference tickets = Program.database.Collection($"servers/{Context.Guild.Id}/supportTickets");
            int ticketsCount = await tickets.ListDocumentsAsync().CountAsync();
            await Context.User.NotifyAsync(Context.Channel, ticketsCount == 1
                ? "There is currently 1 open support ticket."
                : $"There are currently {ticketsCount} open support tickets.");
        }

        [Command("viewticket")]
        [Summary("View a currently open ticket.")]
        [Remarks("``$viewticket [index]``")]
        public async Task<RuntimeResult> ViewTicket(int index)
        {
            CollectionReference ticketsCollection = Program.database.Collection($"servers/{Context.Guild.Id}/supportTickets");
            IAsyncEnumerable<DocumentReference> tickets = ticketsCollection.ListDocumentsAsync();
            if (index > await tickets.CountAsync() || index <= 0) return CommandResult.FromError($"{Context.User.Mention}, there is no support ticket at that index!");

            DocumentReference ticketDoc = await tickets.ElementAtAsync(index - 1);
            DocumentSnapshot ticket = await ticketDoc.GetSnapshotAsync();

            SocketGuildUser helper = Context.Guild.GetUser(ticket.GetValue<ulong>("helper"));
            SocketGuildUser issuer = Context.Guild.GetUser(ticket.GetValue<ulong>("issuer"));
            string request = ticket.GetValue<string>("req");
            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Red,
                Title = $"Support Ticket #{index}",
                Description = $"Issuer: {issuer.Mention}\nHelper: {helper.Mention}\nRequest: {request}"
            };

            await ReplyAsync(embed: embed.Build());
            return CommandResult.FromSuccess();
        }
    }
}
