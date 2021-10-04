using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RRBot.Entities;
using RRBot.Extensions;
using RRBot.Systems;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RRBot.Modules
{
    [RequireUserPermission(GuildPermission.Administrator)]
    [Summary("Commands for admin stuff. Whether you wanna screw with the economy or fuck someone over, I'm sure you'll have fun. However, you'll need to have a very high role to have all this fun. Sorry!")]
    public class Administration : ModuleBase<SocketCommandContext>
    {
        [Command("giveitem")]
        [Summary("Give a user an item.")]
        [Remarks("$giveitem [user] [item]")]
        public async Task<RuntimeResult> GiveItem(IGuildUser user, [Remainder] string item)
        {
            if (user.IsBot)
                return CommandResult.FromError("Nope.");
            if (!Items.items.Contains(item))
                return CommandResult.FromError($"**{item}** is not a valid item!");

            DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
            if (dbUser.Items.Contains(item))
                return CommandResult.FromError($"**{user}** already has a(n) {item}.");

            dbUser.Items.Add(item);
            await dbUser.Write();
            await Context.User.NotifyAsync(Context.Channel, $"Gave **{user}** a(n) {item}.");
            return CommandResult.FromSuccess();
        }

        [Command("resetcd")]
        [Summary("Reset a user's crime cooldowns.")]
        [Remarks("$resetcd [user]")]
        public async Task ResetCooldowns(IGuildUser user)
        {
            DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
            foreach (string cmd in Economy.CMDS_WITH_COOLDOWN)
                dbUser[$"{cmd}Cooldown"] = 0L;
            await dbUser.Write();
            await Context.User.NotifyAsync(Context.Channel, $"Reset **{user}**'s cooldowns.");
        }

        [Command("setcash")]
        [Summary("Set a user's cash.")]
        [Remarks("$setcash [user] [amount]")]
        public async Task<RuntimeResult> SetCash(IGuildUser user, double amount)
        {
            if (double.IsNaN(amount) || amount < 0)
                return CommandResult.FromError("You can't set someone's cash to a negative value or NaN!");
            if (user.IsBot)
                return CommandResult.FromError("Nope.");

            DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
            await dbUser.SetCash(user as SocketUser, amount);
            await ReplyAsync($"Set **{user}**'s cash to **{amount:C2}**.");
            await dbUser.Write();
            return CommandResult.FromSuccess();
        }

        [Command("setcrypto")]
        [Summary("Set a user's cryptocurrency amount. See $invest's help info for currently accepted currencies.")]
        [Remarks("$setcrypto [user] [crypto] [amount]")]
        public async Task<RuntimeResult> SetCrypto(IGuildUser user, string crypto, double amount)
        {
            string cUp = crypto.ToUpper();
            if (user.IsBot)
                return CommandResult.FromError("Nope.");
            if (!cUp.In("BTC", "DOGE", "ETH", "LTC", "XRP"))
                return CommandResult.FromError($"**{crypto}** is not a currently accepted currency!");

            DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
            dbUser[cUp] = Math.Round(amount, 4);
            await dbUser.Write();
            await Context.User.NotifyAsync(Context.Channel, $"Added **{amount}** to **{user}**'s {cUp} balance.");
            return CommandResult.FromSuccess();
        }
    }
}
