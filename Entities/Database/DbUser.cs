namespace RRBot.Entities.Database;
[FirestoreData]
public class DbUser : DbObject
{
    [FirestoreDocumentId]
    public override DocumentReference Reference { get; set; }
    [FirestoreProperty]
    public Dictionary<string, string> Achievements { get; set; } = new();
    [FirestoreProperty]
    public double BTC { get; set; }
    [FirestoreProperty]
    public long BullyCooldown { get; set; }
    [FirestoreProperty]
    public double Cash { get; set; }
    [FirestoreProperty]
    public long ChopCooldown { get; set; }
    [FirestoreProperty]
    public long DailyCooldown { get; set; }
    [FirestoreProperty]
    public long DealCooldown { get; set; }
    [FirestoreProperty]
    public long DigCooldown { get; set; }
    [FirestoreProperty]
    public bool DMNotifs { get; set; }
    [FirestoreProperty]
    public double ETH { get; set; }
    [FirestoreProperty]
    public long FarmCooldown { get; set; }
    [FirestoreProperty]
    public long FishCooldown { get; set; }
    [FirestoreProperty]
    public long HackCooldown { get; set; }
    [FirestoreProperty]
    public long HuntCooldown { get; set; }
    [FirestoreProperty]
    public long LootCooldown { get; set; }
    [FirestoreProperty]
    public double LTC { get; set; }
    [FirestoreProperty]
    public long MineCooldown { get; set; }
    [FirestoreProperty]
    public bool NoReplyPings { get; set; }
    [FirestoreProperty]
    public long PacifistCooldown { get; set; }
    [FirestoreProperty]
    public Dictionary<string, long> Perks { get; set; } = new();
    [FirestoreProperty]
    public bool RankupNotifs { get; set; }
    [FirestoreProperty]
    public long RapeCooldown { get; set; }
    [FirestoreProperty]
    public long RobCooldown { get; set; }
    [FirestoreProperty]
    public long ScavengeCooldown { get; set; }
    [FirestoreProperty]
    public Dictionary<string, string> Stats { get; set; } = new();
    [FirestoreProperty]
    public long SlaveryCooldown { get; set; }
    [FirestoreProperty]
    public long SupportCooldown { get; set; }
    [FirestoreProperty]
    public long TimeTillCash { get; set; }
    [FirestoreProperty]
    public List<string> Tools { get; set; } = new();
    [FirestoreProperty]
    public bool UsingSlots { get; set; }
    [FirestoreProperty]
    public long WhoreCooldown { get; set; }
    [FirestoreProperty]
    public double XRP { get; set; }

    public object this[string name]
    {
        get
        {
            PropertyInfo property = typeof(DbUser).GetProperty(name);
            if (!property.CanRead)
                throw new ArgumentException("Property does not exist");
            return property.GetValue(this, null);
        }
        set
        {
            PropertyInfo property = typeof(DbUser).GetProperty(name);
            if (!property.CanWrite)
                throw new ArgumentException("Property does not exist");
            property.SetValue(this, value);
        }
    }

    public static async Task<DbUser> GetById(ulong guildId, ulong userId, bool useCache = true)
    {
        if (useCache && MemoryCache.Default.Contains($"user-{guildId}-{userId}"))
            return (DbUser)MemoryCache.Default.Get($"user-{guildId}-{userId}");

        DocumentReference doc = Program.database.Collection($"servers/{guildId}/users").Document(userId.ToString());
        DocumentSnapshot snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
        {
            await doc.CreateAsync(new { Cash = 100.0 });
            return await GetById(guildId, userId);
        }

        DbUser user = snap.ConvertTo<DbUser>();
        if (useCache)
            MemoryCache.Default.CacheDatabaseObject($"user-{guildId}-{userId}", user);
        return user;
    }

    public void AddToStat(string stat, string value) => AddToStats(new() {{ stat, value }});

    public void AddToStats(Dictionary<string, string> statsToAddTo)
    {
        CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
        culture.NumberFormat.CurrencyNegativePattern = 2;
        foreach (KeyValuePair<string, string> kvp in statsToAddTo)
        {
            if (Stats.ContainsKey(kvp.Key))
            {
                if (kvp.Value[0] == '$')
                {
                    double oldValue = double.Parse(Stats[kvp.Key][1..]);
                    double toAdd = double.Parse(kvp.Value[1..]);
                    Stats[kvp.Key] = (oldValue + toAdd).ToString("C2", culture);
                }
                else
                {
                    double oldValue = double.Parse(Stats[kvp.Key]);
                    double toAdd = double.Parse(kvp.Value);
                    Stats[kvp.Key] = (oldValue + toAdd).ToString();
                }
            }
            else
            {
                Stats.Add(kvp.Key, kvp.Value);
            }
        }
    }

    public async Task SetCash(IUser user, double amount)
    {
        if (user.IsBot)
            return;
        if (amount < 0)
            amount = 0;

        IGuildUser guildUser = user as IGuildUser;
        amount = Math.Round(amount, 2) * Constants.CASH_MULTIPLIER;
        Cash = amount;

        DbConfigRanks ranks = await DbConfigRanks.GetById(guildUser.GuildId);
        foreach (KeyValuePair<string, double> kvp in ranks.Costs)
        {
            double neededCash = kvp.Value;
            ulong roleId = ranks.Ids[kvp.Key];
            if (amount >= neededCash && !guildUser.RoleIds.Contains(roleId))
                await guildUser.AddRoleAsync(roleId);
            else if (amount <= neededCash && guildUser.RoleIds.Contains(roleId))
                await guildUser.RemoveRoleAsync(roleId);
        }
    }

    public async Task UnlockAchievement(string name, string desc, IUser user, IMessageChannel channel, double reward = 0)
    {
        if (Achievements.ContainsKey(name))
            return;

        Achievements.Add(name, desc);
        string description = $"GG {user}, you unlocked an achievement.\n**{name}**: {desc}";
        if (reward != 0)
        {
            Cash += reward;
            description += $"\nReward: {reward:C2}";
        }

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Achievement Get!")
            .WithDescription(description);
        await channel.SendMessageAsync(embed: embed.Build());
    }
}