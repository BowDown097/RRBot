using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;

namespace RRBot.Modules
{
    [Summary("This is where all the BORING administration stuff goes. Here, you can change how the bot does things in the server in a variety of ways. Huge generalization, but that's the best I can do.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public class Config : ModuleBase<SocketCommandContext>
    {
        private async Task CreateEntry(SocketCommandContext context, string document, object data, string message = "")
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document(document);
            await doc.SetAsync(data, SetOptions.MergeAll);
            if (!string.IsNullOrWhiteSpace(message)) await ReplyAsync(message);
        }

        [Command("addrank")]
        [Summary("Register the ID for a rank, its level, and the money required to get it.")]
        [Remarks("``$addrank [role-id] [level] [cost]``")]
        public async Task AddRank(ulong id, int level, float cost)
        {
            SocketRole role = Context.Guild.GetRole(id);
            await CreateEntry(Context, "ranks", new Dictionary<string, object> { { $"level{level}Id", id.ToString() } });
            await CreateEntry(Context, "ranks", new Dictionary<string, object> { { $"level{level}Cost", cost } }, "Added rank successfully!");
        }

        [Command("addselfrole")]
        [Summary("Add a self role for the self role message.")]
        [Remarks("``$addselfrole [emoji-id] [role-id]``")]
        public async Task<RuntimeResult> AddSelfRole(IEmote emote, ulong roleId)
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("selfroles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("channel", out ulong channelId) && snap.TryGetValue("message", out ulong msgId))
            {
                await doc.SetAsync(new Dictionary<string, object>
                {
                    { emote.ToString(), roleId }
                }, SetOptions.MergeAll);

                SocketTextChannel channel = Context.Guild.GetChannel(channelId) as SocketTextChannel;
                IMessage message = await channel.GetMessageAsync(msgId);
                await message.AddReactionAsync(emote);
                await ReplyAsync("Added self role successfully!");
                return CommandResult.FromSuccess();
            } 

            return CommandResult.FromError("The self roles message has not been set. Please set it using ``$setselfrolesmsg``.");
        }

        [Command("clearconfig")]
        [Summary("Clear all configuration that has been set.")]
        [Remarks("``$clearconfig``")]
        public async Task ClearConfig()
        {
            CollectionReference collection = Program.database.Collection($"servers/{Context.Guild.Id}/config");
            foreach (DocumentReference doc in collection.ListDocumentsAsync().ToEnumerable()) await doc.DeleteAsync();
            await ReplyAsync("All configuration cleared!");
        }

        [Command("clearselfroles")]
        [Summary("Clear the self roles that are registered, if any.")]
        [Remarks("``$clearselfroles``")]
        public async Task ClearSelfRoles()
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("selfroles");
            await doc.DeleteAsync();
            await ReplyAsync("Any registered self roles removed!");
        }

        [Command("currentconfig")]
        [Summary("List the current configuration that has been set for the bot.")]
        [Remarks("``$currentconfig``")]
        public async Task GetCurrentConfig()
        {
            CollectionReference config = Program.database.Collection($"servers/{Context.Guild.Id}/config");
            StringBuilder description = new StringBuilder();
            foreach (DocumentReference doc in config.ListDocumentsAsync().ToEnumerable())
            {
                description.AppendLine($"***{doc.Id}***");
                DocumentSnapshot snap = await doc.GetSnapshotAsync();
                if (snap.Exists)
                {
                    foreach (KeyValuePair<string, object> kvp in snap.ToDictionary())
                    {
                        switch (doc.Id)
                        {
                            case "roles":
                                SocketRole role = Context.Guild.GetRole(Convert.ToUInt64(kvp.Value));
                                description.AppendLine($"**{kvp.Key}**: {role.Name}");
                                break;
                            case "channels":
                                SocketGuildChannel channel = Context.Guild.GetChannel(Convert.ToUInt64(kvp.Value));
                                description.AppendLine($"**{kvp.Key}**: {channel.ToString()}");
                                break;
                            case "ranks":
                                if (kvp.Key.EndsWith("Id", StringComparison.Ordinal))
                                {
                                    SocketRole rank = Context.Guild.GetRole(Convert.ToUInt64(kvp.Value));
                                    description.AppendLine($"**{kvp.Key.Replace("Id", "Role")}**: {rank.Name}");
                                }
                                else
                                {
                                    description.AppendLine($"**{kvp.Key}**: ${Convert.ToSingle(kvp.Value)}");
                                }
                                break;
                            default:
                                description.AppendLine($"**{kvp.Key}**: {kvp.Value.ToString()}");
                                break;
                        }
                    }
                }
            }

            EmbedBuilder embed = new EmbedBuilder
            {
                Title = "Current Configuration",
                Color = Color.Red,
                Description = description.ToString()
            };

            await ReplyAsync(embed: embed.Build());
        }

        [Command("setdjrole")]
        [Summary("Register the ID for the DJ role in your server so that most of the music commands work properly with the bot.")]
        [Remarks("``$setdjrole [role]``")]
        public async Task SetDJRole(IRole role) => await CreateEntry(Context, "roles", new { djRole = role.Id }, "Set DJ role successfully!");

        [Command("setlogschannel")]
        [Summary("Register the ID for the logs channel in your server so that logging works properly with the bot.")]
        [Remarks("``$setlogschannel [channel]``")]
        public async Task SetLogsChannel(IChannel channel) => await CreateEntry(Context, "channels", new { logsChannel = channel.Id }, "Set logs channel successfully!");

        [Command("setmutedrole")]
        [Summary("Register the ID for the Muted role in your server so that mutes work properly with the bot.")]
        [Remarks("``$setmutedrole [role]``")]
        public async Task SetMutedRole(IRole role) => await CreateEntry(Context, "roles", new { mutedRole = role.Id }, "Set muted role successfully!");

        [Command("setpollschannel")]
        [Summary("Register the ID for the polls channel in your server so that polls work properly with the bot.")]
        [Remarks("``$setpollschannel [channel]``")]
        public async Task SetPollsChannel(IChannel channel) => await CreateEntry(Context, "channels", new { pollsChannel = channel.Id }, "Set polls channel successfully!");

        [Command("setselfrolesmsg")]
        [Summary("Register the ID for the message that users can react to to receive roles.")]
        [Remarks("``$setselfrolesmsg [channel] [msg-id]``")]
        public async Task SetSelfRolesMsg(IChannel channel, IMessage msg) => await CreateEntry(Context, "selfroles", new { channel = channel.Id, message = msg.Id }, "Set self roles message successfully!");

        [Command("setstafflvl1role")]
        [Summary("Register the ID for the first level Staff role in your server so that staff-related operations work properly with the bot.")]
        [Remarks("``$setstafflvl1role [role]``")]
        public async Task SetStaffLvl1Role(IRole role) => await CreateEntry(Context, "roles", new { houseRole = role.Id }, "Set first level Staff role successfully!");

        [Command("setstafflvl2role")]
        [Summary("Register the ID for the second level Staff role in your server so that staff-related operations work properly with the bot.")]
        [Remarks("``$setstafflvl2role [role]``")]
        public async Task SetStaffLvl2Role(IRole role) => await CreateEntry(Context, "roles", new { senateRole = role.Id }, "Set second level Staff role successfully!");

        [Command("togglensfw")]
        [Summary("Toggle the NSFW module.")]
        [Remarks("``$togglensfw``")]
        public async Task ToggleNSFW()
        {
            bool status = false;
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("modules");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("nsfw", out bool nsfwEnabled)) status = nsfwEnabled;
            await CreateEntry(Context, "modules", new { nsfw = !status }, $"Toggled NSFW enabled to {!status}");
        }
    }
}
