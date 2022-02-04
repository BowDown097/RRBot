namespace RRBot.Systems;
public static class FilterSystem
{
    public static readonly Dictionary<string, string[]> HOMOGLYPHS = new()
	{
        { "-", new string[]{"\U000006d4", "\U00002cba", "\U0000fe58", "\U000002d7", "\U00002212", "\U00002796", "\U00002011", "\U00002043", "\U00002012", "\U00002013", "\U00002010"} },
        { ".", new string[]{"\U00000701", "\U00000660", "\U00002024", "\U000006f0", "\U0000a60e", "\U0000a4f8", "\U00000702", "\U00010a50", "\U0000ff0e", "\U0001d16d"} },
        { "0", new string[]{"\U0001d476", "\U00000585", "\U0000004f", "\U0000fbaa", "\U0001d4aa", "\U000006be", "\U0001d70e", "\U000009e6", "\U00000d02", "\U0001d4de", "\U0000fee9", "\U0001d630", "\U000006c1", "\U0001ee24", "\U0001d45c", "\U00000a66", "\U0001d7bc", "\U00000c02", "\U000010ff", "\U0001d490", "\U0001d5c8", "\U00000d82", "\U0000ff4f", "\U0001d744", "\U00000d20", "\U0001d5fc", "\U0000fba6", "\U00000c66", "\U000102ab", "\U00001d11", "\U00000665", "\U0000fbab", "\U0001d6d0", "\U0001d7b8", "\U000118c8", "\U000104c2", "\U0001d546", "\U0000ff10", "\U0001d442", "\U0000039f", "\U00010292", "\U0001d79e", "\U0000feec", "\U0001d7ce", "\U0001d782", "\U0001d6d4", "\U000006f5", "\U0000fbad", "\U0000a4f3", "\U0000feeb", "\U0001ee64", "\U000118e0", "\U00010404", "\U00002d54", "\U0001d7ec", "\U0000feea", "\U00003007", "\U00001040", "\U0000fba7", "\U0001d77e", "\U0001d428", "\U00000ae6", "\U000118b5", "\U0001d698", "\U000104ea", "\U00000ed0", "\U000005e1", "\U0001d4f8", "\U00000647", "\U00000c82", "\U00000966", "\U00000d66", "\U0001d7e2", "\U000118d7", "\U0001d64a", "\U0000fbac", "\U0001d764", "\U0001042c", "\U0001d748", "\U00002134", "\U0001d67e", "\U00000b66", "\U0000041e", "\U0000ab3d", "\U0001ee84", "\U0001d6f0", "\U0001fbf0", "\U00000ce6", "\U000114d0", "\U0001d7d8", "\U000006d5", "\U0001d70a", "\U0001d40e", "\U00000b20", "\U00000e50", "\U0001d52c", "\U0001d594", "\U0001d616", "\U0001d5ae", "\U000003c3", "\U0000043e", "\U000012d0", "\U0001d57a", "\U0001d72a", "\U00001d0f", "\U0000006f", "\U000003bf", "\U00002c9e", "\U0001d560", "\U00000555", "\U0001d5e2", "\U00010516", "\U00000be6", "\U000007c0", "\U0001d6b6", "\U0001d664", "\U0000ff2f", "\U0001d512", "\U0000fba8", "\U0000fba9", "\U0001d7f6", "\U00002c9f", "\U0000101d"} },
        { "1", new string[]{"\U0001d55d", "\U00000049", "\U0001d574", "\U0001d43c", "\U00000196", "\U00002d4f", "\U0001ee00", "\U0000a4f2", "\U0000fe8d", "\U0000ff4c", "\U0001d661", "\U00002223", "\U0001d6b0", "\U00000406", "\U00002c92", "\U000005c0", "\U0001d7ed", "\U0001d6ea", "\U0000ff11", "\U0001d610", "\U000005df", "\U0000007c", "\U0001d5c5", "\U0001d695", "\U0000ffe8", "\U00000661", "\U0001d408", "\U0001d540", "\U000005d5", "\U0001d7e3", "\U0001d678", "\U00016f28", "\U0001d5f9", "\U0001d4c1", "\U0001d7f7", "\U0001d724", "\U0001d4f5", "\U0000217c", "\U0000006c", "\U0001d7cf", "\U0001d5a8", "\U0001d425", "\U000004c0", "\U00010309", "\U0001d5dc", "\U00010320", "\U0001d459", "\U0001e8c7", "\U000023fd", "\U00000399", "\U000001c0", "\U0001d529", "\U0001d470", "\U0001d62d", "\U000007ca", "\U0000ff29", "\U00002111", "\U00002160", "\U0000fe8e", "\U0001ee80", "\U00002113", "\U0001028a", "\U0001d75e", "\U00002110", "\U0001d798", "\U0001fbf1", "\U0001d4d8", "\U000006f1", "\U0001d48d", "\U0001d7d9", "\U0001d644", "\U00000627", "\U0001d591", "\U000016c1"} },
        { "2", new string[]{"\U0000a75a", "\U000014bf", "\U000003e8", "\U0001d7ee", "\U0000a6ef", "\U000001a7", "\U0001d7da", "\U0001d7e4", "\U0001fbf2", "\U0001d7d0", "\U0000ff12", "\U0000a644", "\U0001d7f8"} },
        { "3", new string[]{"\U0001d7e5", "\U0000a76a", "\U0001d7f9", "\U0000021c", "\U0001d206", "\U00002ccc", "\U00000417", "\U000004e0", "\U0001d7ef", "\U000001b7", "\U0000ff13", "\U0001fbf3", "\U0001d7db", "\U0001d7d1", "\U00016f3b", "\U000118ca", "\U0000a7ab"} },
        { "4", new string[]{"\U0001d7dc", "\U0001fbf4", "\U0001d7d2", "\U0001d7f0", "\U000118af", "\U0001d7e6", "\U0000ff14", "\U000013ce", "\U0001d7fa"} },
        { "5", new string[]{"\U0001d7f1", "\U000001bc", "\U000118bb", "\U0001fbf5", "\U0001d7d3", "\U0000ff15", "\U0001d7fb", "\U0001d7e7", "\U0001d7dd"} },
        { "6", new string[]{"\U0001d7f2", "\U0001d7e8", "\U0000ff16", "\U0001fbf6", "\U0001d7d4", "\U000118d5", "\U00002cd2", "\U00000431", "\U000013ee", "\U0001d7de", "\U0001d7fc"} },
        { "7", new string[]{"\U0001d7df", "\U000104d2", "\U0000ff17", "\U000118c6", "\U0001fbf7", "\U0001d7f3", "\U0001d7e9", "\U0001d212", "\U0001d7d5", "\U0001d7fd"} },
        { "8", new string[]{"\U0001d7d6", "\U0001d7fe", "\U00000b03", "\U0001e8cb", "\U00000222", "\U000009ea", "\U00000a6a", "\U0001d7f4", "\U00000223", "\U0000ff18", "\U0001031a", "\U0001d7e0", "\U0001fbf8", "\U0001d7ea"} },
        { "9", new string[]{"\U0001d7ff", "\U0001fbf9", "\U0000a76e", "\U000118ac", "\U00000a67", "\U0001d7d7", "\U000118d6", "\U0000ff19", "\U0001d7e1", "\U0001d7eb", "\U000009ed", "\U000118cc", "\U00000d6d", "\U00002cca", "\U00000b68", "\U0001d7f5"} },
        { "a", new string[]{"\U0000ff41", "\U00000251", "\U000003b1", "\U0001d41a", "\U0001d656", "\U0001d770", "\U0001d482", "\U0001d68a", "\U0000237a", "\U0001d7aa", "\U0001d4b6", "\U00000430", "\U0001d51e", "\U0001d5ee", "\U0001d622", "\U0001d552", "\U0001d5ba", "\U0001d44e", "\U0001d6fc", "\U0001d6c2", "\U0001d4ea", "\U0001d736", "\U0001d586"} },
        { "b", new string[]{"\U0001d483", "\U0001d41b", "\U0001d4b7", "\U0001d5bb", "\U000015af", "\U0001d587", "\U0001d623", "\U000013cf", "\U0001d4eb", "\U00000184", "\U0001d5ef", "\U0001d553", "\U0000042c", "\U0001d51f", "\U0001d44f", "\U0000ff42", "\U0001d68b", "\U0001d657", "\U00001472"} },
        { "c", new string[]{"\U0001d520", "\U0001d450", "\U0001d5f0", "\U0000217d", "\U0001d588", "\U00001d04", "\U0001043d", "\U0000abaf", "\U0001d4ec", "\U0001d624", "\U0001d41c", "\U0001d5bc", "\U0001d658", "\U00000441", "\U0001d554", "\U000003f2", "\U00002ca5", "\U0001d68c", "\U0001d484", "\U0001d4b8", "\U0000ff43"} },
        { "d", new string[]{"\U0001d5f1", "\U000013e7", "\U0001d41d", "\U0001d4b9", "\U00002146", "\U0000a4d2", "\U00000501", "\U0000ff44", "\U0001d589", "\U0001d521", "\U0001d68d", "\U0001d659", "\U0001d5bd", "\U0000146f", "\U0001d451", "\U0001d625", "\U0000217e", "\U0001d555", "\U0001d485", "\U0001d4ed"} },
        { "e", new string[]{"\U0000212f", "\U0001d522", "\U000004bd", "\U0000ff45", "\U0001d556", "\U00002147", "\U0001d65a", "\U0000212e", "\U0000ab32", "\U0001d486", "\U0001d5f2", "\U0001d452", "\U0001d5be", "\U0001d4ee", "\U0001d58a", "\U0001d626", "\U0001d68e", "\U00000435", "\U0001d41e"} },
        { "f", new string[]{"\U0001d65b", "\U0001d487", "\U0000017f", "\U0001d4bb", "\U0001d523", "\U00000584", "\U0001d7cb", "\U0001d5f3", "\U0000ff46", "\U0001d68f", "\U0001d58b", "\U0000ab35", "\U0001d4ef", "\U00001e9d", "\U0001d557", "\U0001d5bf", "\U0001d453", "\U0001d41f", "\U000003dd", "\U0001d627", "\U0000a799"} },
        { "g", new string[]{"\U0001d58c", "\U0001d420", "\U0000210a", "\U0001d5f4", "\U0001d558", "\U0001d65c", "\U00000261", "\U0001d524", "\U0001d690", "\U0000018d", "\U00000581", "\U0001d5c0", "\U0001d628", "\U0000ff47", "\U0001d488", "\U00001d83", "\U0001d4f0", "\U0001d454"} },
        { "h", new string[]{"\U0001d421", "\U0001d4bd", "\U0000ff48", "\U0001d58d", "\U0001d65d", "\U0001d691", "\U0001d559", "\U0001d5c1", "\U0001d629", "\U000013c2", "\U00000570", "\U0001d5f5", "\U0001d4f1", "\U0000210e", "\U0001d489", "\U0001d525", "\U000004bb"} },
        { "i", new string[]{"\U0001d62a", "\U00001fbe", "\U00002148", "\U0001d778", "\U0001d58e", "\U0001d422", "\U000004cf", "\U0000037a", "\U0000ff49", "\U0001d5c2", "\U0001d73e", "\U0000a647", "\U0001d5f6", "\U000013a5", "\U0001d65e", "\U000118c3", "\U00000269", "\U0001d4be", "\U0001d6a4", "\U00002373", "\U0001d526", "\U0001d456", "\U000003b9", "\U0001d4f2", "\U0001d6ca", "\U0001d7b2", "\U0001d48a", "\U0001d692", "\U0000026a", "\U0001d704", "\U000002db", "\U0000ab75", "\U00000456", "\U00002170", "\U00002139", "\U0001d55a", "\U00000131"} },
        { "j", new string[]{"\U0001d423", "\U0001d5c3", "\U0001d693", "\U00002149", "\U0001d55b", "\U000003f3", "\U0000ff4a", "\U0001d48b", "\U0001d457", "\U0001d4bf", "\U0001d65f", "\U00000458", "\U0001d527", "\U0001d62b", "\U0001d5f7", "\U0001d4f3", "\U0001d58f"} },
        { "k", new string[]{"\U0000ff4b", "\U0001d55c", "\U0001d458", "\U0001d424", "\U0001d660", "\U0001d694", "\U0001d590", "\U0001d5c4", "\U0001d5f8", "\U0001d528", "\U0001d62c", "\U0001d48c", "\U0001d4f4", "\U0001d4c0"} },
        { "l", new string[]{"\U0001d55d", "\U00000049", "\U0001d574", "\U00000031", "\U0001d43c", "\U00000196", "\U00002d4f", "\U0001ee00", "\U0000a4f2", "\U0000fe8d", "\U0000ff4c", "\U0001d661", "\U00002223", "\U0001d6b0", "\U00000406", "\U00002c92", "\U000005c0", "\U0001d7ed", "\U0001d6ea", "\U0000ff11", "\U0001d610", "\U000005df", "\U0000007c", "\U0001d5c5", "\U0001d695", "\U0000ffe8", "\U00000661", "\U0001d408", "\U0001d540", "\U000005d5", "\U0001d7e3", "\U0001d678", "\U00016f28", "\U0001d5f9", "\U0001d4c1", "\U0001d7f7", "\U0001d724", "\U0001d4f5", "\U0000217c", "\U0001d7cf", "\U0001d5a8", "\U0001d425", "\U000004c0", "\U00010309", "\U0001d5dc", "\U00010320", "\U0001d459", "\U0001e8c7", "\U000023fd", "\U00000399", "\U000001c0", "\U0001d529", "\U0001d470", "\U0001d62d", "\U000007ca", "\U0000ff29", "\U00002111", "\U00002160", "\U0000fe8e", "\U0001ee80", "\U00002113", "\U0001028a", "\U0001d75e", "\U00002110", "\U0001d798", "\U0001fbf1", "\U0001d4d8", "\U000006f1", "\U0001d48d", "\U0001d7d9", "\U0001d644", "\U00000627", "\U0001d591", "\U000016c1"} },
        { "m", new string[]{"\U0000ff4d"} },
        { "n", new string[]{"\U0001d52b", "\U0001d593", "\U0001d5c7", "\U0001d45b", "\U0000ff4e", "\U0001d5fb", "\U00000578", "\U0001d62f", "\U0001d4f7", "\U0001d663", "\U0001d48f", "\U0001d4c3", "\U0001d55f", "\U0001d697", "\U0001d427", "\U0000057c"} },
        { "o", new string[]{"\U0001d476", "\U00000585", "\U0000004f", "\U0000fbaa", "\U0001d4aa", "\U000006be", "\U0001d70e", "\U000009e6", "\U00000d02", "\U0001d4de", "\U0000fee9", "\U0001d630", "\U000006c1", "\U0001ee24", "\U0001d45c", "\U00000a66", "\U0001d7bc", "\U00000c02", "\U000010ff", "\U0001d490", "\U0001d5c8", "\U00000d82", "\U0000ff4f", "\U0001d744", "\U00000d20", "\U0001d5fc", "\U0000fba6", "\U00000c66", "\U000102ab", "\U00001d11", "\U00000665", "\U0000fbab", "\U0001d6d0", "\U0001d7b8", "\U000118c8", "\U00000030", "\U000104c2", "\U0001d546", "\U0000ff10", "\U0001d442", "\U0000039f", "\U00010292", "\U0001d79e", "\U0000feec", "\U0001d7ce", "\U0001d782", "\U0001d6d4", "\U000006f5", "\U0000fbad", "\U0000a4f3", "\U0000feeb", "\U0001ee64", "\U000118e0", "\U00010404", "\U00002d54", "\U0001d7ec", "\U0000feea", "\U00003007", "\U00001040", "\U0000fba7", "\U0001d77e", "\U0001d428", "\U00000ae6", "\U000118b5", "\U0001d698", "\U000104ea", "\U00000ed0", "\U000005e1", "\U0001d4f8", "\U00000647", "\U00000c82", "\U00000966", "\U00000d66", "\U0001d7e2", "\U000118d7", "\U0001d64a", "\U0000fbac", "\U0001d764", "\U0001042c", "\U0001d748", "\U00002134", "\U0001d67e", "\U00000b66", "\U0000041e", "\U0000ab3d", "\U0001ee84", "\U0001d6f0", "\U0001fbf0", "\U00000ce6", "\U000114d0", "\U0001d7d8", "\U000006d5", "\U0001d70a", "\U0001d40e", "\U00000b20", "\U00000e50", "\U0001d52c", "\U0001d594", "\U0001d616", "\U0001d5ae", "\U000003c3", "\U0000043e", "\U000012d0", "\U0001d57a", "\U0001d72a", "\U00001d0f", "\U000003bf", "\U00002c9e", "\U0001d560", "\U00000555", "\U0001d5e2", "\U00010516", "\U00000be6", "\U000007c0", "\U0001d6b6", "\U0001d664", "\U0000ff2f", "\U0001d512", "\U0000fba8", "\U0000fba9", "\U0001d7f6", "\U00002c9f", "\U0000101d"} },
        { "p", new string[]{"\U0001d45d", "\U0001d561", "\U0001d78e", "\U000003f1", "\U0001d7c8", "\U0001d70c", "\U0000ff50", "\U00002ca3", "\U0001d4c5", "\U0001d7ba", "\U0001d491", "\U0001d595", "\U0001d746", "\U0001d429", "\U0001d71a", "\U0001d665", "\U0001d754", "\U0001d780", "\U0001d52d", "\U0001d699", "\U000003c1", "\U00002374", "\U0001d5c9", "\U0001d6e0", "\U0001d5fd", "\U00000440", "\U0001d631", "\U0001d6d2", "\U0001d4f9"} },
        { "q", new string[]{"\U0000051b", "\U0000ff51", "\U0001d4fa", "\U0001d5ca", "\U0001d52e", "\U0001d562", "\U0001d45e", "\U0001d5fe", "\U0001d666", "\U0001d596", "\U0001d69a", "\U00000563", "\U0001d492", "\U0001d632", "\U0001d42a", "\U00000566", "\U0001d4c6"} },
        { "r", new string[]{"\U0001d597", "\U0001d4fb", "\U00002c85", "\U0001d5cb", "\U0001d45f", "\U0000ab47", "\U0001d69b", "\U0001d42b", "\U0001d667", "\U00000433", "\U0001d493", "\U0001d4c7", "\U0000ab48", "\U0001d5ff", "\U0000ff52", "\U0001d52f", "\U00001d26", "\U0001d563", "\U0000ab81", "\U0001d633"} },
        { "s", new string[]{"\U0001d69c", "\U0000a731", "\U0000abaa", "\U0001d600", "\U000001bd", "\U00000455", "\U0001d460", "\U000118c1", "\U0001d564", "\U0001d668", "\U0001d4fc", "\U0001d494", "\U0001d5cc", "\U0001d634", "\U0001d42c", "\U00010448", "\U0001d530", "\U0001d598", "\U0000ff53", "\U0001d4c8"} },
        { "t", new string[]{"\U0001d495", "\U0001d5cd", "\U0001d599", "\U0001d669", "\U0001d531", "\U0001d4fd", "\U0001d4c9", "\U0001d42d", "\U0001d601", "\U0001d461", "\U0001d69d", "\U0001d565", "\U0000ff54", "\U0001d635"} },
        { "u", new string[]{"\U0001d462", "\U000104f6", "\U0001d5ce", "\U0001d6d6", "\U00001d1c", "\U0000ff55", "\U0001d42e", "\U0001d59a", "\U0001d69e", "\U0001d710", "\U0001d602", "\U0001d636", "\U0001d496", "\U0001d532", "\U0001d66a", "\U000118d8", "\U000003c5", "\U0001d7be", "\U0001d4ca", "\U0001d4fe", "\U0001d566", "\U0000057d", "\U0000ab4e", "\U0000ab52", "\U0000a79f", "\U0001d784", "\U0000028b", "\U0001d74a"} },
        { "v", new string[]{"\U0001d66b", "\U0001d4ff", "\U00000475", "\U0001d7b6", "\U0000ff56", "\U0001d497", "\U0001d533", "\U0001d77c", "\U0001d603", "\U0001d69f", "\U0001d42f", "\U00001d20", "\U0001d4cb", "\U0001d59b", "\U000005d8", "\U000022c1", "\U0001d742", "\U0001d6ce", "\U00011706", "\U000003bd", "\U0001d708", "\U0001d463", "\U00002228", "\U0001d637", "\U0001d5cf", "\U0000aba9", "\U000118c0", "\U00002174", "\U0001d567"} },
        { "w", new string[]{"\U0001d604", "\U0000ff57", "\U0001d5d0", "\U0001d498", "\U0001d430", "\U0001170f", "\U0001d638", "\U0001d66c", "\U0001d59c", "\U0001d534", "\U0001d500", "\U0000ab83", "\U0001d464", "\U0000026f", "\U0001170a", "\U00000561", "\U0001d6a0", "\U0001d568", "\U00001d21", "\U0001d4cc", "\U00000461", "\U0001170e", "\U0000051d"} },
        { "x", new string[]{"\U0001d431", "\U0001d465", "\U00002a2f", "\U0001d535", "\U0001d5d1", "\U00000445", "\U0000157d", "\U0001d639", "\U0001d4cd", "\U0001d499", "\U00002179", "\U0000292c", "\U0001d605", "\U000000d7", "\U0000166e", "\U0001d6a1", "\U0000ff58", "\U00001541", "\U0001d569", "\U0000292b", "\U0001d59d", "\U0001d501", "\U0001d66d"} },
        { "y", new string[]{"\U0000ab5a", "\U00001eff", "\U00000443", "\U0000028f", "\U0001d606", "\U0000213d", "\U0001d772", "\U000004af", "\U000010e7", "\U0001d56a", "\U0001d4ce", "\U0001d6c4", "\U0001d63a", "\U0000ff59", "\U0001d66e", "\U0001d738", "\U00000263", "\U0001d7ac", "\U0001d502", "\U0001d466", "\U0001d6a2", "\U000003b3", "\U0001d536", "\U00001d8c", "\U0001d49a", "\U000118dc", "\U0001d432", "\U0001d59e", "\U0001d6fe", "\U0001d5d2"} },
        { "z", new string[]{"\U0001d49b", "\U0001d433", "\U0001d59f", "\U0001d63b", "\U0001d56b", "\U0001d607", "\U0001d537", "\U00001d22", "\U0001d4cf", "\U0000ab93", "\U0001d467", "\U0001d66f", "\U0001d6a3", "\U000118c4", "\U0001d503", "\U0001d5d3", "\U0000ff5a"} }
    };
    private static readonly Regex INVITE_REGEX = new(@"discord(?:app.com\/invite|.gg|.me|.io)(?:[\\]+)?\/([a-zA-Z0-9\-]+)");

