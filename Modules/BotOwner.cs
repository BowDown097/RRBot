using Discord;
using Discord.Commands;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using RRBot.Entities;
using RRBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RRBot.Modules
{
    [RequireOwner]
    [Summary("Commands for bot owners only.")]
    public class BotOwner : ModuleBase<SocketCommandContext>
    {
        public CommandService Commands { get; set; }

        [Alias("botban")]
        [Command("blacklist")]
        [Summary("Ban a user from using the bot.")]
        [Remarks("$blacklist [user]")]
        public async Task<RuntimeResult> Blacklist(IGuildUser user)
        {
            if (user.IsBot)
                return CommandResult.FromError("Nope.");

            DbGlobalConfig globalConfig = await DbGlobalConfig.Get();
            globalConfig.BannedUsers.Add(user.Id);
            await globalConfig.Write();

            await Context.User.NotifyAsync(Context.Channel, $"Blacklisted {user}.");
            return CommandResult.FromSuccess();
        }

        [Command("disablecmd")]
        [Summary("Disable a command.")]
        [Remarks("$disablecmd [cmd]")]
        public async Task<RuntimeResult> DisableCommand(string cmd)
        {
            string cmdLower = cmd.ToLower();
            CommandInfo cmdInfo = Commands.Commands.FirstOrDefault(info => info.Name == cmdLower);
            if (cmdInfo == default)
                return CommandResult.FromError($"**{cmdLower}** is not a command!");

            DbGlobalConfig globalConfig = await DbGlobalConfig.Get();
            globalConfig.DisabledCommands.Add(cmdLower);
            await globalConfig.Write();

            await Context.User.NotifyAsync(Context.Channel, $"Disabled ${cmdLower}.");
            return CommandResult.FromSuccess();
        }

        [Command("enablecmd")]
        [Summary("Enable a previously disabled command.")]
        [Remarks("$enablecmd [cmd]")]
        public async Task<RuntimeResult> EnableCommand(string cmd)
        {
            string cmdLower = cmd.ToLower();
            DbGlobalConfig globalConfig = await DbGlobalConfig.Get();
            if (!globalConfig.DisabledCommands.Contains(cmd))
                return CommandResult.FromError($"**{cmdLower}** is either not a command or is not disabled!");

            globalConfig.DisabledCommands.Remove(cmdLower);
            await globalConfig.Write();

            await Context.User.NotifyAsync(Context.Channel, $"Enabled ${cmdLower}.");
            return CommandResult.FromSuccess();
        }

        [Alias("evaluate")]
        [Command("eval")]
        [Summary("Execute C# code.")]
        [Remarks("$eval [code]")]
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

        [Alias("unbotban")]
        [Command("unblacklist")]
        [Summary("Unban a user from using the bot.")]
        [Remarks("$unblacklist [user]")]
        public async Task<RuntimeResult> Unblacklist(IGuildUser user)
        {
            if (user.IsBot)
                return CommandResult.FromError("Nope.");

            DbGlobalConfig globalConfig = await DbGlobalConfig.Get();
            globalConfig.BannedUsers.Remove(user.Id);
            await globalConfig.Write();

            await Context.User.NotifyAsync(Context.Channel, $"Unblacklisted {user}.");
            return CommandResult.FromSuccess();
        }
    }
}