namespace RRBot.Modules;
[Summary("Choose how you want me to bug you. I can do it in DM, I can do it when you rank up, and I can even ping you, too.")]
public class UserSettings : ModuleBase<SocketCommandContext>
{
    [Command("mysettings")]
    [Summary("List your user settings.")]
    public async Task MySettings()
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Your Settings")
            .RRAddField("DM Notifications", user.DMNotifs)
            .RRAddField("Reply Pings", user.WantsReplyPings);
        await ReplyAsync(embed: embed.Build());
    }

    [Alias("toggledmnotifs")]
    [Command("toggledmnotifications")]
    [Summary("Toggle whether or not you will be DM'd by commands/general notifications that support it. *(default: false)*")]
    public async Task ToggleDMNotifications()
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        user.DMNotifs = !user.DMNotifs;
        await Context.User.NotifyAsync(Context.Channel, $"You will {(user.DMNotifs ? "now see" : "no longer see")} DM notifications.");
    }

    [Command("togglereplypings")]
    [Summary("Set whether or not you will be pinged in command responses. *(default: true)*")]
    [Remarks("$togglereplypings")]
    public async Task ToggleReplyPings()
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        user.WantsReplyPings = !user.WantsReplyPings;
        await Context.User.NotifyAsync(Context.Channel, $"You will {(user.WantsReplyPings ? "now be" : "no longer be")} pinged in command responses.");
    }
}