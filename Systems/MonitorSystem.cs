namespace RRBot.Systems;
public class MonitorSystem
{
    private readonly DiscordSocketClient client;
    private readonly FirestoreDb database;
    private static readonly Dictionary<string, long> emptySL = new();

    public MonitorSystem(DiscordSocketClient client, FirestoreDb database)
    {
        this.client = client;
        this.database = database;
    }

    public async Task Initialise()
    {
        await Task.Factory.StartNew(async () => await StartBanMonitorAsync());
        await Task.Factory.StartNew(async () => await StartChillMonitorAsync());
        await Task.Factory.StartNew(async () => await StartConsumableMonitorAsync());
        await Task.Factory.StartNew(async () => await StartPerkMonitorAsync());
        await Task.Factory.StartNew(async () => await StartPotMonitorAsync());
    }

    private async Task StartBanMonitorAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            foreach (SocketGuild guild in client.Guilds)
            {
                QuerySnapshot bans = await database.Collection($"servers/{guild.Id}/bans").GetSnapshotAsync();
                foreach (DocumentSnapshot banDoc in bans.Documents)
                {
                    ulong userId = Convert.ToUInt64(banDoc.Id);
                    DbBan ban = await DbBan.GetById(guild.Id, userId);

                    if (await guild.GetBanAsync(userId) is null)
                    {
                        await ban.Reference.DeleteAsync();
                        continue;
                    }

                    if (ban.Time <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
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
                foreach (DocumentSnapshot chillDoc in chills.Documents)
                {
                    ulong channelId = Convert.ToUInt64(chillDoc.Id);
                    DbChill chill = await DbChill.GetById(guild.Id, channelId);
                    SocketTextChannel channel = guild.GetTextChannel(channelId);
                    OverwritePermissions perms = channel.GetPermissionOverwrite(guild.EveryoneRole) ?? OverwritePermissions.InheritAll;

                    if (perms.SendMessages != PermValue.Deny)
                    {
                        await chill.Reference.DeleteAsync();
                        continue;
                    }

                    if (chill.Time <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    {
                        await channel.AddPermissionOverwriteAsync(guild.EveryoneRole, perms.Modify(sendMessages: PermValue.Inherit));
                        await channel.SendMessageAsync("This channel has thawed out! Continue the chaos!");
                        await chill.Reference.DeleteAsync();
                    }
                }
            }
        }
    }

    private async Task StartConsumableMonitorAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            foreach (SocketGuild guild in client.Guilds)
            {
                // cocaine
                QuerySnapshot usersWCocaine = await database.Collection($"servers/{guild.Id}/users").WhereNotEqualTo("UsedConsumables.Cocaine", 0).GetSnapshotAsync();
                foreach (DocumentSnapshot snap in usersWCocaine.Documents)
                {
                    DbUser user = await DbUser.GetById(guild.Id, Convert.ToUInt64(snap.Id));
                    if (user.CocaineTime <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                        user.UsedConsumables["Cocaine"] = 0;
                }
                // recovery
                QuerySnapshot usersUnderRecovery = await database.Collection($"servers/{guild.Id}/users").WhereNotEqualTo("CocaineRecoveryTime", 0).GetSnapshotAsync();
                foreach (DocumentSnapshot snap in usersUnderRecovery.Documents)
                {
                    DbUser user = await DbUser.GetById(guild.Id, Convert.ToUInt64(snap.Id));
                    if (user.CocaineRecoveryTime <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                        user.CocaineRecoveryTime = 0;
                }
                // romanian flag
                QuerySnapshot usersWFlag = await database.Collection($"servers/{guild.Id}/users").WhereNotEqualTo("UsedConsumables.Romanian Flag", 0).GetSnapshotAsync();
                foreach (DocumentSnapshot snap in usersWFlag.Documents)
                {
                    DbUser user = await DbUser.GetById(guild.Id, Convert.ToUInt64(snap.Id));
                    if (user.RomanianFlagTime <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                        user.UsedConsumables["Romanian Flag"] = 0;
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
                QuerySnapshot usersWPerks = await database.Collection($"servers/{guild.Id}/users").WhereNotEqualTo("Perks", emptySL).GetSnapshotAsync();
                foreach (DocumentSnapshot snap in usersWPerks.Documents)
                {
                    ulong userId = Convert.ToUInt64(snap.Id);
                    DbUser user = await DbUser.GetById(guild.Id, userId);
                    foreach (KeyValuePair<string, long> kvp in user.Perks)
                    {
                        if (kvp.Value <= DateTimeOffset.UtcNow.ToUnixTimeSeconds() && kvp.Key != "Pacifist")
                        {
                            user.Perks.Remove(kvp.Key);
                            if (kvp.Key == "Multiperk" && user.Perks.Count >= 2)
                            {
                                string lastPerk = user.Perks.Last().Key;
                                Perk perk = ItemSystem.GetItem(lastPerk) as Perk;
                                SocketUser socketUser = guild.GetUser(userId);
                                await user.SetCash(socketUser, user.Cash + perk.Price);
                                user.Perks.Remove(lastPerk);
                            }
                        }
                    }
                }
            }
        }
    }

    private async Task StartPotMonitorAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            foreach (SocketGuild guild in client.Guilds)
            {
                DbPot pot = await DbPot.GetById(guild.Id);
                if (pot.EndTime <= DateTimeOffset.UtcNow.ToUnixTimeSeconds() && pot.EndTime != -1)
                {
                    ulong luckyGuy = pot.DrawMember();
                    SocketGuildUser luckyUser = guild.GetUser(luckyGuy);
                    DbUser luckyDbUser = await DbUser.GetById(guild.Id, luckyGuy);

                    double winnings = pot.Value * (1 - (Constants.POT_FEE / 100));
                    await luckyDbUser.SetCash(luckyUser, luckyDbUser.Cash + winnings);

                    DbConfigChannels channelsConfig = await DbConfigChannels.GetById(guild.Id);
                    if (channelsConfig.PotChannel != default)
                    {
                        SocketTextChannel channel = guild.GetTextChannel(channelsConfig.PotChannel);
                        await channel.SendMessageAsync($"The pot has been drawn, and our LUCKY WINNER is {luckyUser.Mention}!!! After a fee of {Constants.POT_FEE}%, they have won {winnings:C2} with a {pot.GetMemberOdds(luckyGuy.ToString())}% chance of winning the pot!");
                    }

                    pot.EndTime = -1;
                    pot.Members = new();
                    pot.Value = 0;
                }
            }
        }
    }
}