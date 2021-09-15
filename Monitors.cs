using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Google.Cloud.Firestore;

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
    }
}