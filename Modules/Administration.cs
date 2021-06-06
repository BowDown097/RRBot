using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Google.Cloud.Firestore;
using RRBot.Preconditions;
using RRBot.Systems;

namespace RRBot.Modules
{
    public class Administration : ModuleBase<SocketCommandContext>
    {
        [Alias("botban")]
        [Command("blacklist")]
        [Summary("Ban a user from using the bot.")]
        [Remarks("``$blacklist [user]``")]
        [RequireOwner]
        public async Task BotBan(IGuildUser user)
        {
            if (user.IsBot)
            {
                await ReplyAsync("Nope.");
                return;
            }

            DocumentReference doc = Program.database.Collection("globalConfig").Document(user.Id.ToString());
            await doc.SetAsync(new { banned = true }, SetOptions.MergeAll);
            await ReplyAsync($"Blacklisted **{user.ToString()}**.");
        }

        [Command("resetcd")]
        [Summary("Reset your crime cooldowns.")]
        [Remarks("``$resetcd``")]
        [RequireRole("senateRole")]
        public async Task ResetCooldowns()
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            await doc.SetAsync(new { rapeCooldown = 0L, whoreCooldown = 0L, lootCooldown = 0L }, SetOptions.MergeAll);
            await ReplyAsync($"{Context.User.Mention}, your cooldowns have been reset.");
        }

        [Command("setcash")]
        [Summary("Set a user's cash.")]
        [Remarks("``$setcash [user] [amount]``")]
        [RequireRole("senateRole")]
        public async Task SetCash(IGuildUser user, float amount)
        {
            amount = (float)Math.Round(amount, 2);
            await CashSystem.SetCash(user, amount);
            await ReplyAsync($"Set **{user.ToString()}**'s cash to **${amount}**.");
        }

        [Alias("unbotban")]
        [Command("unblacklist")]
        [Summary("Unban a user from using the bot.")]
        [Remarks("``$unblacklist [user]``")]
        [RequireOwner]
        public async Task UnBotBan(IGuildUser user)
        {
            if (user.IsBot)
            {
                await ReplyAsync("Nope.");
                return;
            }

            DocumentReference doc = Program.database.Collection("globalConfig").Document(user.Id.ToString());
            await doc.DeleteAsync();
            await ReplyAsync($"Unblacklisted **{user.ToString()}**.");
        }
    }
}
