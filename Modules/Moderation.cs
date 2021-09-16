using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using Microsoft.CodeAnalysis;
using RRBot.Extensions;
using RRBot.Preconditions;
using RRBot.Systems;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RRBot.Modules
{
    [Summary("Impose an Orwellian life on the poor normies in chat, through bans, kicks, mutes, you name it.")]
    [RequireStaff]
    public class Moderation : ModuleBase<SocketCommandContext>
    {
        private Tuple<TimeSpan, string> ResolveDuration(string duration, int time, string action, IGuildUser target)
        {
            char suffix = char.ToLowerInvariant(duration[^1]);
            return suffix switch
            {
                's' => new(TimeSpan.FromSeconds(time), $"**{Context.User}** has {action} **{target}** for {time} second(s)"),
                'm' => new(TimeSpan.FromMinutes(time), $"**{Context.User}** has {action} **{target}** for {time} minute(s)"),
                'h' => new(TimeSpan.FromHours(time), $"**{Context.User}** has {action} **{target}** for {time} hour(s)"),
                'd' => new(TimeSpan.FromDays(time), $"**{Context.User}** has {action} **{target}** for {time} day(s)"),
                _ => new(TimeSpan.Zero, null),
            };
        }

        [Alias("seethe")]
        [Command("ban")]
        [Summary("Ban any member.")]
        [Remarks("$ban [user] <duration> <reason>")]
        public async Task<RuntimeResult> Ban(IGuildUser user, string duration = "", [Remainder] string reason = "")
        {
            if (user.IsBot)
                return CommandResult.FromError("Nope.");

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("roles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if ((snap.TryGetValue("houseRole", out ulong staffId) && user.RoleIds.Contains(staffId)) ||
            (snap.TryGetValue("senateRole", out ulong senateId) && user.RoleIds.Contains(senateId)))
            {
                return CommandResult.FromError($"You cannot ban **{user}** because they are a staff member.");
            }

            if (int.TryParse(Regex.Match(duration, @"\d+").Value, out int time))
            {
                Tuple<TimeSpan, string> resolved = ResolveDuration(duration, time, "banned", user);
                string response = resolved.Item2;
                if (resolved.Item1 == TimeSpan.Zero)
                    return CommandResult.FromError("You specified an invalid amount of time!");
                response += string.IsNullOrWhiteSpace(reason) ? "." : $" for '{reason}'";
                await ReplyAsync(response);

                DocumentReference banDoc = Program.database.Collection($"servers/{Context.Guild.Id}/bans").Document(user.Id.ToString());
                await Logger.Client_UserBanned(user as SocketUser, user.Guild as SocketGuild);
                await banDoc.SetAsync(new { Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(resolved.Item1.TotalSeconds) });
                await user.BanAsync(reason: reason);
                await (user as SocketUser).AddToStatsAsync(CultureInfo.CurrentCulture, Context.Guild, new Dictionary<string, string>
                {
                    { "Bans", "1" }
                });

                return CommandResult.FromSuccess();
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                await user.BanAsync(reason: duration);
                await (user as SocketUser).AddToStatsAsync(CultureInfo.CurrentCulture, Context.Guild, new Dictionary<string, string>
                {
                    { "Bans", "1" }
                });

                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError("You specified an invalid amount of time!");
        }

        [Command("cancelticket")]
        [Summary("Pre-emptively cancel a support ticket.")]
        [Remarks("$cancelticket [index]")]
        [RequireBeInChannel("help-requests")]
        [RequireRushReborn]
        public async Task<RuntimeResult> CancelTicket(int index)
        {
            CollectionReference ticketsCollection = Program.database.Collection($"servers/{Context.Guild.Id}/supportTickets");
            IAsyncEnumerable<DocumentReference> tickets = ticketsCollection.ListDocumentsAsync();
            if (index > await tickets.CountAsync() || index <= 0)
                return CommandResult.FromError("There is not a support ticket at that index!");

            DocumentReference doc = await tickets.ElementAtAsync(index - 1);
            DocumentSnapshot snap = await doc.GetSnapshotAsync();

            await Support.CloseTicket(Context.Channel, Context.User, doc, snap, $"Support ticket #{index} deleted successfully!");
            return CommandResult.FromSuccess();
        }

        [Command("chill")]
        [Summary("Shut chat the fuck up for a specific amount of time in seconds.")]
        [Remarks("$chill [duration]")]
        public async Task<RuntimeResult> Chill(int duration)
        {
            if (duration < Constants.CHILL_MIN_SECONDS)
                return CommandResult.FromError($"You cannot chill the chat for less than {Constants.CHILL_MIN_SECONDS} seconds.");
            if (duration > Constants.CHILL_MAX_SECONDS)
                return CommandResult.FromError($"You cannot chill the chat for more than {Constants.CHILL_MAX_SECONDS} seconds.");

            SocketTextChannel channel = Context.Channel as SocketTextChannel;
            OverwritePermissions perms = channel.GetPermissionOverwrite(Context.Guild.EveryoneRole).Value;
            if (perms.SendMessages == PermValue.Deny)
                return CommandResult.FromError("This chat is already chilled.");

            await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, perms.Modify(sendMessages: PermValue.Deny));
            await ReplyAsync($"**{Context.User}** would like y'all to sit down and stfu for {duration} seconds!");

            await Task.Factory.StartNew(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(duration));
                await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, perms);
            });

            return CommandResult.FromSuccess();
        }

        [Alias("cope")]
        [Command("kick")]
        [Summary("Kick any member.")]
        [Remarks("$kick [user] <reason>")]
        public async Task<RuntimeResult> Kick(IGuildUser user, [Remainder] string reason = "")
        {
            if (user.IsBot)
                return CommandResult.FromError("Nope.");

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("roles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if ((snap.TryGetValue("houseRole", out ulong staffId) && user.RoleIds.Contains(staffId)) ||
            (snap.TryGetValue("senateRole", out ulong senateId) && user.RoleIds.Contains(senateId)))
            {
                return CommandResult.FromError($"You cannot kick **{user}** because they are a staff member.");
            }

            await user.KickAsync(reason);

            string response = $"**{Context.User}** has kicked **{user}**";
            response += string.IsNullOrWhiteSpace(reason) ? "." : $"for '{reason}'";
            await ReplyAsync(response);

            await (user as SocketUser).AddToStatsAsync(CultureInfo.CurrentCulture, Context.Guild, new Dictionary<string, string>
            {
                { "Kicks", "1" }
            });

            return CommandResult.FromSuccess();
        }

        [Alias("dilate")]
        [Command("mute")]
        [Summary("Mute any member for any amount of time with any reason.")]
        [Remarks("$mute [user] [duration][s/m/h/d] <reason>")]
        public async Task<RuntimeResult> Mute(IGuildUser user, string duration, [Remainder] string reason = "")
        {
            if (user.IsBot)
                return CommandResult.FromError("Nope.");

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("roles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("mutedRole", out ulong mutedId) && snap.TryGetValue("houseRole", out ulong staffId) && snap.TryGetValue("senateRole", out ulong senateId))
            {
                if (user.RoleIds.Contains(mutedId) || user.RoleIds.Contains(staffId) || user.RoleIds.Contains(senateId))
                    return CommandResult.FromError($"You cannot mute **{user}** because they are either already muted or a staff member.");

                if (int.TryParse(Regex.Match(duration, @"\d+").Value, out int time))
                {
                    Tuple<TimeSpan, string> resolved = ResolveDuration(duration, time, "muted", user);
                    string response = resolved.Item2;
                    if (resolved.Item1 == TimeSpan.Zero)
                        return CommandResult.FromError("You specified an invalid amount of time!");
                    response += string.IsNullOrWhiteSpace(reason) ? "." : $" for '{reason}'";
                    await ReplyAsync(response);

                    DocumentReference muteDoc = Program.database.Collection($"servers/{Context.Guild.Id}/mutes").Document(user.Id.ToString());
                    await Logger.Custom_UserMuted(user, Context.User, duration, reason);
                    await muteDoc.SetAsync(new { Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(resolved.Item1.TotalSeconds) });
                    await user.AddRoleAsync(mutedId);
                    await (user as SocketUser).AddToStatsAsync(CultureInfo.CurrentCulture, Context.Guild, new Dictionary<string, string>
                    {
                        { "Mutes", "1" }
                    });

                    return CommandResult.FromSuccess();
                }

                return CommandResult.FromError("You specified an invalid amount of time!");
            }

            return CommandResult.FromError("This server's staff and/or muted role(s) have yet to be set.");
        }

        [Alias("clear", "1984")]
        [Command("purge")]
        [Summary("Purge any amount of messages (Note: messages that are two weeks old or older will fail to delete).")]
        [Remarks("$purge [count] <user>")]
        public async Task<RuntimeResult> Purge(int count, IGuildUser user = null)
        {
            if (count <= 0)
                return CommandResult.FromError("Count must be more than zero.");

            IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync(count + 1).FlattenAsync();
            messages = messages.Where(msg => (DateTimeOffset.UtcNow - msg.Timestamp).TotalDays <= 14);
            if (user != null)
                messages = messages.Where(msg => msg.Author.Id == user.Id);

            if (!messages.Any())
                return CommandResult.FromError("No messages were deleted.");
            if (messages.Any(msg => (DateTimeOffset.UtcNow - msg.Timestamp).TotalDays > 14))
                await Context.User.NotifyAsync(Context.Channel, "Warning: Some messages were found to be older than 2 weeks and can't be deleted.");

            await (Context.Channel as SocketTextChannel)?.DeleteMessagesAsync(messages);
            await Logger.Custom_MessagesPurged(messages, Context.Guild);
            return CommandResult.FromSuccess();
        }

        [Command("unban")]
        [Summary("Unban any currently banned member.")]
        [Remarks("$unban [user]")]
        public async Task<RuntimeResult> Unban(IUser user)
        {
            IReadOnlyCollection<RestBan> bans = await Context.Guild.GetBansAsync();
            if (!bans.Any(ban => ban.User.Id == user.Id))
                return CommandResult.FromError("That user is not currently banned.");

            string userString = bans.FirstOrDefault(ban => ban.User.Id == user.Id).User.ToString();
            await ReplyAsync($"**{Context.User}** has unbanned **{userString}**.");
            await Context.Guild.RemoveBanAsync(user.Id);
            return CommandResult.FromSuccess();
        }

        [Alias("thaw")]
        [Command("unchill")]
        [Summary("Let chat talk now.")]
        [Remarks("$unchill")]
        public async Task<RuntimeResult> Unchill()
        {
            SocketTextChannel channel = Context.Channel as SocketTextChannel;
            OverwritePermissions perms = channel.GetPermissionOverwrite(Context.Guild.EveryoneRole).Value;
            if (perms.SendMessages == PermValue.Allow)
                return CommandResult.FromError("This chat is not chilled.");

            await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, perms.Modify(sendMessages: PermValue.Allow));
            await ReplyAsync($"**{Context.User}** took one for the team and unchilled early.");
            return CommandResult.FromSuccess();
        }

        [Command("unmute")]
        [Summary("Unmute any member.")]
        [Remarks("$unmute [user]")]
        public async Task<RuntimeResult> Unmute(IGuildUser user)
        {
            if (user.IsBot)
                return CommandResult.FromError("Nope.");

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("roles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("mutedRole", out ulong mutedId))
            {
                if (user.RoleIds.Contains(mutedId))
                {
                    await Logger.Custom_UserUnmuted(user, Context.User);
                    await ReplyAsync($"**{Context.User}** has unmuted **{user}**.");
                    await user.RemoveRoleAsync(mutedId);
                    return CommandResult.FromSuccess();
                }

                return CommandResult.FromError("That user is not muted.");
            }

            return CommandResult.FromError("This server's muted role has yet to be set.");
        }
    }
}
