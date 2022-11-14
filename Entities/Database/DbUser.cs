namespace RRBot.Entities.Database;
[FirestoreData]
public class DbUser : DbObject
{
    #region Variables
    [FirestoreDocumentId]
    public override DocumentReference Reference { get; set; }
    [FirestoreProperty]
    public Dictionary<string, string> Achievements { get; set; } = new();
    [FirestoreProperty]
    public long BlackHatTime { get; set; }
    [FirestoreProperty]
    public double Btc { get; set; }
    [FirestoreProperty]
    public long BullyCooldown { get; set; }
    [FirestoreProperty]
    public double Cash { get; set; }
    [FirestoreProperty]
    public long ChopCooldown { get; set; }
    [FirestoreProperty]
    public long CocaineRecoveryTime { get; set; }
    [FirestoreProperty]
    public long CocaineTime { get; set; }
    [FirestoreProperty]
    public Dictionary<string, int> Collectibles { get; set; } = new();
    [FirestoreProperty]
    public Dictionary<string, int> Consumables { get; set; } = new();
    [FirestoreProperty]
    public List<string> Crates { get; set; } = new();
    [FirestoreProperty]
    public long DailyCooldown { get; set; }
    [FirestoreProperty]
    public long DealCooldown { get; set; }
    [FirestoreProperty]
    public long DigCooldown { get; set; }
    [FirestoreProperty]
    public bool DmNotifs { get; set; }
    [FirestoreProperty]
    public double Eth { get; set; }
    [FirestoreProperty]
    public long FarmCooldown { get; set; }
    [FirestoreProperty]
    public long FishCooldown { get; set; }
    [FirestoreProperty]
    public double GamblingMultiplier { get; set; } = 1.0;
    [FirestoreProperty]
    public string Gang { get; set; }
    [FirestoreProperty]
    public long HackCooldown { get; set; }
    [FirestoreProperty]
    public bool HasReachedAMilli { get; set; }
    [FirestoreProperty]
    public int Health { get; set; } = 100;
    [FirestoreProperty]
    public long HuntCooldown { get; set; }
    [FirestoreProperty]
    public long LootCooldown { get; set; }
    [FirestoreProperty]
    public double Ltc { get; set; }
    [FirestoreProperty]
    public long MineCooldown { get; set; }
    [FirestoreProperty]
    public long PacifistCooldown { get; set; }
    [FirestoreProperty]
    public List<string> PendingGangInvites { get; set; } = new();
    [FirestoreProperty]
    public Dictionary<string, long> Perks { get; set; } = new();
    [FirestoreProperty]
    public int Prestige { get; set; }
    [FirestoreProperty]
    public long PrestigeCooldown { get; set; }
    [FirestoreProperty]
    public long RapeCooldown { get; set; }
    [FirestoreProperty]
    public long RomanianFlagTime { get; set; }
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
    public Dictionary<string, int> UsedConsumables { get; set; } = new() {
        { "Black Hat", 0 },
        { "Cocaine", 0 },
        { "Romanian Flag", 0 },
        { "Viagra", 0 }
    };
    [FirestoreProperty]
    public bool UsingSlots { get; set; }
    [FirestoreProperty]
    public bool WantsReplyPings { get; set; } = true;
    [FirestoreProperty]
    public long ViagraTime { get; set; }
    [FirestoreProperty]
    public long WhoreCooldown { get; set; }
    [FirestoreProperty]
    public double Xrp { get; set; }
    #endregion

    #region Methods
    public object this[string name]
    {
        get
        {
            PropertyInfo property = typeof(DbUser).GetProperty(name);
            if (property?.CanRead == false)
                throw new ArgumentException("Property does not exist");
            return property?.GetValue(this, null);
        }
        set
        {
            PropertyInfo property = typeof(DbUser).GetProperty(name);
            if (property?.CanWrite == false)
                throw new ArgumentException("Property does not exist");
            property?.SetValue(this, value);
        }
    }

