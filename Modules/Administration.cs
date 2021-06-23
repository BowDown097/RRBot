using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Google.Cloud.Firestore;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
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

        [Alias("evaluate")]
        [Command("eval")]
        [Summary("Execute C# code.")]
        [Remarks("``$eval [code]``")]
        [RequireOwner]
        public async Task<RuntimeResult> Eval([Remainder] string code)
        {
            try
            {
                code = code.Replace("```cs", "").Trim('`');
                string[] imports = { "System", "System.Collections.Generic", "System.Text" };
                string evaluation = (await CSharpScript.EvaluateAsync(code, ScriptOptions.Default.WithImports(imports), new FunnyContext(Context))).ToString();
                EmbedBuilder embed = new EmbedBuilder
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
            catch (Exception e) when (!(e is NullReferenceException))
            {
                return CommandResult.FromError($"Other error: ``{e.Message}``");
            }
        }

        [Command("giveitem")]
        [Summary("Give a user an item.")]
        [Remarks("``$giveitem [user] item]``")]
        [RequireRole("senateRole")]
        public async Task<RuntimeResult> GiveItem(IGuildUser user, [Remainder] string item)
        {
            if (!CashSystem.itemMap.ContainsValue(item)) return CommandResult.FromError($"{Context.User.Mention}, **{item}** is not a valid item!");
            await CashSystem.RewardItem(user, item);
            await ReplyAsync($"Gave **{user.ToString()}** a(n) **{item}**.");
            return CommandResult.FromSuccess();
        }

        [Command("resetcd")]
        [Summary("Reset your crime cooldowns.")]
        [Remarks("``$resetcd``")]
        [RequireRole("senateRole")]
        public async Task ResetCooldowns()
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            await doc.SetAsync(
            new { rapeCooldown = 0L, whoreCooldown = 0L, lootCooldown = 0L, slaveryCooldown = 0L, mineCooldown = 0L, digCooldown = 0L, chopCooldown = 0L, farmCooldown = 0L, 
                huntCooldown = 0L }, 
                SetOptions.MergeAll);
            await ReplyAsync($"{Context.User.Mention}, your cooldowns have been reset.");
        }

        [Command("setcash")]
        [Summary("Set a user's cash.")]
        [Remarks("``$setcash [user] [amount]``")]
        [RequireRole("senateRole")]
        public async Task SetCash(IGuildUser user, float amount)
        {
            await CashSystem.SetCash(user, amount);
            await ReplyAsync($"Set **{user.ToString()}**'s cash to **{amount.ToString("C2")}**.");
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
