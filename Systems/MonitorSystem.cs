namespace RRBot.Systems;
public class MonitorSystem(BaseSocketClient client)
{
    public async Task Initialize()
    {
        await Task.Factory.StartNew(async () => await StartBanMonitorAsync());
        await Task.Factory.StartNew(async () => await StartChillMonitorAsync());
        await Task.Factory.StartNew(async () => await StartConsumableMonitorAsync());
        await Task.Factory.StartNew(async () => await StartElectionMonitorAsync());
        await Task.Factory.StartNew(async () => await StartPerkMonitorAsync());
        await Task.Factory.StartNew(async () => await StartPotMonitorAsync());
    }

    private async Task StartBanMonitorAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            foreach (DbBan ban in await MongoManager.Bans.Aggregate().ToListAsync())
            {
                SocketGuild guild = client.GetGuild(ban.GuildId);
                if (await guild.GetBanAsync(ban.UserId) is null)
                {
                    await MongoManager.DeleteObjectAsync(ban);
                    continue;
                }

                if (ban.Time > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) 
                    continue;
                await guild.RemoveBanAsync(ban.UserId);
                await MongoManager.DeleteObjectAsync(ban);
            }
        }
    }

    private async Task StartChillMonitorAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            foreach (DbChill chill in await MongoManager.Chills.Aggregate().ToListAsync())
            {
                SocketGuild guild = client.GetGuild(chill.GuildId);
                SocketTextChannel channel = guild.GetTextChannel(chill.ChannelId);
                OverwritePermissions perms = channel.GetPermissionOverwrite(guild.EveryoneRole) ?? OverwritePermissions.InheritAll;

                if (perms.SendMessages != PermValue.Deny)
                {
                    await MongoManager.DeleteObjectAsync(chill);
                    continue;
                }

                if (chill.Time > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) 
                    continue;
                await channel.AddPermissionOverwriteAsync(guild.EveryoneRole, perms.Modify(sendMessages: PermValue.Inherit));
                await channel.SendMessageAsync("This channel has thawed out! Continue the chaos!");
                await MongoManager.DeleteObjectAsync(chill);
            }
        }
    }

    private static async Task StartConsumableMonitorAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            await ConsumableCheck("Black Hat", "BlackHatTime");
            await ConsumableCheck("Cocaine", "CocaineTime");
            await ConsumableCheck("CocaineRecoveryTime", "CocaineRecoveryTime", true);
            await ConsumableCheck("Romanian Flag", "RomanianFlagTime");
            await ConsumableCheck("Viagra", "ViagraTime");
        }
    }

    private async Task StartElectionMonitorAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            IAsyncCursor<DbElection> elections = await MongoManager.Elections.FindAsync(e =>
                e.EndTime <= DateTimeOffset.UtcNow.ToUnixTimeSeconds() && e.EndTime != -1);
            await elections.ForEachAsync(async election =>
            {
                SocketGuild guild = client.GetGuild(election.GuildId);
                DbConfigChannels channels = await MongoManager.FetchConfigAsync<DbConfigChannels>(election.GuildId);
                await Polls.ConcludeElection(election, channels, guild);
                await MongoManager.UpdateObjectAsync(election);
            });
        }
    }

    private async Task StartPerkMonitorAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            IAsyncCursor<DbUser> users = await MongoManager.Users.FindAsync(u =>
                u.Perks.Any(kvp => kvp.Value <= DateTimeOffset.UtcNow.ToUnixTimeSeconds() && kvp.Key != "Pacifist"));
            await users.ForEachAsync(async user =>
            {
                foreach (KeyValuePair<string, long> kvp in user.Perks
                    .Where(kvp => kvp.Value <= DateTimeOffset.UtcNow.ToUnixTimeSeconds() && kvp.Key != "Pacifist"))
                {
                    user.Perks.Remove(kvp.Key);
                    if (kvp.Key != "Multiperk" || user.Perks.Count < 2)
                        continue;
 
                    string lastPerk = user.Perks.Last().Key;
                    Perk perk = (Perk)ItemSystem.GetItem(lastPerk)!;

                    SocketGuild guild = client.GetGuild(user.GuildId);
                    SocketUser socketUser = guild.GetUser(user.UserId);
                    await user.SetCash(socketUser, user.Cash + perk.Price);
                    user.Perks.Remove(lastPerk);
                }
                
                await MongoManager.UpdateObjectAsync(user);
            });
        }
    }

    private async Task StartPotMonitorAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            IAsyncCursor<DbPot> pots = await MongoManager.Pots.FindAsync(p =>
                p.EndTime <= DateTimeOffset.UtcNow.ToUnixTimeSeconds() && p.EndTime != -1);
            await pots.ForEachAsync(async pot =>
            {
                ulong luckyGuy = pot.DrawMember();
                SocketGuild guild = client.GetGuild(pot.GuildId);
                SocketGuildUser luckyUser = guild.GetUser(luckyGuy);
                DbUser luckyDbUser = await MongoManager.FetchUserAsync(luckyGuy, guild.Id);

                decimal winnings = pot.Value * (1 - Constants.PotFee / 100);
                await luckyDbUser.SetCash(luckyUser, luckyDbUser.Cash + winnings);
                
                DbConfigChannels channels = await MongoManager.FetchConfigAsync<DbConfigChannels>(guild.Id);
                if (channels.PotChannel != default)
                {
                    SocketTextChannel channel = guild.GetTextChannel(channels.PotChannel);
                    await channel.SendMessageAsync($"The pot has been drawn, and our LUCKY WINNER is {luckyUser.Mention}!!! After a fee of {Constants.PotFee}%, they have won {winnings:C2} with a {pot.GetMemberOdds(luckyGuy)}% chance of winning the pot!");
                }

                pot.EndTime = -1;
                pot.Members.Clear();
                pot.Value = 0;

                await MongoManager.UpdateObjectAsync(luckyDbUser);
                await MongoManager.UpdateObjectAsync(pot);
            });
        }
    }

    private static async Task ConsumableCheck(string name, string timeKey, bool recoveryTime = false)
    {
        IAsyncCursor<DbUser> usersWConsumable = await MongoManager.Users.FindAsync(u =>
            (long)u[timeKey] <= DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            && (recoveryTime ? u.CocaineRecoveryTime > 0 : u.UsedConsumables[name] > 0));
        await usersWConsumable.ForEachAsync(async user =>
        {
            if (recoveryTime)
                user[name] = 0;
            else
                user.UsedConsumables[name] = 0;
            await MongoManager.UpdateObjectAsync(user);
        });
    }
}