using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using RRBot.Preconditions;

namespace RRBot.Modules
{
    [RequireStaff]
    public class Moderation : ModuleBase<SocketCommandContext>
    {
        [Alias("seethe")]
        [Command("ban")]
        [Summary("Ban any member.")]
        [Remarks("``$ban [user] <duration> <reason>``")]
        public async Task<RuntimeResult> Ban(IGuildUser user, string duration = "", [Remainder] string reason = "")
        {
            if (user.IsBot) return CommandResult.FromError("Nope.");

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("roles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("houseRole", out ulong staffId) && user.RoleIds.Contains(staffId))
                return CommandResult.FromError($"{Context.User.Mention}, you cannot ban **{user.ToString()}** because they are a staff member.");

            if (int.TryParse(Regex.Match(duration, @"\d+").Value, out int time))
            {
                char suffix = char.ToLowerInvariant(duration[duration.Length - 1]);
                string response;
                TimeSpan timeSpan;
                switch (suffix)
                {
                    case 's':
                        timeSpan = TimeSpan.FromSeconds(time);
                        response = $"**{Context.User.ToString()}** has banned **{user.ToString()}** for {time} second(s)";
                        break;
                    case 'm':
                        timeSpan = TimeSpan.FromMinutes(time);
                        response = $"**{Context.User.ToString()}** has banned **{user.ToString()}** for {time} minute(s)";
                        break;
                    case 'h':
                        timeSpan = TimeSpan.FromHours(time);
                        response = $"**{Context.User.ToString()}** has banned **{user.ToString()}** for {time} hour(s)";
                        break;
                    case 'd':
                        timeSpan = TimeSpan.FromDays(time);
                        response = $"**{Context.User.ToString()}** has banned **{user.ToString()}** for {time} day(s)";
                        break;
                    default:
                        return CommandResult.FromError($"{Context.User.Mention}, you specified an invalid amount of time!");
                }

                DocumentReference banDoc = Program.database.Collection($"servers/{Context.Guild.Id}/bans").Document(user.Id.ToString());
                response += string.IsNullOrWhiteSpace(reason) ? ". So long, sack of shit!" : $"for `{reason}`. So long, sack of shit!";
                await ReplyAsync(response);
                await Program.logger.Client_UserBanned(user as SocketUser, user.Guild as SocketGuild);
                await banDoc.SetAsync(new { Time = Global.UnixTime(timeSpan.TotalSeconds) });
                await user.BanAsync(reason: reason);
                return CommandResult.FromSuccess();
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                await user.BanAsync(reason: duration);
                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError($"{Context.User.Mention}, you specified an invalid amount of time!");
        }

        [Command("chill")]
        [Summary("Shut chat the fuck up for a specific amount of time in seconds.")]
        [Remarks("``$chill [duration]``")]
        public async Task<RuntimeResult> Chill(int duration)
        {
            if (duration < 30) return CommandResult.FromError($"{Context.User.Mention}, you cannot chill for less than 30 seconds.");
            if (duration > 3600) return CommandResult.FromError($"{Context.User.Mention}, you cannot chill for more than an hour.");

            SocketTextChannel channel = Context.Channel as SocketTextChannel;
            OverwritePermissions perms = channel.GetPermissionOverwrite(Context.Guild.EveryoneRole).Value;

            if (perms.SendMessages == PermValue.Deny) return CommandResult.FromError($"{Context.User.Mention}, this chat is already chilled.");

            await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, perms.Modify(sendMessages: PermValue.Deny));
            await ReplyAsync($"**{Context.User.ToString()}** would like y'all to sit down and stfu for {duration} seconds!");

            Global.RunInBackground(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(duration));
                await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, perms);
            });

