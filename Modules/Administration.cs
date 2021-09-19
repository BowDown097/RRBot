using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using RRBot.Entities;
using RRBot.Extensions;
using RRBot.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RRBot.Modules
{
    [Summary("Commands for admin stuff. Whether you wanna screw with the economy or completely blacklist people from using the bot, I'm sure you'll have fun. However, you'll need to be a Bot Owner or have a very high role to have all this fun. Sorry!")]
    public class Administration : ModuleBase<SocketCommandContext>
    {
        public List<ulong> BannedUsers { get; set; }

        [Alias("botban")]
        [Command("blacklist")]
        [Summary("Ban a user from using the bot.")]
        [Remarks("$blacklist [user]")]
        [RequireOwner]
        public async Task<RuntimeResult> BotBan(IGuildUser user)
        {
            if (user.IsBot)
                return CommandResult.FromError("Nope.");

            DocumentReference doc = Program.database.Collection("globalConfig").Document(user.Id.ToString());
            await doc.SetAsync(new { banned = true }, SetOptions.MergeAll);
            await Context.User.NotifyAsync(Context.Channel, $"Blacklisted **{user}**.");
            BannedUsers.Add(user.Id);

            return CommandResult.FromSuccess();
        }

        [Alias("evaluate")]
        [Command("eval")]
        [Summary("Execute C# code.")]
        [Remarks("$eval [code]")]
        [RequireOwner]
        public async Task<RuntimeResult> Eval([Remainder] string code)
        {
            try
            {
                code = code.Replace("```cs", "").Trim('`');
                string[] imports = { "System", "System.Collections.Generic", "System.Text" };
                string evaluation = (await CSharpScript.EvaluateAsync(code, ScriptOptions.Default.WithImports(imports),
                    new FunnyContext(Context))).ToString();
                EmbedBuilder embed = new()
                {
                    Color = Color.Red,
                    Title = "Code evaluation",
                    Description = $"Your code, ```cs\n{code}``` evaluates to: ```cs\n\"{evaluation}\"```"
                };
                await ReplyAsync(embed: embed.Build());
                return CommandResult.FromSuccess();
            }
            catch (CompilationErrorException cee)
            {
                return CommandResult.FromError($"Compilation error: ``{cee.Message}``");
            }
            catch (Exception e) when (e is not NullReferenceException)
            {
                return CommandResult.FromError($"Other error: ``{e.Message}``");
            }
        }

        [Command("giveitem")]
        [Summary("Give a user an item.")]
        [Remarks("$giveitem [user] [item]")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task<RuntimeResult> GiveItem(IGuildUser user, [Remainder] string item)
        {
            if (user.IsBot)
                return CommandResult.FromError("Nope.");
            if (!Items.items.Contains(item))
                return CommandResult.FromError($"**{item}** is not a valid item!");

            DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
            if (dbUser.Items?.Contains(item) == true)
                return CommandResult.FromError($"**{user}** already has a(n) {item}.");

            dbUser.Items.Add(item);
            await dbUser.Write();
            await Context.User.NotifyAsync(Context.Channel, $"Gave **{user}** a(n) {item}.");
            return CommandResult.FromSuccess();
        }

        [Command("resetcd")]
        [Summary("Reset your crime cooldowns.")]
        [Remarks("$resetcd")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ResetCooldowns()
        {
            DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
            foreach (string cmd in Economy.CMDS_WITH_COOLDOWN)
                user[$"{cmd}Cooldown"] = 0L;
            await user.Write();
            await Context.User.NotifyAsync(Context.Channel, "Your cooldowns have been reset.");
        }

        [Command("setcash")]
        [Summary("Set a user's cash.")]
        [Remarks("$setcash [user] [amount]")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task<RuntimeResult> SetCash(IGuildUser user, double amount)
        {
            DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
            if (user.IsBot)
                return CommandResult.FromError("Nope.");
            await dbUser.SetCash(user as SocketUser, Context.Channel, amount);
            await ReplyAsync($"Set **{user}**'s cash to **{amount:C2}**.");

            await dbUser.Write();
            return CommandResult.FromSuccess();
        }

        [Command("setcrypto")]
        [Summary("Set a user's cryptocurrency amount. See $invest's help info for currently accepted currencies.")]
        [Remarks("$setcrypto [user] [crypto] [amount]")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task<RuntimeResult> SetCrypto(IGuildUser user, string crypto, double amount)
        {
            DbUser dbUser = await DbUser.GetById(Context.Guild.Id, user.Id);
            string cUp = crypto.ToUpper();

            if (user.IsBot)
                return CommandResult.FromError("Nope.");
            if (cUp != "BTC" && cUp != "DOGE" && cUp != "ETH" && cUp != "LTC" && cUp != "XRP")
                return CommandResult.FromError($"**{crypto}** is not a currently accepted currency!");

            dbUser[cUp] = Math.Round(amount, 4);
            await dbUser.Write();
            await Context.User.NotifyAsync(Context.Channel, $"Added **{amount}** to **{user}**'s {cUp} balance.");
            return CommandResult.FromSuccess();
        }

        [Alias("unbotban")]
        [Command("unblacklist")]
        [Summary("Unban a user from using the bot.")]
        [Remarks("$unblacklist [user]")]
        [RequireOwner]
        public async Task<RuntimeResult> UnBotBan(IGuildUser user)
        {
            if (user.IsBot)
                return CommandResult.FromError("Nope.");

            DocumentReference doc = Program.database.Collection("globalConfig").Document(user.Id.ToString());
            await doc.DeleteAsync();
            await ReplyAsync($"Unblacklisted **{user}**.");
            BannedUsers.Remove(user.Id);

            return CommandResult.FromSuccess();
        }
    }
}
