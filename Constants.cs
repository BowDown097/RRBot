namespace RRBot;
// Note: Win odds will always be out of 100, so if you want an 80% win chance, set the respective constant to 80.
// Cooldowns are in seconds, so if you want a 5 minute cooldown for something, set the respective constant to 300.
public static class Constants
{
    // ********************
    //     ACHIEVEMENTS
    // ********************
    public static readonly Achievement[] DefaultAchievements =
    {
        new("Hard Boiled Egg", "Find a hard boiled egg.", 1337),
        new("I Just Feel Bad", "Take a million dollar L."),
        new("Jackpot!", "Get a jackpot with $slots."),
        new("Luckiest Dude Alive", "Win $69.69."),
        new("Literally 1984", "Get muted."),
        new("Maxed!", "Reach the max prestige.", 1420),
        new("Prestiged!", "Get your first prestige.", 1000),
        new("Pretty Damn Lucky", "Win $99+.")
    };
    public static readonly string[] GamblingAchievements =
    {
        "I Just Feel Bad",
        "Jackpot!",
        "Luckiest Dude Alive",
        "Pretty Damn Lucky"
    };
    // ********************
    //     AUDIO SYSTEM
    // ********************
    public const float MaxVolume = 200;
    public const float MinVolume = 5;
    public const double PollIntervalSecs = 30;
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
    public const double CashMultiplier = 1.0;
    public const double MessageCash = 30;
    public const long MessageCashCooldown = 60;
    public const double TransactionMin = 100;
    // ********************
    //        CRIME
    // ********************
    public const long BullyCooldown = 900;
    public const long DealCooldown = 3600;
    public const double GenericCrimeWinOdds = 80;
    public const double GenericCrimeLossMax = 461;
    public const double GenericCrimeLossMin = 69;
    public const double GenericCrimeToolOdds = 4;
    public const double GenericCrimeWinMax = 691;
    public const double GenericCrimeWinMin = 69;
    public const long HackCooldown = 7200;
    public const double HackOdds = 10;
    public const long LootCooldown = 3600;
    public const long RapeCooldown = 7200;
    public const double RapeOdds = 50;
    public const double RapeMaxPercent = 9;
    public const double RapeMinPercent = 5;
    public const long RobCooldown = 7200;
    public const double RobOdds = 40;
    public const double RobMaxPercent = 20;
    public const double RobMinCash = 100;
    public const long ScavengeCooldown = 60;
    public const double ScavengeMinCash = 50;
    public const double ScavengeMaxCash = 100;
    public const double ScavengeTimeout = 15;
    public const long SlaveryCooldown = 3600;
    public const long WhoreCooldown = 3600;
    // ********************
    //        ECONOMY
    // ********************
    public const long DailyCooldown = 86400;
    // ********************
    //         FUN
    // ********************
    public static readonly string[] MagicConchImages =
    {
        "https://i.imgur.com/kJmUvt2.png", // i don't think so
        "https://i.imgur.com/h1GvIe6.png", // maybe someday
        "https://i.imgur.com/ozPmMUQ.png", // no
        "https://i.imgur.com/uRynAAx.png", // try asking again
        "https://i.imgur.com/qVjaDSs.png", // yes
    };
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
    public const double DoubleOdds = 45;
    public const double PotFee = 3;
    public const double SlotsMultThreeinarow = 4;
    public const double SlotsMultThreesevens = 21;
    public const double SlotsMultTwoinarow = 1.75;
    // ********************
    //       GANGS
    // ********************
    public const double GangCreationCost = 5000;
    public const int GangMaxMembers = 10;
    public const double GangVaultCost = 10000;
    public static readonly string[] GangPositions = { "Leader", "Elder", "Member" };
    public const int MaxGangsPerGuild = 50;
    public const double VaultTaxPercent = 1.5;
    // ********************
    //       GOODS
    // ********************
    public const long BlackHatDuration = 3600;
    public const long CocaineDuration = 3600;
    public const long ViagraDuration = 3600;
    public const long RomanianFlagDuration = 3600;
    // ********************
    //     INVESTMENTS
    // ********************
    public const double InvestmentFeePercent = 1.5;
    public const double InvestmentMinAmount = 0.01;
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
    public const long PrestigeCooldown = 86400;
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
    public const long ChopCooldown = 3600;
    public const long DigCooldown = 3600;
    public const long FarmCooldown = 3600;
    public static readonly Dictionary<string, double> Fish = new()
    {
        { "carp", 24 },
        { "trout", 27 },
        { "goldfish", 30 }
    };
    public const long FishCooldown = 3600;
    public const double FishCoconutOdds = 20;
    public const int GenericTaskWoodMax = 65;
    public const int GenericTaskWoodMin = 32;
    public const int GenericTaskStoneMax = 113;
    public const int GenericTaskStoneMin = 65;
    public const int GenericTaskIronMax = 161;
    public const int GenericTaskIronMin = 113;
    public const int GenericTaskDiamondMax = 209;
    public const int GenericTaskDiamondMin = 161;
    public const long HuntCooldown = 3600;
    public const long MineCooldown = 3600;
    public const double MineStoneMultiplier = 1.33;
    public const double MineIronMultiplier = 1.66;
    public const double MineDiamondMultiplier = 2.00;
}