            return CommandResult.FromSuccess();
        }

        [Alias("cope")]
        [Command("kick")]
        [Summary("Kick any member.")]
        [Remarks("``$kick [user] <reason>``")]
        public async Task<RuntimeResult> Kick(IGuildUser user, [Remainder] string reason = "")
        {
            if (user.IsBot) return CommandResult.FromError("Nope.");

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("roles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("houseRole", out ulong staffId) && user.RoleIds.Contains(staffId)) 
                return CommandResult.FromError($"{Context.User.Mention}, you cannot kick **{user.ToString()}** because they are a staff member.");

            await user.KickAsync(reason);

            string response = $"**{Context.User.ToString()}** has kicked **{user.ToString()}";
            response += string.IsNullOrWhiteSpace(reason) ? "." : $"for '{reason}'";
            await ReplyAsync(response);

            return CommandResult.FromSuccess();
        }

        [Command("mute")]
        [Summary("Mute any member for any amount of time with any reason.")]
        [Remarks("``$mute [user] [duration][s/m/h/d] <reason>``")]
        public async Task<RuntimeResult> Mute(IGuildUser user, string duration, [Remainder] string reason = "")
        {
            if (user.IsBot) return CommandResult.FromError("Nope.");

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("roles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("mutedRole", out ulong mutedId) && snap.TryGetValue("houseRole", out ulong staffId))
            {
                if (user.RoleIds.Contains(mutedId) || user.RoleIds.Contains(staffId)) 
                    return CommandResult.FromError($"{Context.User.Mention}, you cannot mute **{user.ToString()}** because they are either already muted or a staff member.");

                SocketRole role = Context.Guild.GetRole(mutedId);
                char suffix = char.ToLowerInvariant(duration[duration.Length - 1]);
                if (int.TryParse(Regex.Match(duration, @"\d+").Value, out int time))
                {
                    string response;
                    TimeSpan timeSpan;
                    switch (suffix)
                    {
                        case 's':
                            timeSpan = TimeSpan.FromSeconds(time);
                            response = $"**{Context.User.ToString()}** has handed an L of the mute variety to **{user.ToString()}** for {time} second(s)";
                            break;
                        case 'm':
                            timeSpan = TimeSpan.FromMinutes(time);
                            response = $"**{Context.User.ToString()}** has handed an L of the mute variety to **{user.ToString()}** for {time} minute(s)";
                            break;
                        case 'h':
                            timeSpan = TimeSpan.FromHours(time);
                            response = $"**{Context.User.ToString()}** has handed an L of the mute variety to **{user.ToString()}** for {time} hour(s)";
                            break;
                        case 'd':
                            timeSpan = TimeSpan.FromDays(time);
                            response = $"**{Context.User.ToString()}** has handed an L of the mute variety to **{user.ToString()}** for {time} day(s)";
                            break;
                        default:
                            return CommandResult.FromError($"{Context.User.Mention}, you specified an invalid amount of time!");
                    }

                    DocumentReference muteDoc = Program.database.Collection($"servers/{Context.Guild.Id}/mutes").Document(user.Id.ToString());
                    response += string.IsNullOrWhiteSpace(reason) ? "." : $" for '{reason}'";
                    await ReplyAsync(response);
                    await Program.logger.Custom_UserMuted(user, Context.User, duration, reason);
                    await muteDoc.SetAsync(new { Time = Global.UnixTime(timeSpan.TotalSeconds) });
                    await user.AddRoleAsync(role);
                    return CommandResult.FromSuccess();
                }

                return CommandResult.FromError($"{Context.User.Mention}, you specified an invalid amount of time!");
            }

            return CommandResult.FromError("This server's staff and/or muted role(s) have yet to be set.");
        }

        [Alias("clear", "1984")]
        [Command("purge")]
        [Summary("Purge any amount of messages (Note: messages that are two weeks old or older will fail to delete).")]
        [Remarks("``$purge [count] <user>``")]
        public async Task<RuntimeResult> Purge(int count, IGuildUser user = null)
        {
            if (count == 0) return CommandResult.FromError($"{Context.User.Mention}, count must be more than zero.");

            IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync(count + 1).FlattenAsync();
            messages = messages.Where(msg => (DateTimeOffset.UtcNow - msg.Timestamp).TotalDays <= 14);

            if (user != null) messages = messages.Where(msg => msg.Author.Id == user.Id);
            if (!messages.Any()) return CommandResult.FromError("No messages were deleted.");
            if (messages.Any(msg => (DateTimeOffset.UtcNow - msg.Timestamp).TotalDays > 14)) 
                await ReplyAsync($"{Context.User.Mention}, some messages were found to be older than 2 weeks and cannot be deleted.");

            await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(messages);

            await Program.logger.Custom_MessagesPurged(messages, Context.Guild);

            return CommandResult.FromSuccess();
        }

        [Command("unban")]
        [Summary("Unban any currently banned member.")]
        [Remarks("``$unban [user-id]``")]
        public async Task<RuntimeResult> Unban(ulong userId)
        {
            IReadOnlyCollection<RestBan> bans = await Context.Guild.GetBansAsync();
            if (!bans.Any(ban => ban.User.Id == userId)) return CommandResult.FromError($"{Context.User.Mention}, that user is not currently banned.");

            string userString = bans.FirstOrDefault(ban => ban.User.Id == userId).User.ToString();
            await ReplyAsync($"**{Context.User.ToString()}** has unbanned **{userString}**.");
            await Context.Guild.RemoveBanAsync(userId);
            return CommandResult.FromSuccess();
        }

        [Alias("thaw")]
        [Command("unchill")]
        [Summary("Let chat talk now.")]
        [Remarks("``$unchill``")]
        public async Task<RuntimeResult> Unchill()
        {
            SocketTextChannel channel = Context.Channel as SocketTextChannel;
            OverwritePermissions perms = channel.GetPermissionOverwrite(Context.Guild.EveryoneRole).Value;

            if (perms.SendMessages == PermValue.Allow) return CommandResult.FromError($"{Context.User.Mention}, this chat is not chilled.");

            await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, perms.Modify(sendMessages: PermValue.Allow));
            await ReplyAsync($"**{Context.User.ToString()}** took one for the team and unchilled early.");

            return CommandResult.FromSuccess();
        }

        [Command("unmute")]
        [Summary("Unmute any member.")]
        [Remarks("``$unmute [user]``")]
        public async Task<RuntimeResult> Unmute(IGuildUser user)
        {
            if (user.IsBot) return CommandResult.FromError("Nope.");

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("roles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("mutedRole", out ulong mutedId))
            {
                SocketRole role = Context.Guild.GetRole(mutedId);
                if (user.RoleIds.Contains(role.Id))
                {
                    await Program.logger.Custom_UserUnmuted(user, Context.User);
                    await ReplyAsync($"**{Context.User.ToString()}** has unmuted **{user.ToString()}**.");
                    await user.RemoveRoleAsync(role);
                    return CommandResult.FromSuccess();
                }
                return CommandResult.FromError($"**{Context.User.ToString()}** is a brainiac and tried to unmute someone that wasn't muted in the first place. Everyone point and laugh!");
            }

            return CommandResult.FromError("This server's muted role has yet to be set.");
        }
    }
}