    public static async Task<bool> ContainsFilteredWord(IGuild guild, string input, DbConfigOptionals opt = null)
    {
        string cleaned = new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLower();
        DbConfigOptionals optionals = opt ?? await DbConfigOptionals.GetById(guild.Id);
        foreach (string regexStr in optionals.FilterRegexes)
        {
            Regex regex = new(regexStr);
            if (regex.IsMatch(cleaned))
                return true;
        }

        return false;
    }

    public static async Task DoInviteCheckAsync(SocketUserMessage message, IGuild guild, DiscordSocketClient client)
    {
        if (string.IsNullOrWhiteSpace(message.Content))
            return;

        DbConfigOptionals optionals = await DbConfigOptionals.GetById(guild.Id);
        if (!optionals.InviteFilterEnabled || optionals.NoFilterChannels.Contains(message.Channel.Id))
            return;

        foreach (Match match in INVITE_REGEX.Matches(message.Content))
        {
            string inviteCode = match.Groups[1].Value;
            RestInviteMetadata invite = await client.GetInviteAsync(inviteCode);
            if (invite != null)
                await message.DeleteAsync();
        }
    }

    public static async Task DoFilteredWordCheckAsync(SocketUserMessage message, IGuild guild)
    {
        if (string.IsNullOrWhiteSpace(message.Content))
            return;

        DbConfigOptionals optionals = await DbConfigOptionals.GetById(guild.Id);
        if (optionals.NoFilterChannels.Contains(message.Channel.Id))
            return;
    }

