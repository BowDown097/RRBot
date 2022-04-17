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
            .RRAddField("No Reply Pings", user.NoReplyPings);
        await ReplyAsync(embed: embed.Build());
    }

    [Alias("setdmnotifs")]
    [Command("setdmnotifications")]
    [Summary("Set whether or not you will be DM'd by commands/general notifications that support it. *(default: false)*")]
    [Remarks("$setdmnotifications true")]
    public async Task SetDMNotifications(bool status)
    {
        await GenericSet("DMNotifs", status);
        await Context.User.NotifyAsync(Context.Channel, $"You will {(status ? "now see" : "no longer see")} DM notifications.");
    }

    [Command("setnoreplypings")]
    [Summary("Set whether or not you will be pinged in command responses. *(default: false)*")]
    [Remarks("$setnoreplypings false")]
    public async Task SetNoReplyPings(bool status)
    {
        await GenericSet("NoReplyPings", status);
        await Context.User.NotifyAsync(Context.Channel, $"You will {(status ? "no longer be" : "now be")} pinged in command responses.");
    }

    private async Task GenericSet(string property, bool status)
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        user[property] = status;
    }
}