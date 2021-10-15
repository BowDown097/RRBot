using Discord;
using Discord.Commands;
using Google.Cloud.Firestore;
using RRBot.Entities;
using RRBot.Extensions;
using RRBot.Preconditions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RRBot.Modules
{
    [Summary("The name really explains it all. Fun fact, you used one of the commands under this module to view info about this module.")]
    public class General : ModuleBase<SocketCommandContext>
    {
        public CommandService Commands { get; set; }

        [Alias("ach")]
        [Command("achievements")]
        [Summary("View your own or someone else's achievements.")]
        [Remarks("$achievements <user>")]
        public async Task Achievements(IGuildUser user = null)
        {
            ulong userId = user != null ? user.Id : Context.User.Id;
            DbUser dbUser = await DbUser.GetById(Context.Guild.Id, userId);
            StringBuilder description = new();
            foreach (KeyValuePair<string, string> achievement in dbUser.Achievements)
                description.AppendLine($"**{achievement.Key}**: {achievement.Value}");

            EmbedBuilder embed = new()
            {
                Color = Color.Red,
                Title = user == null ? "Cooldowns" : $"{user}'s Cooldowns",
                Description = description.Length > 0 ? description.ToString() : "None"
            };
            await ReplyAsync(embed: embed.Build());
        }

        [Command("help")]
        [Summary("View info about the bot or view info about a command, depending on if you specify a command or not.")]
        [Remarks("$help <command>")]
        public async Task<RuntimeResult> Help(string command = "")
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                EmbedBuilder infoEmbed = new()
                {
                    Color = Color.Red,
                    Title = "Rush Reborn Bot",
                    Description = "Say hello to the most amazing bot you will ever bare witness to, made by the greatest programmer who has ever lived! ~~definitely not capping~~\n\n" +
                        "This is a module-based bot, so all of the commands are split up into modules.\n\n" +
                        "If you want to learn about a particular module, use ``$modules`` to view the bot's modules and ``$module [module]`` to view the information of whatever module you want to look up.\n\n" +
                        "If you want to learn about a particular command in a module, use ``$help [command]``. In command usage examples, [] indicate required arguments and <> indicate optional arguments.\n\n" +
                        "If you have **ANY** questions, just ask!"
                };

                await ReplyAsync(embed: infoEmbed.Build());
                return CommandResult.FromSuccess();
            }

            SearchResult search = Commands.Search(command);
            if (!search.IsSuccess)
                return CommandResult.FromError("You have specified a nonexistent command!");
            CommandInfo commandInfo = search.Commands[0].Command;
            IEnumerable<string> aliases = commandInfo.Aliases.Except(new string[] { commandInfo.Name });

            StringBuilder description = new($"**Description**: {commandInfo.Summary}\n**Usage**: ``{commandInfo.Remarks}``");
            if (aliases.Any())
                description.Append($"\n**Aliases**: {string.Join(", ", aliases)}");

            if (commandInfo.TryGetPrecondition<CheckPacifistAttribute>())
                description.Append("\nRequires not having the Pacifist perk equipped");
            if (commandInfo.TryGetPrecondition<RequireCashAttribute>())
                description.Append("\nRequires any amount of cash");
            if (commandInfo.TryGetPrecondition<RequireDJAttribute>())
                description.Append("\nRequires DJ");
            if (commandInfo.TryGetPrecondition<RequireNsfwAttribute>())
                description.Append("\nMust be in NSFW channel");
            if (commandInfo.TryGetPrecondition<RequireOwnerAttribute>())
                description.Append("\nRequires Bot Owner");
            if (commandInfo.TryGetPrecondition<RequirePerkAttribute>())
                description.Append("\nRequires a perk");
            if (commandInfo.TryGetPrecondition<RequireStaffAttribute>())
                description.Append("\nRequires Staff");
            if (commandInfo.TryGetPrecondition(out RequireBeInChannelAttribute rBIC))
                description.Append($"\nMust be in #{rBIC.Name}");
            if (commandInfo.TryGetPrecondition(out RequireItemAttribute ri))
                description.Append(string.IsNullOrEmpty(ri.ItemType) ? "\nRequires an item" : $"\nRequires a {ri.ItemType}");
            if (commandInfo.TryGetPrecondition(out RequireUserPermissionAttribute rUP))
                description.Append($"\nRequires {Enum.GetName(rUP.GuildPermission.GetType(), rUP.GuildPermission)} permission");
            if (commandInfo.TryGetPrecondition<RequireNsfwEnabledAttribute>())
            {
                DbConfigModules modules = await DbConfigModules.GetById(Context.Guild.Id);
                if (!modules.NSFWEnabled)
                    return CommandResult.FromError("NSFW commands are disabled!");
            }
            if (commandInfo.TryGetPrecondition(out RequireRankLevelAttribute rRL))
            {
                try
                {
                    DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("ranks");
                    DocumentSnapshot snap = await doc.GetSnapshotAsync();
                    KeyValuePair<string, object> level = snap.ToDictionary().First(kvp => kvp.Key.StartsWith($"level{rRL.RankLevel}") &&
                        kvp.Key.EndsWith("Id"));
                    IRole rank = Context.Guild.GetRole(Convert.ToUInt64(level.Value));
                    description.AppendLine($"\nRequires {rank.Name}");
                }
                catch (Exception)
                {
                    description.Append($"\nRequires rank level {rRL.RankLevel} (rank has not been set)");
                }
            }
            if (commandInfo.TryGetPrecondition<RequireRushRebornAttribute>())
            {
                if (Context.Guild.Id != RequireRushRebornAttribute.RR_MAIN &&
                    Context.Guild.Id != RequireRushRebornAttribute.RR_TEST)
                {
                    return CommandResult.FromError("You have specified a nonexistent command!");
                }

                description.Append("\nExclusive to Rush Reborn");
            }

            EmbedBuilder commandEmbed = new()
            {
                Color = Color.Red,
                Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(command.ToLower()),
                Description = description.ToString()
            };

            await ReplyAsync(embed: commandEmbed.Build());
            return CommandResult.FromSuccess();
        }

        [Command("module")]
        [Summary("View info about a module.")]
        [Remarks("$module [module]")]
        public async Task<RuntimeResult> Module(string module)
        {
            ModuleInfo moduleInfo = Commands.Modules.FirstOrDefault(m => m.Name.Equals(module, StringComparison.OrdinalIgnoreCase));
            if (moduleInfo == default)
                return CommandResult.FromError("You have specified a nonexistent module!");

            if (moduleInfo.Commands[0].TryGetPrecondition<RequireNsfwEnabledAttribute>())
            {
                DbConfigModules modules = await DbConfigModules.GetById(Context.Guild.Id);
                if (!modules.NSFWEnabled)
                    return CommandResult.FromError("NSFW commands are disabled!");
            }

            if (moduleInfo.Commands[0].TryGetPrecondition<RequireRushRebornAttribute>() &&
                Context.Guild.Id != RequireRushRebornAttribute.RR_MAIN &&
                Context.Guild.Id != RequireRushRebornAttribute.RR_TEST)
            {
                return CommandResult.FromError("You have specified a nonexistent module!");
            }

            EmbedBuilder moduleEmbed = new()
            {
                Color = Color.Red,
                Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(module.ToLower()),
                Description = $"**Available commands**: {string.Join(", ", moduleInfo.Commands.Select(x => x.Name).ToArray())}\n**Description**: {moduleInfo.Summary}"
            };

            await ReplyAsync(embed: moduleEmbed.Build());
            return CommandResult.FromSuccess();
        }

        [Command("modules")]
        [Summary("View info about the bot's modules.")]
        [Remarks("$modules")]
        public async Task Modules()
        {
            List<string> modulesList = Commands.Modules.Select(x => x.Name).ToList();
            if (Context.Guild.Id != RequireRushRebornAttribute.RR_MAIN && Context.Guild.Id != RequireRushRebornAttribute.RR_TEST)
                modulesList.Remove("Support");

            EmbedBuilder modulesEmbed = new()
            {
                Color = Color.Red,
                Title = "Currently available modules",
                Description = string.Join(", ", modulesList)
            };

            await ReplyAsync(embed: modulesEmbed.Build());
        }

        [Alias("statistics")]
        [Command("stats")]
        [Summary("View various statistics about your own, or another user's, bot usage.")]
        [Remarks("$stats <user>")]
        public async Task<RuntimeResult> Stats(IGuildUser user = null)
        {
            if (user?.IsBot == true)
                return CommandResult.FromError("Nope.");

            ulong userId = user == null ? Context.User.Id : user.Id;
            DbUser dbUser = await DbUser.GetById(Context.Guild.Id, userId);
            if (dbUser.Stats.Count == 0)
                return CommandResult.FromError(user == null ? "You have no available stats!" : $"**{user}** has no available stats!");

            StringBuilder description = new();
            foreach (string key in dbUser.Stats.Keys.ToList().OrderBy(s => s))
                description.AppendLine($"**{key}**: {dbUser.Stats[key]}");

            EmbedBuilder embed = new()
            {
                Title = user == null ? "Stats" : $"{user}'s Stats",
                Color = Color.Red,
                Description = description.ToString()
            };

            await ReplyAsync(embed: embed.Build());
            return CommandResult.FromSuccess();
        }
    }
}
