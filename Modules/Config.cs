using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using RRBot.Entities;
using RRBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RRBot.Modules
{
    [Summary("This is where all the BORING administration stuff goes. Here, you can change how the bot does things in the server in a variety of ways. Huge generalization, but that's the best I can do.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public class Config : ModuleBase<SocketCommandContext>
    {
        [Command("addrank")]
        [Summary("Register a rank, its level, and the money required to get it.")]
        [Remarks("$addrank [role] [level] [cost]")]
        public async Task AddRank(IRole role, int level, double cost)
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("ranks");
            await doc.SetAsync(new Dictionary<string, object>
            {
                { $"level{level}Id", role.Id.ToString() },
                { $"level{level}Cost", cost }
            });

            await Context.User.NotifyAsync(Context.Channel, $"Added {role} as a level {level} rank that costs {cost:C2}.");
        }

        [Command("addselfrole")]
        [Summary("Add a self role for the self role message.")]
        [Remarks("$addselfrole [emoji] [role]")]
        public async Task<RuntimeResult> AddSelfRole(IEmote emote, IRole role)
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("selfroles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (!snap.TryGetValue("channel", out ulong channelId) || !snap.TryGetValue("message", out ulong msgId))
                return CommandResult.FromError("The self roles message has not been set. Please set it using ``$setselfrolesmsg``.");

            await doc.SetAsync(new Dictionary<string, object>
            {
                { emote.ToString(), role.Id }
            }, SetOptions.MergeAll);

            SocketTextChannel channel = Context.Guild.GetChannel(channelId) as SocketTextChannel;
            IMessage message = await channel.GetMessageAsync(msgId);
            await message.AddReactionAsync(emote);
            await Context.User.NotifyAsync(Context.Channel, $"Added {role} as a self role bound to {emote}.");
            return CommandResult.FromSuccess();
        }

        [Command("clearconfig")]
        [Summary("Clear all configuration that has been set.")]
        [Remarks("$clearconfig")]
        public async Task ClearConfig()
        {
            CollectionReference collection = Program.database.Collection($"servers/{Context.Guild.Id}/config");
            foreach (DocumentReference doc in collection.ListDocumentsAsync().ToEnumerable())
                await doc.DeleteAsync();
            await Context.User.NotifyAsync(Context.Channel, "All configuration cleared!");
        }

        [Command("clearselfroles")]
        [Summary("Clear the self roles that are registered, if any.")]
        [Remarks("$clearselfroles")]
        public async Task ClearSelfRoles()
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("selfroles");
            await doc.DeleteAsync();
            await Context.User.NotifyAsync(Context.Channel, "Any registered self roles removed!");
        }

        [Command("currentconfig")]
        [Summary("List the current configuration that has been set for the bot.")]
        [Remarks("$currentconfig")]
        public async Task GetCurrentConfig()
        {
            QuerySnapshot config = await Program.database.Collection($"servers/{Context.Guild.Id}/config").GetSnapshotAsync();
            StringBuilder description = new();
            foreach (DocumentSnapshot entry in config.Documents)
            {
                description.AppendLine($"***{entry.Id}***");

                Dictionary<string, object> dict = entry.ToDictionary();
                foreach (string key in dict.Keys.ToList().OrderBy(s => s))
                {
                    switch (entry.Id)
                    {
                        case "channels":
                            SocketGuildChannel channel = Context.Guild.GetChannel(Convert.ToUInt64(dict[key]));
                            description.AppendLine($"**{key}**: #{channel.ToString() ?? "deleted-channel"}");
                            break;
                        case "ranks":
                            if (key.EndsWith("Id"))
                            {
                                SocketRole rank = Context.Guild.GetRole(Convert.ToUInt64(dict[key]));
                                description.AppendLine($"**{key.Replace("Id", "Role")}**: {rank.ToString() ?? "(deleted role)"}");
                            }
                            else
                            {
                                description.AppendLine($"**{key}**: ${Convert.ToSingle(dict[key])}");
                            }
                            break;
                        case "roles":
                        case "selfroles":
                            if (key != "message" && key != "channel")
                            {
                                SocketRole role = Context.Guild.GetRole(Convert.ToUInt64(dict[key]));
                                description.AppendLine($"**{key}**: {role.ToString() ?? "(deleted role)"}");
                            }
                            else
                            {
                                description.AppendLine($"**{key}**: {dict[key]}");
                            }
                            break;
                        default:
                            description.AppendLine($"**{key}**: {dict[key]}");
                            break;
                    }
                }
            }

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Current Configuration")
                .WithDescription(description.ToString());
            await ReplyAsync(embed: embed.Build());
        }

        [Command("setdjrole")]
        [Summary("Register the ID for the DJ role in your server so that most of the music commands work properly with the bot.")]
        [Remarks("$setdjrole [role]")]
        public async Task SetDJRole(IRole role)
        {
            DbConfigRoles roles = await DbConfigRoles.GetById(Context.Guild.Id);
            roles.DJRole = role.Id;
            await Context.User.NotifyAsync(Context.Channel, $"Set {role} as the DJ role.");
            await roles.Write();
        }

        [Command("setlogschannel")]
        [Summary("Register the ID for the logs channel in your server so that logging works properly with the bot.")]
        [Remarks("$setlogschannel [channel]")]
        public async Task SetLogsChannel(IChannel channel)
        {
            DbConfigChannels channels = await DbConfigChannels.GetById(Context.Guild.Id);
            channels.LogsChannel = channel.Id;
            await Context.User.NotifyAsync(Context.Channel, $"Set logs channel to #{channel}.");
            await channels.Write();
        }

        [Command("setmutedrole")]
        [Summary("Register the ID for the Muted role in your server so that mutes work properly with the bot.")]
        [Remarks("$setmutedrole [role]")]
        public async Task SetMutedRole(IRole role)
        {
            DbConfigRoles roles = await DbConfigRoles.GetById(Context.Guild.Id);
            roles.MutedRole = role.Id;
            await Context.User.NotifyAsync(Context.Channel, $"Set muted role to {role}.");
            await roles.Write();
        }

        [Command("setpollschannel")]
        [Summary("Register the ID for the polls channel in your server so that polls work properly with the bot.")]
        [Remarks("$setpollschannel [channel]")]
        public async Task SetPollsChannel(IChannel channel)
        {
            DbConfigChannels channels = await DbConfigChannels.GetById(Context.Guild.Id);
            channels.PollsChannel = channel.Id;
            await Context.User.NotifyAsync(Context.Channel, $"Set polls channel to #{channel}.");
            await channels.Write();
        }

        [Command("setselfrolesmsg")]
        [Summary("Register the ID for the message that users can react to to receive roles.")]
        [Remarks("$setselfrolesmsg [channel] [msg-id]")]
        public async Task<RuntimeResult> SetSelfRolesMsg(IChannel channel, ulong msgId)
        {
            IMessage msg = await (channel as ITextChannel)?.GetMessageAsync(msgId);
            if (msg == null)
                return CommandResult.FromError("You specified an invalid message!");

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("selfroles");
            await doc.SetAsync(new { channel = channel.Id, message = msgId }, SetOptions.MergeAll);
            await Context.User.NotifyAsync(Context.Channel, $"Set self roles message to the one at {msg.GetJumpUrl()}.");
            return CommandResult.FromSuccess();
        }

        [Command("setstafflvl1role")]
        [Summary("Register the ID for the first level Staff role in your server so that staff-related operations work properly with the bot.")]
        [Remarks("$setstafflvl1role [role]")]
        public async Task SetStaffLvl1Role(IRole role)
        {
            DbConfigRoles roles = await DbConfigRoles.GetById(Context.Guild.Id);
            roles.StaffLvl1Role = role.Id;
            await Context.User.NotifyAsync(Context.Channel, $"Set first level Staff role to {role}.");
            await roles.Write();
        }

        [Command("setstafflvl2role")]
        [Summary("Register the ID for the second level Staff role in your server so that staff-related operations work properly with the bot.")]
        [Remarks("$setstafflvl2role [role]")]
        public async Task SetStaffLvl2Role(IRole role)
        {
            DbConfigRoles roles = await DbConfigRoles.GetById(Context.Guild.Id);
            roles.StaffLvl2Role = role.Id;
            await Context.User.NotifyAsync(Context.Channel, $"Set second level Staff role to {role}.");
            await roles.Write();
        }

        [Command("togglensfw")]
        [Summary("Toggle the NSFW module.")]
        [Remarks("$togglensfw")]
        public async Task ToggleNSFW()
        {
            DbConfigModules modules = await DbConfigModules.GetById(Context.Guild.Id);
            modules.NSFWEnabled = !modules.NSFWEnabled;
            await ReplyAsync($"Toggled NSFW enabled to {modules.NSFWEnabled}.");
            await modules.Write();
        }
    }
}
