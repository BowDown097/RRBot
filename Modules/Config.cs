using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using RRBot.Preconditions;

namespace RRBot.Modules
{
    public class Config : ModuleBase<SocketCommandContext>
    {
        // helpers
        private async Task CreateEntry(SocketCommandContext context, string document, object data, string message)
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document(document);
            await doc.SetAsync(data, SetOptions.MergeAll);
            await ReplyAsync(message);
        }

        // commands
        [Command("addrank")]
        [Summary("Register the ID for a rank and the money required to get it.")]
        [Remarks("``$addrank [role-id] [cost]``")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task AddRank(ulong id, float cost) => await CreateEntry(Context, "ranks", new Dictionary<string, object> { { id.ToString(), cost } }, "Added rank successfully!");

        [Command("addselfrole")]
        [Summary("Add a self role for the self role message.")]
        [Remarks("``$addselfrole [emoji-id] [role-id]``")]
        [RequireUserPermission(GuildPermission.Administrator)]
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

        [Command("clearselfroles")]
        [Summary("Clear the self roles that are registered, if any.")]
        [Remarks("``$clearselfroles``")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ClearSelfRoles()
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("selfroles");
            await doc.DeleteAsync();
            await ReplyAsync("Any registered self roles removed!");
        }

        [Command("currentconfig")]
        [Summary("List the current configuration that has been set for the bot.")]
        [Remarks("``$currentconfig``")]
        [RequireStaff]
        public async Task GetCurrentConfig()
        {
            CollectionReference config = Program.database.Collection($"servers/{Context.Guild.Id}/config");
            string description = "";
            foreach (DocumentReference doc in config.ListDocumentsAsync().ToEnumerable())
            {
                description += $"***{doc.Id}***\n";
                DocumentSnapshot snap = await doc.GetSnapshotAsync();
                if (snap.Exists)
                {
                    foreach (KeyValuePair<string, object> kvp in snap.ToDictionary())
                    {
                        switch (doc.Id)
                        {
                            case "roles":
                                SocketRole role = Context.Guild.GetRole(Convert.ToUInt64(kvp.Value));
                                description += $"**{kvp.Key}**: {role.Name}\n";
                                break;
                            case "channels":
                                SocketGuildChannel channel = Context.Guild.GetChannel(Convert.ToUInt64(kvp.Value));
                                description += $"**{kvp.Key}**: #{channel.Name}\n";
                                break;
                            case "ranks":
                                SocketRole rank = Context.Guild.GetRole(ulong.Parse(kvp.Key));
                                description += $"**{rank.Name}**: ${Convert.ToSingle(kvp.Value)}\n";
                                break;
                            default:
                                description += $"**{kvp.Key}**: {kvp.Value.ToString()}\n";
                                break;
                        }
                    }
                }
            }

            EmbedBuilder embed = new EmbedBuilder
            {
                Title = "Current Configuration",
                Color = Color.Red,
                Description = description ?? "None"
            };
            await ReplyAsync(embed: embed.Build());
        }

        [Command("setdebaterole")]
        [Summary("Register the ID for the debate role in your server so that debates work properly with the bot.")]
        [Remarks("``$setdebaterole [role-id]``")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetDebateRole(ulong id) => await CreateEntry(Context, "roles", new { debateRole = id }, "Set debate role successfully!");

        [Command("setdjrole")]
        [Summary("Register the ID for the DJ role in your server so that most of the music commands work properly with the bot.")]
        [Remarks("``$setdjrole [role-id]``")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetDJRole(ulong id) => await CreateEntry(Context, "roles", new { djRole = id }, "Set DJ role successfully!");

        [Command("setlogschannel")]
        [Summary("Register the ID for the logs channel in your server so that logging works properly with the bot.")]
        [Remarks("``$setlogschannel [channel-id]``")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetLogsChannel(ulong id) => await CreateEntry(Context, "channels", new { logsChannel = id }, "Set logs channel successfully!");

        [Command("setmutedrole")]
        [Summary("Register the ID for the Muted role in your server so that mutes work properly with the bot.")]
        [Remarks("``$setmutedrole [role-id]``")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetMutedRole(ulong id) => await CreateEntry(Context, "roles", new { mutedRole = id }, "Set muted role successfully!");

        [Command("setpollschannel")]
        [Summary("Register the ID for the polls channel in your server so that polls work properly with the bot.")]
        [Remarks("``$setpollschannel [channel-id]``")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetPollsChannel(ulong id) => await CreateEntry(Context, "channels", new { pollsChannel = id }, "Set polls channel successfully!");

        [Command("setselfrolesmsg")]
        [Summary("Register the ID for the message that users can react to to receive roles.")]
        [Remarks("``$setselfrolesmsg [channel-id] [msg-id]``")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetSelfRolesMsg(ulong channelId, ulong msgId) => await CreateEntry(Context, "selfroles", new { channel = channelId, message = msgId }, "Set self roles message successfully!");

        [Command("setstafflvl1role")]
        [Summary("Register the ID for the first level Staff role in your server so that staff-related operations work properly with the bot.")]
        [Remarks("``$setstafflvl1role [role-id]``")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetStaffLvl1Role(ulong id) => await CreateEntry(Context, "roles", new { houseRole = id }, "Set first level Staff role successfully!");

        [Command("setstafflvl2role")]
        [Summary("Register the ID for the second level Staff role in your server so that staff-related operations work properly with the bot.")]
        [Remarks("``$setstafflvl2role [role-id]``")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetStaffLvl2Role(ulong id) => await CreateEntry(Context, "roles", new { senateRole = id }, "Set second level Staff role successfully!");
    }
}
