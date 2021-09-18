using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using RRBot.Entities;
using RRBot.Systems;

namespace RRBot
{
    public class Monitors
    {
        private readonly DiscordSocketClient client;
        private readonly FirestoreDb database;

        public Monitors(DiscordSocketClient client, FirestoreDb database)
        {
            this.client = client;
            this.database = database;
        }

        public async Task Initialise()
        {
            await Task.Factory.StartNew(async () => await StartBanMonitorAsync());
            await Task.Factory.StartNew(async () => await StartChillMonitorAsync());
            await Task.Factory.StartNew(async () => await StartMuteMonitorAsync());
            await Task.Factory.StartNew(async () => await StartPerkMonitorAsync());
        }

        private async Task StartBanMonitorAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
                foreach (SocketGuild guild in client.Guilds)
                {
                    QuerySnapshot bans = await database.Collection($"servers/{guild.Id}/bans").GetSnapshotAsync();
                    foreach (DocumentSnapshot ban in bans.Documents)
                    {
                        long timestamp = ban.GetValue<long>("Time");
                        ulong userId = Convert.ToUInt64(ban.Id);

                        if (!(await guild.GetBansAsync()).Any(ban => ban.User.Id == userId))
                        {
                            await ban.Reference.DeleteAsync();
                            continue;
                        }

                        if (timestamp <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                        {
                            await guild.RemoveBanAsync(userId);
                            await ban.Reference.DeleteAsync();
                        }
                    }
                }
            }
        }

        private async Task StartChillMonitorAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
                foreach (SocketGuild guild in client.Guilds)
                {
                    QuerySnapshot chills = await database.Collection($"servers/{guild.Id}/chills").GetSnapshotAsync();
                    foreach (DocumentSnapshot chill in chills.Documents)
                    {
                        long timestamp = chill.GetValue<long>("Time");
                        SocketTextChannel channel = guild.GetTextChannel(Convert.ToUInt64(chill.Id));
                        OverwritePermissions perms = channel.GetPermissionOverwrite(guild.EveryoneRole) ?? OverwritePermissions.InheritAll;

                        if (perms.SendMessages != PermValue.Deny)
                        {
                            await chill.Reference.DeleteAsync();
                            continue;
                        }

                        if (timestamp <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                        {
                            await channel.AddPermissionOverwriteAsync(guild.EveryoneRole, perms.Modify(sendMessages: PermValue.Inherit));
                            await channel.SendMessageAsync("This channel has thawed out! Continue the chaos!");
                            await chill.Reference.DeleteAsync();
                        }
                    }
                }
            }
        }

        private async Task StartMuteMonitorAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
                foreach (SocketGuild guild in client.Guilds)
                {
                    DocumentReference doc = database.Collection($"servers/{guild.Id}/config").Document("roles");
                    DocumentSnapshot snap = await doc.GetSnapshotAsync();
                    if (snap.TryGetValue("mutedRole", out ulong mutedId))
                    {
                        QuerySnapshot mutes = await database.Collection($"servers/{guild.Id}/mutes").GetSnapshotAsync();
                        foreach (DocumentSnapshot mute in mutes.Documents)
                        {
                            long timestamp = mute.GetValue<long>("Time");
                            SocketGuildUser user = guild.GetUser(Convert.ToUInt64(mute.Id));

                            if (timestamp <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                            {
                                if (user != null) await user.RemoveRoleAsync(mutedId);
                                await mute.Reference.DeleteAsync();
                            }
                        }
                    }
                }
            }
        }

        private async Task StartPerkMonitorAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
                foreach (SocketGuild guild in client.Guilds)
                {
                    QuerySnapshot usersWPerks = await database.Collection($"servers/{guild.Id}/users")
                        .WhereNotEqualTo("perks", null).WhereNotEqualTo("perks", new Dictionary<string, long>()).GetSnapshotAsync();
                    foreach (DocumentSnapshot snap in usersWPerks.Documents)
                    {
                        ulong userId = Convert.ToUInt64(snap.Id);
                        DbUser user = await DbUser.GetById(guild.Id, userId);
                        foreach (KeyValuePair<string, long> perk in user.Perks)
                        {
                            if (perk.Value <= DateTimeOffset.UtcNow.ToUnixTimeSeconds() && perk.Key != "Pacifist")
                            {
                                user.Perks.Remove(perk.Key);
                                if (perk.Key == "Multiperk" && user.Perks.Count >= 2)
                                {
                                    string lastPerk = user.Perks.Last().Key;
                                    Tuple<string, string, double, long> tuple = Array.Find(Items.perks, p => p.Item1 == lastPerk);
                                    SocketUser socketUser = guild.GetUser(userId);
                                    await user.SetCash(socketUser, null, user.Cash + tuple.Item3);
                                    user.Perks.Remove(lastPerk);
                                    await user.Write();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}