    public static async Task<DbUser> GetById(ulong guildId, ulong userId, bool useCache = true)
    {
        if (useCache && MemoryCache.Default.Contains($"user-{guildId}-{userId}"))
            return (DbUser)MemoryCache.Default.Get($"user-{guildId}-{userId}");

        DocumentReference doc = Program.Database.Collection($"servers/{guildId}/users").Document(userId.ToString());
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

    public void AddToStat(string stat, string value) => AddToStats(new Dictionary<string, string> {{ stat, value }});

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
                    Stats[kvp.Key] = (oldValue + toAdd).ToString("0.####");
                }
            }
            else
            {
                Stats.Add(kvp.Key, kvp.Value);
            }
        }
    }

    public async Task SetCash(IUser user, double amount, IMessageChannel channel = null, string message = "", bool showPrestigeMessage = true)
    {
        if (user.IsBot)
            return;
        if (amount < 0)
            amount = 0;

        amount = Math.Round(amount, 2) * Constants.CashMultiplier;

        double difference = amount - Cash;
        if (Prestige > 0 && difference > 0 && channel != null)
        {
            double prestigeCash = difference * (0.20 * Prestige);
            difference += prestigeCash;
            if (showPrestigeMessage)
                message += $"\n*(+{prestigeCash:C2} from Prestige)*";
        }

        await SetCashWithoutAdjustment(user, Cash + difference, channel, message);
    }

    public async Task SetCashWithoutAdjustment(IUser user, double amount, IMessageChannel channel = null, string message = "")
    {
        IGuildUser guildUser = user as IGuildUser;
        Cash = amount;
        
        if (channel != null)
            await user.NotifyAsync(channel, message);

        DbConfigRanks ranks = await DbConfigRanks.GetById(guildUser.GuildId);
        foreach (KeyValuePair<string, double> kvp in ranks.Costs)
        {
            double neededCash = kvp.Value * (1 + (0.5 * Prestige));
            ulong roleId = ranks.Ids[kvp.Key];
            if (Cash >= neededCash && !guildUser.RoleIds.Contains(roleId))
                await guildUser.AddRoleAsync(roleId);
            else if (Cash <= neededCash && guildUser.RoleIds.Contains(roleId))
                await guildUser.RemoveRoleAsync(roleId);
        }
    }

    public async Task SetCooldown(string name, long secs, IGuild guild, IUser user)
    {
        // speed demon cooldown reducer
        if (Perks.ContainsKey("Speed Demon"))
            secs = (long)(secs * 0.85);
        // highest rank cooldown reducer
        DbConfigRanks ranks = await DbConfigRanks.GetById(guild.Id);
        ulong highest = ranks.Ids.OrderByDescending(kvp => int.Parse(kvp.Key)).FirstOrDefault().Value;
        if (user.GetRoleIds().Contains(highest))
            secs = (long)(secs * 0.80);
        // cocaine cooldown reducer
        secs = (long)(secs * (1 - (0.10 * UsedConsumables["Cocaine"])));

        this[name] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(secs);
    }

    public async Task UnlockAchievement(string name, IUser user, IMessageChannel channel)
    {
        if (Achievements.Any(kvp => kvp.Key.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return;

        Achievement ach = Array.Find(Constants.DefaultAchievements, ach => ach.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        Achievements.Add(ach.Name, ach.Description);
        string description = $"GG {user}, you unlocked an achievement.\n**{ach.Name}**: {ach.Description}";
        if (ach.Reward > 0)
        {
            Cash += ach.Reward;
            description += $"\nReward: {ach.Reward:C2}";
        }

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Achievement Get!")
            .WithDescription(description);
        await channel.SendMessageAsync(embed: embed.Build());

        if (GamblingMultiplier == 1.0 && Constants.GamblingAchievements.All(a => Achievements.ContainsKey(a)))
        {
            GamblingMultiplier = 1.1;
            await user.NotifyAsync(channel, "Congratulations! You've acquired every gambling achievement. Enjoy this **1.1x gambling multiplier**!");
        }
    }
    #endregion
}