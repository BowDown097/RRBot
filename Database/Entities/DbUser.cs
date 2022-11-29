namespace RRBot.Database.Entities;

[BsonCollection("users")]
[BsonIgnoreExtraElements]
public class DbUser : DbObject
{
    public override ObjectId Id { get; set; }
    
    public ulong GuildId { get; init; }
    public ulong UserId { get; init; }

    public Dictionary<string, string> Achievements { get; set; } = new();
    public long BlackHatTime { get; set; }
    public decimal Btc { get; set; }
    public long BullyCooldown { get; set; }
    public decimal Cash { get; set; } = 100;
    public long ChopCooldown { get; set; }
    public long CocaineRecoveryTime { get; set; }
    public long CocaineTime { get; set; }
    public Dictionary<string, int> Collectibles { get; set; } = new();
    public Dictionary<string, int> Consumables { get; set;  } = new();
    public List<string> Crates { get; set; } = new();
    public long DailyCooldown { get; set; }
    public long DealCooldown { get; set; }
    public long DigCooldown { get; set; }
    public bool DmNotifs { get; set; }
    public decimal Eth { get; set; }
    public long FarmCooldown { get; set; }
    public long FishCooldown { get; set; }
    public decimal GamblingMultiplier { get; set; } = 1;
    public string Gang { get; set; }
    public long HackCooldown { get; set; }
    public bool HasReachedAMilli { get; set; }
    public int Health { get; set; } = 100;
    public long HuntCooldown { get; set; }
    public long LootCooldown { get; set; }
    public decimal Ltc { get; set; }
    public long MineCooldown { get; set; }
    public long PacifistCooldown { get; set; }
    public List<string> PendingGangInvites { get; set; } = new();
    public Dictionary<string, long> Perks { get; set; } = new();
    public int Prestige { get; set; }
    public long PrestigeCooldown { get; set; }
    public long RapeCooldown { get; set; }
    public long RomanianFlagTime { get; set; }
    public long RobCooldown { get; set; }
    public long ScavengeCooldown { get; set; }
    public Dictionary<string, string> Stats { get; set; } = new();
    public long SlaveryCooldown { get; set; }
    public long TimeTillCash { get; set; }
    public List<string> Tools { get; set; } = new();
    public Dictionary<string, int> UsedConsumables { get; set; } = new() {
        { "Black Hat", 0 },
        { "Cocaine", 0 },
        { "Romanian Flag", 0 },
        { "Viagra", 0 }
    };
    public bool UsingSlots { get; set; }
    public bool WantsReplyPings { get; set; } = true;
    public long ViagraTime { get; set; }
    public long WhoreCooldown { get; set; }
    public decimal Xrp { get; set; }

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
                    decimal oldValue = decimal.Parse(Stats[kvp.Key][1..]);
                    decimal toAdd = decimal.Parse(kvp.Value[1..]);
                    Stats[kvp.Key] = (oldValue + toAdd).ToString("C2", culture);
                }
                else
                {
                    decimal oldValue = decimal.Parse(Stats[kvp.Key]);
                    decimal toAdd = decimal.Parse(kvp.Value);
                    Stats[kvp.Key] = (oldValue + toAdd).ToString("0.####");
                }
            }
            else
            {
                Stats.Add(kvp.Key, kvp.Value);
            }
        }
    }

    public async Task SetCash(IUser user, decimal amount, IMessageChannel channel = null, string message = "", bool showPrestigeMessage = true)
    {
        if (user.IsBot)
            return;
        if (amount < 0)
            amount = 0;

        amount = Math.Round(amount, 2) * Constants.CashMultiplier;

        decimal difference = amount - Cash;
        if (Prestige > 0 && difference > 0 && channel != null)
        {
            decimal prestigeCash = difference * 0.20m * Prestige;
            difference += prestigeCash;
            if (showPrestigeMessage)
                message += $"\n*(+{prestigeCash:C2} from Prestige)*";
        }

        await SetCashWithoutAdjustment(user, Cash + difference, channel, message);
    }

    public async Task SetCashWithoutAdjustment(IUser user, decimal amount, IMessageChannel channel = null, string message = "")
    {
        IGuildUser guildUser = user as IGuildUser;
        Cash = amount;
        
        if (channel != null)
            await user.NotifyAsync(channel, message);

        DbConfigRanks ranks = await MongoManager.FetchConfigAsync<DbConfigRanks>(guildUser.GuildId);
        foreach (KeyValuePair<int, decimal> kvp in ranks.Costs)
        {
            decimal neededCash = kvp.Value * (1 + 0.5m * Prestige);
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
        DbConfigRanks ranks = await MongoManager.FetchConfigAsync<DbConfigRanks>(guild.Id);
        ulong highest = ranks.Ids.OrderByDescending(kvp => kvp.Key).FirstOrDefault().Value;
        if (user.GetRoleIds().Contains(highest))
            secs = (long)(secs * 0.80);
        // cocaine cooldown reducer
        secs = (long)(secs * (1 - 0.10 * UsedConsumables["Cocaine"]));

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

        if (GamblingMultiplier == 1.0m && Constants.GamblingAchievements.All(a => Achievements.ContainsKey(a)))
        {
            GamblingMultiplier = 1.1m;
            await user.NotifyAsync(channel, "Congratulations! You've acquired every gambling achievement. Enjoy this **1.1x gambling multiplier**!");
        }
    }
}