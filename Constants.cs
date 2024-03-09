namespace RRBot;
// Note: Win odds will always be out of 100, so if you want an 80% win chance, set the respective constant to 80.
// Cooldowns are in seconds, so if you want a 5 minute cooldown for something, set the respective constant to 300.
public static class Constants
{
    // ********************
    //     ACHIEVEMENTS
    // ********************
    public static readonly Achievement[] DefaultAchievements =
    [
        new Achievement("Hard Boiled Egg", "Find a hard boiled egg.", 1337),
        new Achievement("I Just Feel Bad", "Take a million dollar L."),
        new Achievement("Jackpot!", "Get a jackpot with $slots."),
        new Achievement("Luckiest Dude Alive", "Win $69.69."),
        new Achievement("Literally 1984", "Get muted."),
        new Achievement("Maxed!", "Reach the max prestige.", 1420),
        new Achievement("OH BABY A TRIPLE", "Roll triples in $dice."),
        new Achievement("Prestiged!", "Get your first prestige.", 1000),
        new Achievement("Pretty Damn Lucky", "Win $99+.")
    ];
    public static readonly string[] GamblingAchievements =
    [
        "I Just Feel Bad",
        "Jackpot!",
        "Luckiest Dude Alive",
        "OH BABY A TRIPLE",
        "Pretty Damn Lucky"
    ];
    // *************************************************
    //                   AUDIO SYSTEM
    // (string fields required because of C# limitation)
    // *************************************************
    public const double InactivityTimeoutSecs = 10;
    public const float MaxPitch = 500;
    public const string MaxPitchString = "500";
    public const float MaxSpeed = 500;
    public const string MaxSpeedString = "500";
    public const float MaxTempo = 500;
    public const string MaxTempoString = "500";
    public const float MaxVolume = 200;
    public const string MaxVolumeString = "200";
    public const float MinPitch = 5;
    public const string MinPitchString = "5";
    public const float MinSpeed = 5;
    public const string MinSpeedString = "5";
    public const float MinTempo = 5;
    public const string MinTempoString = "5";
    public const float MinVolume = 5;
    public const string MinVolumeString = "5";
    // ********************
    //         BOT
    // ********************
    public const string Activity = "your mom shower";
    public const ActivityType ActivityType = Discord.ActivityType.Watching;
    public static readonly AllowedMentions Mentions = new(AllowedMentionTypes.Users);
    public const string Prefix = "$";
    // ********************
    //     CASH SYSTEM
    // ********************
    public const decimal CashMultiplier = 1.0m;
    public const decimal MessageCash = 30;
    public const decimal TransactionMin = 100;
    // ********************
    //      COOLDOWNS
    // ********************
    public const long BullyCooldown = 600;
    public const long ChopCooldown = 3600;
    public const long DailyCooldown = 86400;
    public const long DealCooldown = 3600;
    public const long DigCooldown = 3600;
    public const long FarmCooldown = 3600;
    public const long FishCooldown = 3600;
    public const long HackCooldown = 3600;
    public const long HuntCooldown = 3600;
    public const long LootCooldown = 3600;
    public const long MessageCashCooldown = 60;
    public const long MineCooldown = 3600;
    public const long PrestigeCooldown = 43200;
    public const long RapeCooldown = 3600;
    public const long RobCooldown = 3600;
    public const long ScavengeCooldown = 60;
    public const long ShootCooldown = 14400;
    public const long SlaveryCooldown = 3600;
    public const long WhoreCooldown = 3600;
    // ********************
    //        CRIME
    // ********************
    public const double GenericCrimeWinOdds = 80;
    public const decimal GenericCrimeLossMax = 461;
    public const decimal GenericCrimeLossMin = 69;
    public const double GenericCrimeToolOdds = 4;
    public const decimal GenericCrimeWinMax = 691;
    public const decimal GenericCrimeWinMin = 69;
    public const double HackOdds = 10;
    public const double RapeOdds = 50;
    public const decimal RapeMaxPercent = 9;
    public const decimal RapeMinPercent = 5;
    public const double RobOdds = 40;
    public const decimal RobMaxPercent = 20;
    public const decimal RobMinCash = 100;
    public const decimal ScavengeMinCash = 50;
    public const decimal ScavengeMaxCash = 100;
    public const double ScavengeTimeout = 15;
    // ********************
    //         FUN
    // ********************
    public static readonly string[] MagicConchImages =
    [
        "https://i.imgur.com/kJmUvt2.png", // i don't think so
        "https://i.imgur.com/h1GvIe6.png", // maybe someday
        "https://i.imgur.com/ozPmMUQ.png", // no
        "https://i.imgur.com/uRynAAx.png", // try asking again
        "https://i.imgur.com/qVjaDSs.png" // yes
    ];
    public static readonly Dictionary<string, string> Waifus = new()
    {
        { "Adolf Dripler", "https://i.redd.it/cd9v84v46ma21.jpg" },
        { "Aqua", "https://thicc.mywaifulist.moe/waifus/554/bd320a06a7b1b3b7f44e980a4c8e1ac8a975e575465915f1f13f60efe1108c3f_thumb.jpeg" },
        { "Astolfo", "https://i.pinimg.com/originals/47/0d/3d/470d3d86bfd0502f374b1ae7e4ea73b6.jpg" },
        { "Asuna", "https://i.redd.it/oj81n8bpy4e41.jpg" },
        { "Augustus Caesar, first Roman Emperor", "https://cdn.discordapp.com/attachments/1034079617239224331/1041892312130801784/augustus.jpg" },
        { "Baldi", "https://cdn.shopify.com/s/files/1/0076/4769/0825/products/bb-render-minifigure-baldi-solo-front_1024x1024.png?v=1565975377" },
        { "Barack Obama", "https://upload.wikimedia.org/wikipedia/commons/thumb/8/8d/President_Barack_Obama.jpg/1200px-President_Barack_Obama.jpg" },
        { "carlos", "https://cdn.discordapp.com/attachments/804898294873456701/817271464067072010/unknown.png" },
        { "DaBaby", "https://s3.amazonaws.com/media.thecrimson.com/photos/2021/03/02/205432_1348650.png" },
        { "Drake", "https://cdn.discordapp.com/attachments/804898294873456701/817272071871922226/ee3e7e8c7c26dbef49b8095c1ca90db2.png" },
        { "eduardo", "https://i.imgur.com/1bwSckX.png" },
        { "Emilia", "https://kawaii-mobile.com/wp-content/uploads/2016/10/Re-Zero-Emilia.iPhone-6-Plus-wallpaper-1080x1920.jpg" },
        { "Felix", "https://cdn.discordapp.com/attachments/804898294873456701/817269666845294622/739fa73c-be4f-40c3-a057-50395eb46539.png" },
        { "French", "https://live.staticflickr.com/110/297887549_2dc0ee273f_c.jpg" },
        { "George Lincoln Rockwell", "https://i.ytimg.com/vi/hRlvjkQFQvg/hqdefault.jpg" },
        { "Goku", "https://i1.sndcdn.com/artworks-000558462795-v3asuu-t500x500.jpg" },
        { "Gypsycrusader", "https://i.kym-cdn.com/entries/icons/facebook/000/035/821/cover3.jpg" },
        { "Herbert", "https://upload.wikimedia.org/wikipedia/en/thumb/6/67/Herbert_-_Family_Guy.png/250px-Herbert_-_Family_Guy.png" },
        { "Holo", "https://thicc.mywaifulist.moe/waifus/91/d89a6fa083b95e76b9aa8e3be7a5d5d8dc6ddcb87737d428ffc1b537a0146965_thumb.jpeg" },
        { "juan.", "https://cdn.discordapp.com/attachments/804898294873456701/817275147060772874/unknown.png" },
        { "Keffals", "https://cdn.discordapp.com/attachments/1034079617239224331/1041893493473280000/keffals.png" },
        { "Kizuna Ai", "https://thicc.mywaifulist.moe/waifus/1608/105790f902e38da70c7ac59da446586c86eb19c7a9afc063b974d74b8870c4cc_thumb.png" },
        { "Linus", "https://i.ytimg.com/vi/hAsZCTL__lo/mqdefault.jpg" },
        { "Luke Smith", "https://i.ytimg.com/vi/UWpf4ZSAHBo/maxresdefault.jpg" },
        { "maria", "https://i.imgur.com/4Rj8HRs.png" },
        { "Mental Outlaw", "https://static.wikia.nocookie.net/youtube/images/7/7e/Mental.jpg/revision/latest?cb=20220318072553" },
        { "Midnight", "https://cdn.discordapp.com/attachments/804898294873456701/817268857374375986/653c4c631795ba90acefabb745ba3aa4.png" },
        { "Nagisa", "https://cdn.discordapp.com/attachments/804898294873456701/817270514401280010/3f244bab8ef7beafa5167ef0f7cdfe46.png" },
        { "Oswald Mosley", "https://cdn.britannica.com/16/133916-050-01D4245B/Oswald-Mosley-rally-London.jpg" },
        { "pablo", "https://cdn.discordapp.com/attachments/804898294873456701/817271690391715850/unknown.png" },
        { "Peter Griffin (in 2015)", "https://i.kym-cdn.com/photos/images/original/001/868/400/45d.jpg" },
        { "Pizza Heist Witness from Spiderman 2", "https://cdn.discordapp.com/attachments/804898294873456701/817272392002961438/unknown.png" },
        { "Quagmire", "https://s3.amazonaws.com/rapgenius/1361855949_glenn_quagmire_by_gan187-d3r70hu.png" },
        { "Rem", "https://cdn.discordapp.com/attachments/804898294873456701/817269005526106122/latest.png" },
        { "Rikka", "https://cdn.discordapp.com/attachments/804898294873456701/817269185176141824/db6e77106a10787b339da6e0b590410c.png" },
        { "Rin", "https://thicc.mywaifulist.moe/waifus/106/94da5e87c3dcc9eb3db018b815d067bed46f63f16a7e12357cafa1b530ce1c1a_thumb.jpeg" },
        { "Senjougahara", "https://thicc.mywaifulist.moe/waifus/262/1289a42d80717ce4fb0767ddc6c2a19cae5b897d4efe8260401aaacdba166f6e_thumb.jpeg" },
        { "Shinobu", "https://thicc.mywaifulist.moe/waifus/255/3906aba5167583d163ff90d46f86777242e6ff25550ed8ac915ef04f65a8d041_thumb.jpeg" },
        { "Squidward", "https://upload.wikimedia.org/wikipedia/en/thumb/8/8f/Squidward_Tentacles.svg/1200px-Squidward_Tentacles.svg.png" },
        { "Superjombombo", "https://pbs.twimg.com/profile_images/735305572405366786/LF5j-XcT_400x400.jpg" },
        { "Terry Davis", "https://upload.wikimedia.org/wikipedia/commons/3/34/Terry_A._Davis_2017.jpg" },
        { "Warren G. Harding, 29th President of the United States", "https://assets.atlasobscura.com/article_images/18223/image.jpg" },
        { "Your mom (ew bro that's weird)", "https://s.abcnews.com/images/Technology/whale-gty-jt-191219_hpMain_16x9_1600.jpg" },
        { "Zero Two", "https://cdn.discordapp.com/attachments/804898294873456701/817269546024042547/c4c54c906261b82f9401b60daf0e5be2.png" },
        { "Zimbabwe", "https://cdn.discordapp.com/attachments/802654650040844380/817273008821108736/unknown.png" }
    };
    // ********************
    //       GAMBLING
    // ********************
    public const string DiceEmojis = "⚀⚁⚂⚃⚄⚅";
    public const double DoubleOdds = 45;
    public const decimal PotFee = 3;
    public const decimal SlotsMultThreeinarow = 4;
    public const decimal SlotsMultThreesevens = 21;
    public const decimal SlotsMultTwoinarow = 1.75m;
    // ********************
    //       GANGS
    // ********************
    public const decimal GangCreationCost = 5000;
    public const int GangMaxMembers = 10;
    public const decimal GangRenameCost = 2000;
    public const decimal GangVaultCost = 5000;
    public static readonly string[] GangPositions = ["Leader", "Elder", "Member"];
    public const int MaxGangsPerGuild = 50;
    public const decimal VaultTaxPercent = 1.5m;
    // ********************
    //       GENERAL
    // ********************
    public static readonly DateTime StartTime = DateTime.Now;
    // ********************
    //       GOODS
    // ********************
    public const long BlackHatDuration = 3600;
    public const long CocaineDuration = 3600;
    public const long RomanianFlagDuration = 3600;
    public const long ViagraDuration = 3600;
    // ********************
    //     INVESTMENTS
    // ********************
    public const decimal InvestmentFeePercent = 1.5m;
    public const decimal InvestmentMinAmount = 0.01m;
    // ********************
    //      MODERATION
    // ********************
    public const int ChillMaxSeconds = 21600;
    public const int ChillMinSeconds = 10;
    // ********************
    //        POLLS
    // ********************
    public const long ElectionDuration = 259200;
    public static readonly Dictionary<int, string> PollEmotes = new()
    {
        { 0, "0️⃣" },
        { 1, "1️⃣" },
        { 2, "2️⃣" },
        { 3, "3️⃣" },
        { 4, "4️⃣" },
        { 5, "5️⃣" },
        { 6, "6️⃣" },
        { 7, "7️⃣" },
        { 8, "8️⃣" },
        { 9, "9️⃣" },
    };
    // ********************
    //      PRESTIGE
    // ********************
    public const int MaxPrestige = 10;
    public static readonly Dictionary<int, string> PrestigeImages = new()
    {
        { 1, "https://static.wikia.nocookie.net/callofduty/images/e/e8/Prestige_1_emblem_MW2.png/revision/latest/scale-to-width-down/64?cb=20121219030716" },
        { 2, "https://static.wikia.nocookie.net/callofduty/images/a/a4/Prestige_2_emblem_MW2.png/revision/latest/scale-to-width-down/64?cb=20121219031107" },
        { 3, "https://static.wikia.nocookie.net/callofduty/images/b/b3/Prestige_3_emblem_MW2.png/revision/latest/scale-to-width-down/64?cb=20121219031611" },
        { 4, "https://static.wikia.nocookie.net/callofduty/images/2/2f/Prestige_4_emblem_MW2.png/revision/latest/scale-to-width-down/64?cb=20121219032157" },
        { 5, "https://static.wikia.nocookie.net/callofduty/images/c/c5/Prestige_5_emblem_MW2.png/revision/latest/scale-to-width-down/64?cb=20121219054424" },
        { 6, "https://static.wikia.nocookie.net/callofduty/images/6/6c/Prestige_6_emblem_MW2.png/revision/latest/scale-to-width-down/64?cb=20121219060041" },
        { 7, "https://static.wikia.nocookie.net/callofduty/images/0/00/Prestige_7_emblem_MW2.png/revision/latest/scale-to-width-down/64?cb=20121219075022" },
        { 8, "https://static.wikia.nocookie.net/callofduty/images/d/d8/Prestige_8_emblem_MW2.png/revision/latest/scale-to-width-down/64?cb=20121219075347" },
        { 9, "https://static.wikia.nocookie.net/callofduty/images/8/83/Prestige_9_emblem_MW2.png/revision/latest/scale-to-width-down/64?cb=20121219075718" },
        { 10, "https://static.wikia.nocookie.net/callofduty/images/3/37/Prestige_10_emblem_MW2.png/revision/latest/scale-to-width-down/64?cb=20121219075719" }
    };
    // ********************
    //        TASKS
    // ********************
    public static readonly Dictionary<string, decimal> Fish = new()
    {
        { "carp", 24 },
        { "trout", 27 },
        { "goldfish", 30 }
    };
    public const double FishCoconutOdds = 20;
    public const int GenericTaskWoodMax = 65;
    public const int GenericTaskWoodMin = 32;
    public const int GenericTaskStoneMax = 113;
    public const int GenericTaskStoneMin = 65;
    public const int GenericTaskIronMax = 161;
    public const int GenericTaskIronMin = 113;
    public const int GenericTaskDiamondMax = 209;
    public const int GenericTaskDiamondMin = 161;
    public const int GenericTaskNetheriteMax = 257;
    public const int GenericTaskNetheriteMin = 209;
    // ********************
    //        ITEMS
    // ********************
    public static readonly Ammo[] Ammo =
    [
        new Ammo("Pistol Round", 3),
        new Ammo("Rifle Round", 2),
        new Ammo("Sniper Round", 1),
        new Ammo("Rocket", 0.5)
    ];
    public static readonly Crate[] Crates =
    [
        new Crate("Daily", 0, 0, 1, cash: 1500),
        new Crate("Bronze", 5000, 1, 2 ),
        new Crate("Silver", 10000, 2, 2, 1),
        new Crate("Gold", 15000, 3, 4, 2),
        new Crate("Diamond", 25000, 4, 6, 3)
    ];
    public static readonly Collectible[] Collectibles =
    [
        new Collectible("Ape NFT", "Who actually likes these? Why does this have value?", 1000, "https://i.ibb.co/w0syJ61/nft.png"),
        new Collectible("Bank Cheque", "Hey hey hey, we got ourselves some free money!", -1, "https://i.ibb.co/wCYcrP7/Blank-Cheque.png"),
        new Collectible("Coconut", "Well this is cool, I guess.", 3, "https://i.ibb.co/svxvLKP/coconut.png"),
        new Collectible("V Card", "Here you go, ya fuckin' virgin. Get a life bro", 69696969.69m, "https://i.ibb.co/rvKXgb5/vcard.png", false)
    ];
    public static readonly Consumable[] Consumables =
    [
        new Consumable("Black Hat", "Become an epic hax0r.", "You might get busted by the feds and get fined.", "$hack chance increased by 10%.", BlackHatDuration, 1),
        new Consumable("Cocaine", "Snorting a line of this funny sugar makes you HYPER and has some crazy effects.", "You have a chance of overdosing, which will make you lose all your remaining cocaine as well as not be able to use commands with cooldowns for a certain amount of time. The chance of overdosing and how long you can't use economy commands depends on how many lines you have in your system.", "Cooldowns are reduced by 10% for each line snorted.", CocaineDuration),
        new Consumable("Romanian Flag", "A neat little good luck charm for $rob. Your Romanian pride makes stealing wallets much easier!", "A Romanian might notice you and take some of your money.", "$rob chance increased by 10%.", RomanianFlagDuration, 1),
        new Consumable("Viagra", "Get it goin', if you know what I mean.", "The pill has a chance to backfire and give you ED.", "$rape chance increased by 10%.", ViagraDuration, 1)
    ];
    public static readonly Perk[] Perks =
    [
        new Perk("Enchanter", "Tasks are 20% more effective, but your tools have a 2% chance of breaking after use.", 5000, 172800),
        new Perk("Speed Demon", "Cooldowns are 15% shorter, but you have a 5% higher chance of failing any command that can fail.", 5000, 172800),
        new Perk("Multiperk", "Grants the ability to equip 2 perks, not including this one.", 10000, 604800),
        new Perk("Pacifist", "You are immune to all crimes, but you cannot use any crime commands and you also cannot appear on the leaderboard. Cannot be stacked with other perks, even if you have the Multiperk. Can be discarded, but cannot be used again for 3 days.", 0, -1)
    ];
    public static readonly Tool[] Tools =
    [
        new Tool("Wooden Pickaxe", 4500),
        new Tool("Stone Pickaxe", 6000, mult: 1.33m),
        new Tool("Iron Pickaxe", 7500, mult: 1.66m),
        new Tool("Diamond Pickaxe", 9000, mult: 2),
        new Tool("Netherite Pickaxe", 10500, mult: 2.33m),
        new Tool("Wooden Sword", 4500, GenericTaskWoodMin * 2.5m, GenericTaskWoodMax * 2.5m),
        new Tool("Stone Sword", 6000, GenericTaskStoneMin * 2.5m, GenericTaskStoneMax * 2.5m),
        new Tool("Iron Sword", 7500, GenericTaskIronMin * 2.5m, GenericTaskIronMax * 2.5m),
        new Tool("Diamond Sword", 9000, GenericTaskDiamondMin * 2.5m, GenericTaskDiamondMax * 2.5m),
        new Tool("Netherite Sword", 10500, GenericTaskNetheriteMin * 2.5m, GenericTaskNetheriteMax * 2.5m),
        new Tool("Wooden Shovel", 4500, GenericTaskWoodMin * 2.5m, GenericTaskWoodMax * 2.5m),
        new Tool("Stone Shovel", 6000, GenericTaskStoneMin * 2.5m, GenericTaskStoneMax * 2.5m),
        new Tool("Iron Shovel", 7500, GenericTaskIronMin * 2.5m, GenericTaskIronMax * 2.5m),
        new Tool("Diamond Shovel", 9000, GenericTaskDiamondMin * 2.5m, GenericTaskDiamondMax * 2.5m),
        new Tool("Netherite Shovel", 10500, GenericTaskNetheriteMin * 2.5m, GenericTaskNetheriteMax * 2.5m),
        new Tool("Wooden Axe", 4500, GenericTaskWoodMin * 2.5m, GenericTaskWoodMax * 2.5m),
        new Tool("Stone Axe", 6000, GenericTaskStoneMin * 2.5m, GenericTaskStoneMax * 2.5m),
        new Tool("Iron Axe", 7500, GenericTaskIronMin * 2.5m, GenericTaskIronMax * 2.5m),
        new Tool("Diamond Axe", 9000, GenericTaskDiamondMin * 2.5m, GenericTaskDiamondMax * 2.5m),
        new Tool("Netherite Axe", 10500, GenericTaskNetheriteMin * 2.5m, GenericTaskNetheriteMax * 2.5m),
        new Tool("Wooden Hoe", 4500, GenericTaskWoodMin * 2.5m, GenericTaskWoodMax * 2.5m),
        new Tool("Stone Hoe", 6000, GenericTaskStoneMin * 2.5m, GenericTaskStoneMax * 2.5m),
        new Tool("Iron Hoe", 7500, GenericTaskIronMin * 2.5m, GenericTaskIronMax * 2.5m),
        new Tool("Diamond Hoe", 9000, GenericTaskDiamondMin * 2.5m, GenericTaskDiamondMax * 2.5m),
        new Tool("Netherite Hoe", 10500, GenericTaskNetheriteMin * 2.5m, GenericTaskNetheriteMax * 2.5m),
        new Tool("Fishing Rod", 7500, Fish.First().Value * 7, Fish.Last().Value * 15)
    ];
    public static readonly Weapon[] Weapons =
    [
        new Weapon("Glock 17", 30, "Pistol Round", 13, 21, 40, "The classic. Also the weakest. Upgrade when?", new[] { "Bronze", "Silver", "Gold", "Diamond" }, "Gun"),
        new Weapon("Python", 35, "Pistol Round", 20, 25, 30, "Nice, small, and hits like a truck. Like a Glock but it doesn't suck.", new[] { "Silver", "Gold", "Diamond" }, "Gun"),
        new Weapon("AR-15", 50, "Rifle Round", 40, 45, 25, "\"Ummmm.. This is like a full on military weapon, we should ban it\" said some fucking loser no one cares about. This gun is awesome.", new[] { "Gold", "Diamond" }, "Gun"),
        new Weapon("M16", 60, "Rifle Round", 35, 40, 20, "TA-TA-TA! Three round burst. Nice n' accurate. Absolute beauty.", new[] { "Gold", "Diamond" }, "Gun"),
        new Weapon("Intervention", 70, "Sniper Round", 60, 80, 3, "Big dick energy in a weapon. Sexy. Accurate. Hard-hitting. The bros love it, the hoes love it. I love it.", new[] { "Gold", "Diamond" }, "Gun"),
        new Weapon("Barrett M82", 60, "Sniper Round", 50, 70, 7, "Yawn. Gay. It's cool I guess, but why have this when you could have an Intervention?", new[] { "Gold", "Diamond" }, "Gun"),
        new Weapon("RPG", 30, "Rocket", 99, 100, 15, "A FUCKIN' ROCKET LAUNCHER!!!! GUN GO BOOM!", new[] { "Diamond" }, "Gun")
    ];
}