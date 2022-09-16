namespace RRBot.Modules;
[Summary("Technical support, not from bots and not from creepy dudes out of New Delhi. Your wish is our command!")]
[RequireBeInChannel("help-requests")]
[RequireRushReborn]
public class Support : ModuleBase<SocketCommandContext>
{
    #region Commands
    [Command("close")]
    [Summary("Close your currently active support ticket or a support ticket that you have been assigned to (if there is one).")]
    [Remarks("$close BlazeItGhey")]
    public async Task<RuntimeResult> Close([Remainder] IGuildUser user = null)
    {
        DbSupportTicket ticket = await DbSupportTicket.GetById(Context.Guild.Id, user?.Id ?? Context.User.Id);
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
            return await CloseTicket(Context, Context.User, ticket, $"Your support ticket with **{user.Sanitize()}** has been closed.");
        else
            return CommandResult.FromError("That user has created a support ticket, but you are not assigned as the helper.");
    }

    [Alias("askforhelp")]
    [Command("support")]
    [Summary("Ask for help from a Helper.")]
    [Remarks("$support I dropped the toaster in the bath, what was that lightning stuff?")]
    [RequireCooldown("SupportCooldown", "You cannot request support again for {0}. This is done to prevent spam.")]
    public async Task<RuntimeResult> GetSupport([Remainder] string request)
    {
        if (await FilterSystem.ContainsFilteredWord(Context.Guild, request))
            return CommandResult.FromError("Nope.");

        DbSupportTicket ticket = await DbSupportTicket.GetById(Context.Guild.Id, Context.User.Id);
        int tickets = await ticket.Reference.Parent.ListDocumentsAsync().CountAsync();
        if (!string.IsNullOrWhiteSpace(ticket.Request))
        {
            IGuildUser dbHelper = Context.Guild.GetUser(ticket.Helper);
            return CommandResult.FromError($"You already have a support ticket open with **{dbHelper}**.\n" +
                "If they have taken an extraordinarily long time to respond, or if the issue has been solved by yourself or someone else, you can use ``$close``.");
        }

        List<SocketGuildUser> helpers = Context.Guild.Roles.FirstOrDefault(role => role.Name == "Helper")?.Members
            .Where(user => user.Id != Context.User.Id).ToList();
        if (helpers == null || helpers.Count == 0)
            return CommandResult.FromError("There are no helpers! Unfortunate.");

        SocketGuildUser helperUser = helpers[RandomUtil.Next(0, helpers.Count)];

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle($"Support Ticket #{tickets + 1}")
            .RrAddField("Issuer", Context.User.Mention)
            .RrAddField("Helper", helperUser.Mention)
            .RrAddField("Request", request);

        IUserMessage userMessage = await ReplyAsync($"{helperUser.Mention}, someone needs some help!", embed: embed.Build(), allowedMentions: Constants.Mentions);
        ticket.Helper = helperUser.Id;
        ticket.Issuer = Context.User.Id;
        ticket.Message = userMessage.Id;
        ticket.Request = request;
        return CommandResult.FromSuccess();
    }

    [Command("tickets")]
    [Summary("Check the amount of currently open support tickets.")]
    public async Task Tickets()
    {
        QuerySnapshot tickets = await Program.Database.Collection($"servers/{Context.Guild.Id}/supportTickets").GetSnapshotAsync();
        await Context.User.NotifyAsync(Context.Channel, tickets.Documents.Count == 1
            ? "There is currently 1 open support ticket."
            : $"There are currently {tickets.Documents.Count} open support tickets.");
    }

    [Command("viewticket")]
    [Summary("View a user's support ticket, if they have one that is opened.")]
    [Remarks("$viewticket Sylent")]
    public async Task<RuntimeResult> ViewTicket([Remainder] IGuildUser user)
    {
        DbSupportTicket ticket = await DbSupportTicket.GetById(Context.Guild.Id, user.Id);
        if (string.IsNullOrWhiteSpace(ticket.Request))
            return CommandResult.FromError("That user does not have an open support ticket!");

        SocketGuildUser helper = Context.Guild.GetUser(ticket.Helper);
        SocketGuildUser issuer = Context.Guild.GetUser(ticket.Issuer);

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle($"Support Ticket from {issuer}")
            .RrAddField("Helper", helper.Mention)
            .RrAddField("Request", ticket.Request);
        await ReplyAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }
    #endregion

    #region Helpers
    public static async Task<RuntimeResult> CloseTicket(SocketCommandContext context, SocketUser user,
        DbSupportTicket ticket, string response)
    {
        if (await context.Channel.GetMessageAsync(ticket.Message) is not IUserMessage message)
            return CommandResult.FromError("Failed to get support ticket.");

        EmbedBuilder embed = message.Embeds.FirstOrDefault().ToEmbedBuilder();
        embed.Description += "\nStatus: Closed";
        await message.ModifyAsync(msg => msg.Embed = embed.Build());

        DbUser dbUser = await DbUser.GetById(context.Guild.Id, user.Id);
        await dbUser.SetCooldown("SupportCooldown", 600, context.Guild, user);
        await ticket.Reference.DeleteAsync();

        await user.NotifyAsync(context.Channel, response);
        return CommandResult.FromSuccess();
    }
    #endregion
}