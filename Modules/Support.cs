namespace RRBot.Modules
{
    [Summary("Technical support, not from bots and not from creepy dudes out of New Delhi. Your wish is our command!")]
    [RequireBeInChannel("help-requests")]
    [RequireRushReborn]
    public class Support : ModuleBase<SocketCommandContext>
    {
        public static async Task<RuntimeResult> CloseTicket(SocketCommandContext context, SocketUser user,
            DbSupportTicket ticket, string response)
        {
            IUserMessage message = await context.Channel.GetMessageAsync(ticket.Message) as IUserMessage;
            EmbedBuilder embed = message.Embeds.FirstOrDefault().ToEmbedBuilder();
            embed.Description += "\nStatus: Closed";
            await message.ModifyAsync(msg => msg.Embed = embed.Build());

            DbUser dbUser = await DbUser.GetById(context.Guild.Id, user.Id);
            await ticket.Reference.DeleteAsync();
            dbUser.SupportCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(600);
            await dbUser.Write();

            await user.NotifyAsync(context.Channel, response);
            return CommandResult.FromSuccess();
        }

        [Command("close")]
        [Summary("Close your currently active support ticket or a support ticket that you have been assigned to (if there is one).")]
        [Remarks("$close <user>")]
        public async Task<RuntimeResult> Close(IGuildUser user = null)
        {
            DbSupportTicket ticket = await DbSupportTicket.GetById(Context.Guild.Id, user == null ? Context.User.Id : user.Id);
            if (string.IsNullOrWhiteSpace(ticket.Request))
            {
                return CommandResult.FromError(user == null
                    ? "You have yet to open a support ticket. If you wish to open one, you can use ``$support``."
                    : "That user does not have a currently active support ticket.");
            }

            IGuildUser helper = Context.Guild.GetUser(ticket.Helper);
            if (user == null)
                return await CloseTicket(Context, Context.User, ticket, $"Your support ticket with **{helper}** has been closed.");
            else if (helper.Id == Context.User.Id)
                return await CloseTicket(Context, Context.User, ticket, $"Your support ticket with **{user}** has been closed.");
            else
                return CommandResult.FromError("That user has created a support ticket, but you are not assigned as the helper.");
        }

        [Alias("askforhelp")]
        [Command("support")]
        [Summary("Ask for help from a Helper.")]
        [Remarks("$support [request]")]
        [RequireCooldown("SupportCooldown", "You cannot request support again for {0}. This is done to prevent spam.")]
        public async Task<RuntimeResult> GetSupport([Remainder] string request)
        {
            string cleaned = new string(request
                .Where(c => char.IsLetterOrDigit(c) || FilterSystem.NWORD_SPCHARS.Contains(c))
                .ToArray()).ToLower();
            if (FilterSystem.NWORD_REGEX.IsMatch(cleaned))
                return CommandResult.FromError("You cannot have the funny word in your request.");

            QuerySnapshot tickets = await Program.database.Collection($"servers/{Context.Guild.Id}/supportTickets").GetSnapshotAsync();
            DbSupportTicket ticket = await DbSupportTicket.GetById(Context.Guild.Id, Context.User.Id);
            if (!string.IsNullOrWhiteSpace(ticket.Request))
            {
                IGuildUser dbHelper = Context.Guild.GetUser(ticket.Helper);
                return CommandResult.FromError($"You already have a support ticket open with **{dbHelper}**.\n" +
                    "If they have taken an extraordinarily long time to respond, or if the issue has been solved by yourself or someone else, you can use ``$close``.");
            }

            IEnumerable<SocketGuildUser> helpers = Context.Guild.Roles.FirstOrDefault(role => role.Name == "Helper")
                .Members.Where(user => user.Id != Context.User.Id);
            SocketGuildUser helperUser = helpers.ElementAt(RandomUtil.Next(0, helpers.Count()));

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle($"Support Ticket #{tickets.Count + 1}")
                .RRAddField("Issuer", Context.User.Mention)
                .RRAddField("Helper", helperUser.Mention)
                .RRAddField("Request", request);

            IUserMessage userMessage = await ReplyAsync($"{helperUser.Mention}, someone needs some help!", embed: embed.Build());
            ticket.Helper = helperUser.Id;
            ticket.Issuer = Context.User.Id;
            ticket.Message = userMessage.Id;
            ticket.Request = request;
            await ticket.Write();
            return CommandResult.FromSuccess();
        }

        [Command("tickets")]
        [Summary("Check the amount of currently open support tickets.")]
        [Remarks("$tickets")]
        public async Task Tickets()
        {
            QuerySnapshot tickets = await Program.database.Collection($"servers/{Context.Guild.Id}/supportTickets").GetSnapshotAsync();
            await Context.User.NotifyAsync(Context.Channel, tickets.Documents.Count == 1
                ? "There is currently 1 open support ticket."
                : $"There are currently {tickets.Documents.Count} open support tickets.");
        }

        [Command("viewticket")]
        [Summary("View a user's support ticket, if they have one that is opened.")]
        [Remarks("$viewticket [user]")]
        public async Task<RuntimeResult> ViewTicket(IGuildUser user)
        {
            DbSupportTicket ticket = await DbSupportTicket.GetById(Context.Guild.Id, user.Id);
            if (string.IsNullOrWhiteSpace(ticket.Request))
                return CommandResult.FromError("That user does not have an open support ticket!");

            SocketGuildUser helper = Context.Guild.GetUser(ticket.Helper);
            SocketGuildUser issuer = Context.Guild.GetUser(ticket.Issuer);

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle($"Support Ticket from {issuer}")
                .RRAddField("Helper", helper.Mention)
                .RRAddField("Request", ticket.Request);
            await ReplyAsync(embed: embed.Build());
            return CommandResult.FromSuccess();
        }
    }
}
