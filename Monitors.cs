using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using RRBot.Extensions;
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
                        .WhereNotEqualTo("perks", null).GetSnapshotAsync();
                    foreach (DocumentSnapshot user in usersWPerks.Documents)
                    {
                        Dictionary<string, long> usrPerks = user.GetValue<Dictionary<string, long>>("perks");
                        foreach (KeyValuePair<string, long> perk in usrPerks)
                        {
                            if (perk.Value <= DateTimeOffset.UtcNow.ToUnixTimeSeconds() && perk.Key != "Pacifist")
                            {
                                usrPerks.Remove(perk.Key);
                                if (perk.Key == "Multiperk" && usrPerks.Count >= 2)
                                {
                                    string lastPerk = usrPerks.Last().Key;
                                    Tuple<string, string, double, long> tuple = Array.Find(Items.perks, p => p.Item1 == lastPerk);
                                    double cash = user.GetValue<double>("cash");
                                    cash += tuple.Item3;
                                    usrPerks.Remove(lastPerk);
                                    SocketUser userObj = guild.GetUser(Convert.ToUInt64(user.Id));
                                    await CashSystem.SetCash(userObj, null, cash);
                                }

                                Dictionary<string, object> newPerks = new() { { "perks", usrPerks } };
                                await user.Reference.UpdateAsync(newPerks);
                            }
                        }
                    }
                }
            }
        }
    }
}