    public static async Task DoScamCheckAsync(SocketUserMessage message, IGuild guild)
    {
        if (string.IsNullOrWhiteSpace(message.Content))
            return;

        DbConfigOptionals optionals = await DbConfigOptionals.GetById(guild.Id);
        if (!optionals.ScamFilterEnabled || optionals.NoFilterChannels.Contains(message.Channel.Id))
            return;

        string content = message.Content.ToLower();
        if ((content.Contains("skins") && content.Contains("imgur"))
            || (content.Contains("nitro") && content.Contains("free") && content.Contains("http"))
            || (content.Contains("nitro") && content.Contains("steam")))
        {
            await message.DeleteAsync();
            return;
        }

        foreach (Embed epicEmbed in message.Embeds.Where(e => !string.IsNullOrWhiteSpace(e.Title)))
        {
            if (Uri.TryCreate(epicEmbed.Url, UriKind.Absolute, out Uri uri) && !string.IsNullOrWhiteSpace(uri.Host))
            {
                string host = uri.Host.Replace("www.", "").ToLower();
                string title = epicEmbed.Title.ToLower();
                if ((title.Contains("Trade offer") && host != "steamcommunity.com")
                    || (title.Contains("Steam Community") && host != "steamcommunity.com")
                    || (title.Contains("You've been gifted") && host != "discord.gift")
                    || (title.Contains("nitro") && title.Contains("steam")))
                {
                    await message.DeleteAsync();
                }
            }
        }
    }
}
