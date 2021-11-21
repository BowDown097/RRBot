namespace RRBot.Entities.Database
{
    [FirestoreData]
    public class DbUser
    {
        [FirestoreDocumentId]
        public DocumentReference Reference { get; set; }
        [FirestoreProperty("achievements")]
        public Dictionary<string, string> Achievements { get; set; } = new();
        [FirestoreProperty("btc")]
        public double BTC { get; set; }
        [FirestoreProperty("bullyCooldown")]
        public long BullyCooldown { get; set; }
        [FirestoreProperty("cash")]
        public double Cash { get; set; }
        [FirestoreProperty("chopCooldown")]
        public long ChopCooldown { get; set; }
        [FirestoreProperty("dealCooldown")]
        public long DealCooldown { get; set; }
        [FirestoreProperty("digCooldown")]
        public long DigCooldown { get; set; }
        [FirestoreProperty("dmNotifs")]
        public bool DMNotifs { get; set; }
        [FirestoreProperty("doge")]
        public double DOGE { get; set; }
        [FirestoreProperty("eth")]
        public double ETH { get; set; }
        [FirestoreProperty("farmCooldown")]
        public long FarmCooldown { get; set; }
        [FirestoreProperty("fishCooldown")]
        public long FishCooldown { get; set; }
        [FirestoreProperty("hackCooldown")]
        public long HackCooldown { get; set; }
        [FirestoreProperty("huntCooldown")]
        public long HuntCooldown { get; set; }
        [FirestoreProperty("items")]
        public List<string> Items { get; set; } = new();
        [FirestoreProperty("lootCooldown")]
        public long LootCooldown { get; set; }
        [FirestoreProperty("ltc")]
        public double LTC { get; set; }
        [FirestoreProperty("mineCooldown")]
        public long MineCooldown { get; set; }
        [FirestoreProperty("noReplyPings")]
        public bool NoReplyPings { get; set; }
        [FirestoreProperty("pacifistCooldown")]
        public long PacifistCooldown { get; set; }
        [FirestoreProperty("perks")]
        public Dictionary<string, long> Perks { get; set; } = new();
        [FirestoreProperty("rankupNotifs")]
        public bool RankupNotifs { get; set; }
        [FirestoreProperty("rapeCooldown")]
        public long RapeCooldown { get; set; }
        [FirestoreProperty("robCooldown")]
        public long RobCooldown { get; set; }
        [FirestoreProperty("stats")]
        public Dictionary<string, string> Stats { get; set; } = new();
        [FirestoreProperty("slaveryCooldown")]
        public long SlaveryCooldown { get; set; }
        [FirestoreProperty("supportCooldown")]
        public long SupportCooldown { get; set; }
        [FirestoreProperty("timeTillCash")]
        public long TimeTillCash { get; set; }
        [FirestoreProperty("usingSlots")]
        public bool UsingSlots { get; set; }
        [FirestoreProperty("whoreCooldown")]
        public long WhoreCooldown { get; set; }
        [FirestoreProperty("xrp")]
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

        public static async Task<DbUser> GetById(ulong guildId, ulong userId)
        {
            DocumentReference doc = Program.database.Collection($"servers/{guildId}/users").Document(userId.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (!snap.Exists)
            {
                await doc.CreateAsync(new { cash = 100.0 });
                return await GetById(guildId, userId);
            }

            return snap.ConvertTo<DbUser>();
        }

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

        public async Task SetCash(SocketUser user, double amount)
        {
            if (user.IsBot)
                return;
            if (amount < 0)
                amount = 0;

            IGuildUser guildUser = user as IGuildUser;
            amount = Math.Round(amount, 2) * Constants.CASH_MULTIPLIER;
            Cash = amount;

            DocumentReference ranksDoc = Program.database.Collection($"servers/{guildUser.GuildId}/config").Document("ranks");
            DocumentSnapshot snap = await ranksDoc.GetSnapshotAsync();
            if (snap.Exists)
            {
                foreach (KeyValuePair<string, object> kvp in snap.ToDictionary().Where(kvp => kvp.Key.EndsWith("Id")))
                {
                    double neededCash = snap.GetValue<double>(kvp.Key.Replace("Id", "Cost"));
                    ulong roleId = Convert.ToUInt64(kvp.Value);
                    IRole role = guildUser.Guild.GetRole(roleId);

                    if (amount >= neededCash && !guildUser.RoleIds.Contains(roleId))
                        await guildUser.AddRoleAsync(roleId);
                    else if (amount <= neededCash && guildUser.RoleIds.Contains(roleId))
                        await guildUser.RemoveRoleAsync(roleId);
                }
            }
        }

        public async Task UnlockAchievement(string name, string desc, SocketUser user, ISocketMessageChannel channel, double reward = 0)
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

        public async Task Write() => await Reference.SetAsync(this);
    }
}
