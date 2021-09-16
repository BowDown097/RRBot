using Discord;
using Discord.Commands;
using Google.Cloud.Firestore;
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
    public class FunnyContext
    {
        public SocketCommandContext Context;
        public FunnyContext(SocketCommandContext context) => Context = context;
    }

    [Summary("The name really explains it all. Fun fact, you used one of the commands under this module to view info about this module.")]
    public class General : ModuleBase<SocketCommandContext>
    {
        public CommandService Commands { get; set; }

        [Command("help")]
        [Summary("View info about the bot or view info about a command, depending on if you specify a command or not.")]
        [Remarks("$help <command>")]
        public async Task<RuntimeResult> Help(string command = "")
        {
            string cmdLower = command.ToLower();
            IEnumerable<ModuleInfo> modules = Commands.Modules;

            if (string.IsNullOrWhiteSpace(command))
            {
                EmbedBuilder infoEmbed = new()
                {
                    Color = Color.Red,
                    Title = "Rush Reborn Bot",
                    Description = "Say hello to the most amazing bot you will ever bare witness to, made by the greatest programmer who has ever lived! ~~definitely not capping~~\n\n" +
                        "This is what I like to call a \"module-based\" bot, where all of the commands are split up into modules.\n\n" +
                        "If you want to learn about a particular module, use ``$modules`` to view the bot's modules and ``$modules [module]`` to view the information of whatever module you want to look up.\n\n" +
                        "If you want to learn about a particular command in a module, use ``$help [command]``. In command usage examples, [] indicate required arguments and <> indicate optional arguments.\n\n" +
                        "If you have **ANY** questions, just ask!"
                };

                await ReplyAsync(embed: infoEmbed.Build());
                return CommandResult.FromSuccess();
            }

            foreach (ModuleInfo moduleInfo in modules)
            {
                foreach (CommandInfo commandInfo in moduleInfo.Commands)
                {
                    IEnumerable<string> actualAliases = commandInfo.Aliases.Except(new string[] { commandInfo.Name }); // Aliases includes the actual command for some reason
                    if (commandInfo.Name == cmdLower || actualAliases.Contains(cmdLower))
                    {
                        CollectionReference config = Program.database.Collection($"servers/{Context.Guild.Id}/config");
                        if (moduleInfo.TryGetPrecondition<RequireNsfwEnabledAttribute>())
                        {
                            DocumentSnapshot modSnap = await config.Document("modules").GetSnapshotAsync();
                            if (!modSnap.TryGetValue("nsfw", out bool nsfw) || !nsfw)
                                return CommandResult.FromError("NSFW commands are disabled!");
                        }

                        StringBuilder description = new($"**Description**: {commandInfo.Summary}\n**Usage**: ``{commandInfo.Remarks}``");
                        if (actualAliases.Any())
                            description.Append($"\n**Alias(es)**: {string.Join(", ", actualAliases)}");

                        if (commandInfo.TryGetPrecondition<CheckPacifistAttribute>() || moduleInfo.TryGetPrecondition<CheckPacifistAttribute>())
                            description.Append("\nRequires not having the Pacifist perk equipped");
                        if (commandInfo.TryGetPrecondition<RequireDJAttribute>() || moduleInfo.TryGetPrecondition<RequireDJAttribute>())
                            description.Append("\nRequires DJ");
                        if (commandInfo.TryGetPrecondition<RequireNsfwAttribute>() || moduleInfo.TryGetPrecondition<RequireNsfwAttribute>())
                            description.Append("\nMust be in NSFW channel");
                        if (commandInfo.TryGetPrecondition<RequireOwnerAttribute>() || moduleInfo.TryGetPrecondition<RequireOwnerAttribute>())
                            description.Append("\nRequires Bot Owner");
                        if (commandInfo.TryGetPrecondition<RequirePerkAttribute>() || moduleInfo.TryGetPrecondition<RequirePerkAttribute>())
                            description.Append("\nRequires a perk");
                        if (commandInfo.TryGetPrecondition<RequireStaffAttribute>() || moduleInfo.TryGetPrecondition<RequireOwnerAttribute>())
                            description.Append("\nRequires Staff");
                        if (commandInfo.TryGetPrecondition(out RequireBeInChannelAttribute rBIC) || moduleInfo.TryGetPrecondition(out rBIC))
                            description.Append($"\nMust be in #{rBIC.Name}");
                        if (commandInfo.TryGetPrecondition(out RequireCashAttribute rc) || moduleInfo.TryGetPrecondition(out rc))
                            description.Append((int)rc.Amount == 1 ? "\nRequires any amount of cash" : $"\nRequires ${(int)rc.Amount}");
                        if (commandInfo.TryGetPrecondition(out RequireItemAttribute ri) || moduleInfo.TryGetPrecondition(out ri))
                            description.Append(string.IsNullOrEmpty(ri.ItemType) ? "\nRequires an item" : $"\nRequires a {ri.ItemType}");
                        if (commandInfo.TryGetPrecondition(out RequireUserPermissionAttribute rUP) || moduleInfo.TryGetPrecondition(out rUP))
                            description.Append($"\nRequires {Enum.GetName(rUP.GuildPermission.GetType(), rUP.GuildPermission)} permission");
                        if (commandInfo.TryGetPrecondition(out RequireRankLevelAttribute rRL) || moduleInfo.TryGetPrecondition(out rRL))
                        {
                            try
                            {
                                DocumentSnapshot snap = await config.Document("ranks").GetSnapshotAsync();
                                KeyValuePair<string, object> level = snap.ToDictionary().First(kvp => kvp.Key.StartsWith($"level{rRL.RankLevel}", StringComparison.Ordinal) &&
                                    kvp.Key.EndsWith("Id", StringComparison.Ordinal));
                                IRole rank = Context.Guild.GetRole(Convert.ToUInt64(level.Value));
                                description.AppendLine($"\nRequires {rank.Name}");
                            }
                            catch (Exception)
                            {
                                description.Append($"\nRequires rank level {rRL.RankLevel} (rank has not been set)");
                            }
                        }
                        if (commandInfo.TryGetPrecondition<RequireRushRebornAttribute>() || moduleInfo.TryGetPrecondition<RequireRushRebornAttribute>())
                        {
                            if (Context.Guild.Id != RequireRushRebornAttribute.RR_MAIN &&
                                Context.Guild.Id != RequireRushRebornAttribute.RR_TEST)
                            {
                                break;
                            }
                            else
                            {
                                description.Append("\nExclusive to Rush Reborn");
                            }
                        }

                        EmbedBuilder commandEmbed = new()
                        {
                            Color = Color.Red,
                            Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cmdLower),
                            Description = description.ToString()
                        };

                        await ReplyAsync(embed: commandEmbed.Build());
                        return CommandResult.FromSuccess();
                    }
                }
            }

            return CommandResult.FromError("You have specified a nonexistent command!");
        }

        [Command("module")]
        [Summary("View info about a module.")]
        [Remarks("$module [module]")]
        public async Task<RuntimeResult> Module(string module)
        {
            string moduleLower = module.ToLower();

            foreach (ModuleInfo moduleInfo in Commands.Modules)
            {
                if (moduleInfo.Name == moduleLower)
                {
                    if (moduleInfo.TryGetPrecondition<RequireNsfwEnabledAttribute>())
                    {
                        DocumentReference modDoc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("modules");
                        DocumentSnapshot modSnap = await modDoc.GetSnapshotAsync();
                        if (!modSnap.TryGetValue("nsfw", out bool nsfw) || !nsfw)
                            return CommandResult.FromError("NSFW commands are disabled!");
                    }

                    if (moduleInfo.TryGetPrecondition<RequireRushRebornAttribute>() &&
                        Context.Guild.Id != RequireRushRebornAttribute.RR_MAIN &&
                        Context.Guild.Id != RequireRushRebornAttribute.RR_TEST)
                    {
                        break;
                    }

                    EmbedBuilder moduleEmbed = new()
                    {
                        Color = Color.Red,
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(moduleLower),
                        Description = $"**Available commands**: {string.Join(", ", moduleInfo.Commands.Select(x => x.Name).ToArray())}\n**Description**: {moduleInfo.Summary}"
                    };

                    await ReplyAsync(embed: moduleEmbed.Build());
                    return CommandResult.FromSuccess();
                }
            }

            return CommandResult.FromError("You have specified a nonexistent module!");
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
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(userId.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            StringBuilder description = new();

            if (snap.TryGetValue("stats", out Dictionary<string, string> stats) && stats.Count > 0)
            {
                List<string> keys = stats.Keys.ToList();
                keys.Sort();
                foreach (string key in keys)
                    description.AppendLine($"**{key}**: {stats[key]}");

                EmbedBuilder embed = new()
                {
                    Title = (user == null ? "Your " : $"{user}'s ") + "Stats",
                    Color = Color.Red,
                    Description = description.ToString()
                };

                await ReplyAsync(embed: embed.Build());
                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError(user == null ? "You have no available stats!" : $"**{user}** has no available stats!");
        }
    }
}
