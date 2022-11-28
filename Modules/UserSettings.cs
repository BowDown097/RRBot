namespace RRBot.Modules;
[Summary("Choose how you want me to bug you. I can do it in DM, I can do it when you rank up, and I can even ping you, too.")]
public class UserSettings : ModuleBase<SocketCommandContext>
{
    [Command("mysettings")]
    [Summary("List your user settings.")]
    public async Task MySettings()
    {
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Your Settings")
            .RrAddField("DM Notifications", user.DmNotifs)
            .RrAddField("Reply Pings", user.WantsReplyPings);
        await ReplyAsync(embed: embed.Build());
    }

    [Alias("toggledmnotifs")]
    [Command("toggledmnotifications")]
    [Summary("Toggle whether or not you will be DM'd by commands/general notifications that support it. *(default: false)*")]
    public async Task ToggleDmNotifications()
    {
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        user.DmNotifs = !user.DmNotifs;
        await Context.User.NotifyAsync(Context.Channel, $"You will {(user.DmNotifs ? "now see" : "no longer see")} DM notifications.");
        await MongoManager.UpdateObjectAsync(user);
    }

    [Command("togglereplypings")]
    [Summary("Set whether or not you will be pinged in command responses. *(default: true)*")]
    [Remarks("$togglereplypings")]
    public async Task ToggleReplyPings()
    {
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        user.WantsReplyPings = !user.WantsReplyPings;
        await Context.User.NotifyAsync(Context.Channel, $"You will {(user.WantsReplyPings ? "now be" : "no longer be")} pinged in command responses.");
        await MongoManager.UpdateObjectAsync(user);
    }
}