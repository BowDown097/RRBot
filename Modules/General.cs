using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Google.Cloud.Firestore;
using RRBot.Extensions;
using RRBot.Preconditions;

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
        public static readonly Random random = new Random();

        public static readonly Dictionary<string, string> waifus = new Dictionary<string, string>
        {
            { "Adolf Dripler", "https://i.redd.it/cd9v84v46ma21.jpg" },
            { "Arctic Hawk's mom", "https://s.abcnews.com/images/Technology/whale-gty-jt-191219_hpMain_16x9_1600.jpg" },
            { "Astolfo", "https://i.pinimg.com/originals/47/0d/3d/470d3d86bfd0502f374b1ae7e4ea73b6.jpg" },
            { "Asuna", "https://i.redd.it/oj81n8bpy4e41.jpg" },
            { "Aqua", "https://thicc.mywaifulist.moe/waifus/554/bd320a06a7b1b3b7f44e980a4c8e1ac8a975e575465915f1f13f60efe1108c3f_thumb.jpeg" },
            { "Baldi", "https://cdn.shopify.com/s/files/1/0076/4769/0825/products/bb-render-minifigure-baldi-solo-front_1024x1024.png?v=1565975377" },
            { "Barack Obama", "https://upload.wikimedia.org/wikipedia/commons/thumb/8/8d/President_Barack_Obama.jpg/1200px-President_Barack_Obama.jpg" },
            { "carlos", "https://cdn.discordapp.com/attachments/804898294873456701/817271464067072010/unknown.png" },
            { "DaBaby", "https://s3.amazonaws.com/media.thecrimson.com/photos/2021/03/02/205432_1348650.png" },
            { "Drake", "https://cdn.discordapp.com/attachments/804898294873456701/817272071871922226/ee3e7e8c7c26dbef49b8095c1ca90db2.png" },
            { "Drip Goku", "https://i1.sndcdn.com/artworks-000558462795-v3asuu-t500x500.jpg" },
            { "eduardo", "https://i.imgur.com/1bwSckX.png" },
            { "Emilia", "https://kawaii-mobile.com/wp-content/uploads/2016/10/Re-Zero-Emilia.iPhone-6-Plus-wallpaper-1080x1920.jpg" },
            { "Felix", "https://cdn.discordapp.com/attachments/804898294873456701/817269666845294622/739fa73c-be4f-40c3-a057-50395eb46539.png" },
            { "French Person", "https://live.staticflickr.com/110/297887549_2dc0ee273f_c.jpg" },
            { "George Lincoln Rockwell", "https://i.ytimg.com/vi/hRlvjkQFQvg/hqdefault.jpg" },
            { "Gypsycrusader", "https://cdn.bitwave.tv/uploads/v2/avatar/282be9ac-41d4-4b38-aecd-1320d6b9165f-128.jpg" },
            { "Herbert", "https://upload.wikimedia.org/wikipedia/en/thumb/6/67/Herbert_-_Family_Guy.png/250px-Herbert_-_Family_Guy.png" },
            { "Holo", "https://thicc.mywaifulist.moe/waifus/91/d89a6fa083b95e76b9aa8e3be7a5d5d8dc6ddcb87737d428ffc1b537a0146965_thumb.jpeg" },
            { "juan.", "https://cdn.discordapp.com/attachments/804898294873456701/817275147060772874/unknown.png" },
            { "Kizuna Ai", "https://thicc.mywaifulist.moe/waifus/1608/105790f902e38da70c7ac59da446586c86eb19c7a9afc063b974d74b8870c4cc_thumb.png" },
            { "Linus", "https://i.ytimg.com/vi/hAsZCTL__lo/mqdefault.jpg" },
            { "Luke Smith", "https://i.ytimg.com/vi/UWpf4ZSAHBo/maxresdefault.jpg" },
            { "Midnight", "https://cdn.discordapp.com/attachments/804898294873456701/817268857374375986/653c4c631795ba90acefabb745ba3aa4.png" },
            { "Nagisa", "https://cdn.discordapp.com/attachments/804898294873456701/817270514401280010/3f244bab8ef7beafa5167ef0f7cdfe46.png" },
            { "pablo", "https://cdn.discordapp.com/attachments/804898294873456701/817271690391715850/unknown.png" },
            { "Peter Griffin (in 2015)", "https://i.kym-cdn.com/photos/images/original/001/868/400/45d.jpg" },
            { "Pizza Heist Witness from Spiderman 2", "https://cdn.discordapp.com/attachments/804898294873456701/817272392002961438/unknown.png" },
            { "Quagmire", "https://s3.amazonaws.com/rapgenius/1361855949_glenn_quagmire_by_gan187-d3r70hu.png" },
            { "Rem", "https://cdn.discordapp.com/attachments/804898294873456701/817269005526106122/latest.png" },
            { "Rikka", "https://cdn.discordapp.com/attachments/804898294873456701/817269185176141824/db6e77106a10787b339da6e0b590410c.png" },
            { "Rin", "https://thicc.mywaifulist.moe/waifus/106/94da5e87c3dcc9eb3db018b815d067bed46f63f16a7e12357cafa1b530ce1c1a_thumb.jpeg" },
            { "Senjougahara", "https://thicc.mywaifulist.moe/waifus/262/1289a42d80717ce4fb0767ddc6c2a19cae5b897d4efe8260401aaacdba166f6e_thumb.jpeg" },
            { "Shinobu", "https://thicc.mywaifulist.moe/waifus/255/3906aba5167583d163ff90d46f86777242e6ff25550ed8ac915ef04f65a8d041_thumb.jpeg" },
            { "Shrimpstar", "https://cdn.discordapp.com/attachments/530897481400320030/575123757891452995/image0.jpg" },
            { "Squidward", "https://upload.wikimedia.org/wikipedia/en/thumb/8/8f/Squidward_Tentacles.svg/1200px-Squidward_Tentacles.svg.png" },
            { "Superjombombo", "https://pbs.twimg.com/profile_images/735305572405366786/LF5j-XcT_400x400.jpg" },
            { "Terry A. Davis", "https://upload.wikimedia.org/wikipedia/commons/3/34/Terry_A._Davis_2017.jpg" },
            { "Warren G. Harding, 29th President of the United States", "https://assets.atlasobscura.com/article_images/18223/image.jpg" },
            { "Zero Two", "https://cdn.discordapp.com/attachments/804898294873456701/817269546024042547/c4c54c906261b82f9401b60daf0e5be2.png" },
            { "Zimbabwe", "https://cdn.discordapp.com/attachments/802654650040844380/817273008821108736/unknown.png" }
        };

        [Command("help")]
        [Summary("View info about the bot or view info about a command, depending on if you specify a command or not.")]
        [Remarks("``$help <command>``")]
        public async Task Help([Remainder] string command = "")
        {
            string strippedCommand = string.Join("", command.ToLower().Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            var modules = Commands.Modules;
            
            if (string.IsNullOrWhiteSpace(command))
            {
                EmbedBuilder infoEmbed = new EmbedBuilder
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
                return;
            }

            foreach (ModuleInfo moduleInfo in modules)
            {
                foreach (CommandInfo commandInfo in moduleInfo.Commands)
                {
                    IEnumerable<string> actualAliases = commandInfo.Aliases.Except(new string[] { commandInfo.Name }); // Aliases includes the actual command for some reason
                    if (commandInfo.Name.Equals(strippedCommand, StringComparison.OrdinalIgnoreCase) || actualAliases.Contains(strippedCommand.ToLower()))
                    {
                        CollectionReference config = Program.database.Collection($"servers/{Context.Guild.Id}/config");
                        if (moduleInfo.TryGetPrecondition<RequireNsfwEnabledAttribute>())
                        {
                            DocumentSnapshot modSnap = await config.Document("modules").GetSnapshotAsync();
                            if (!modSnap.TryGetValue("nsfw", out bool nsfw) || !nsfw)
                            {
                                await ReplyAsync($"{Context.User.Mention}, NSFW commands are disabled!");
                                return;
                            }
                        }

                        StringBuilder description = new StringBuilder($"**Description**: {commandInfo.Summary}\n**Usage**: {commandInfo.Remarks}");
                        if (actualAliases.Any()) description.Append($"\n**Alias(es)**: {string.Join(", ", actualAliases)}");

                        if (commandInfo.TryGetPrecondition<RequireDJAttribute>() || moduleInfo.TryGetPrecondition<RequireDJAttribute>()) 
                            description.Append("\nRequires DJ");
                        if (commandInfo.TryGetPrecondition<RequireNsfwAttribute>() || moduleInfo.TryGetPrecondition<RequireNsfwAttribute>()) 
                            description.Append("\nMust be in NSFW channel");
                        if (commandInfo.TryGetPrecondition<RequireOwnerAttribute>() || moduleInfo.TryGetPrecondition<RequireOwnerAttribute>()) 
                            description.Append("\nRequires Bot Owner");
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
                        if (commandInfo.TryGetPrecondition(out RequireRoleAttribute rR) || moduleInfo.TryGetPrecondition(out rR))
                        {
                            DocumentSnapshot snap = await config.Document("roles").GetSnapshotAsync();
                            if (snap.TryGetValue(rR.DatabaseReference, out ulong roleId))
                            {
                                IRole role = Context.Guild.GetRole(roleId);
                                description.Append($"\nRequires {role.Name}");
                            }
                            else
                            {
                                description.Append($"\nRequires {rR.DatabaseReference} (role has not been set)");
                            }
                        }
                        if (commandInfo.TryGetPrecondition<RequireRushRebornAttribute>() || moduleInfo.TryGetPrecondition<RequireRushRebornAttribute>())
                        {
                            if (Context.Guild.Id != RequireRushRebornAttribute.RR_MAIN && Context.Guild.Id != RequireRushRebornAttribute.RR_TEST)
                                break;
                            else
                                description.Append($"\nExclusive to Rush Reborn");
                        }

                        EmbedBuilder commandEmbed = new EmbedBuilder
                        {
                            Color = Color.Red,
                            Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(strippedCommand),
                            Description = description.ToString()
                        };

                        await ReplyAsync(embed: commandEmbed.Build());
                        return;
                    }
                }
            }

            await ReplyAsync($"{Context.User.Mention}, you have specified a nonexistent command!");
        }

        [Alias("module")]
        [Command("modules")]
        [Summary("View info about the bot's modules or view info about a module, depending on if you specify a module or not.")]
        [Remarks("``$modules <module>``")]
        public async Task Modules([Remainder] string module = "")
        {
            string strippedModule = string.Join("", module.ToLower().Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            var modules = Commands.Modules;

            if (string.IsNullOrWhiteSpace(module))
            {
                List<string> modulesList = modules.Select(x => x.Name).ToList();
                if (Context.Guild.Id != RequireRushRebornAttribute.RR_MAIN && Context.Guild.Id != RequireRushRebornAttribute.RR_TEST) modulesList.Remove("Support");

                EmbedBuilder modulesEmbed = new EmbedBuilder
                {
                    Color = Color.Red,
                    Title = "Currently available modules",
                    Description = string.Join(", ", modulesList)
                };

                await ReplyAsync(embed: modulesEmbed.Build());
                return;
            }

            foreach (ModuleInfo moduleInfo in modules)
            {
                if (moduleInfo.Name.Equals(strippedModule, StringComparison.OrdinalIgnoreCase))
                {
                    if (moduleInfo.TryGetPrecondition<RequireNsfwEnabledAttribute>())
                    {
                        DocumentReference modDoc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("modules");
                        DocumentSnapshot modSnap = await modDoc.GetSnapshotAsync();
                        if (modSnap.TryGetValue("nsfw", out bool nsfw) && !nsfw || !modSnap.TryGetValue<bool>("nsfw", out _))
                        {
                            await ReplyAsync($"{Context.User.Mention}, NSFW commands are disabled!");
                            return;
                        }
                    }

                    if (moduleInfo.TryGetPrecondition<RequireRushRebornAttribute>())
                    {
                        if (Context.Guild.Id != RequireRushRebornAttribute.RR_MAIN && Context.Guild.Id != RequireRushRebornAttribute.RR_TEST)
                            break;
                    }

                    EmbedBuilder moduleEmbed = new EmbedBuilder
                    {
                        Color = Color.Red,
                        Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(strippedModule),
                        Description = $"**Available commands**: {string.Join(", ", moduleInfo.Commands.Select(x => x.Name).ToArray())}\n**Description**: {moduleInfo.Summary}"
                    };

                    await ReplyAsync(embed: moduleEmbed.Build());
                    return;
                }
            }

            await ReplyAsync($"{Context.User.Mention}, you have specified a nonexistent module!");
        }

        [Alias("statistics")]
        [Command("stats")]
        [Summary("View various statistics about your own, or another user's, bot usage.")]
        [Remarks("``$stats <user>``")]
        public async Task<RuntimeResult> Stats(IGuildUser user = null)
        {
            if (user != null && user.IsBot) return CommandResult.FromError("Nope.");

            ulong userId = user == null ? Context.User.Id : user.Id;
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(userId.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            StringBuilder description = new StringBuilder();

            if (snap.TryGetValue("stats", out Dictionary<string, string> stats) && stats.Count > 0)
            {
                List<string> keys = stats.Keys.ToList();
                keys.Sort();
                foreach (string key in keys)
                {
                    description.AppendLine($"**{key}**: {stats[key]}");
                }

                EmbedBuilder embed = new EmbedBuilder
                {
                    Title = (user == null ? "Your " : $"{user.ToString()}'s ") + "Stats",
                    Color = Color.Red,
                    Description = description.ToString()
                };

                await ReplyAsync(embed: embed.Build());
                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError(user == null ? $"{Context.User.Mention}, you have no available stats!" : $"**{user.ToString()}** has no available stats!");
        }

        [Command("waifu")]
        [Summary("Get yourself a random waifu from our vast and sexy collection of scrumptious waifus.")]
        [Remarks("``$waifu``")]
        public async Task Waifu()
        {
            List<string> keys = Enumerable.ToList(waifus.Keys);
            string waifu = keys[random.Next(waifus.Count)];

            EmbedBuilder waifuEmbed = new EmbedBuilder
            {
                Color = Color.Red,
                Title = "Say hello to your new waifu!",
                Description = $"Your waifu is **{waifu}**.",
                ImageUrl = waifus[waifu]
            };
            
            await ReplyAsync(embed: waifuEmbed.Build());
        }
    }
}
