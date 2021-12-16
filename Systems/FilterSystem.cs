namespace RRBot.Systems;
public static class FilterSystem
{
    public static readonly Dictionary<string, string[]> HOMOGLYPHS = new()
    {
        { "-", new string[]{"\x06d4", "\x2cba", "\xfe58", "\x02d7", "\x2212", "\x2796", "\x2011", "\x2043", "\x2012", "\x2013", "\x2010"} },
        { ".", new string[]{"\x0701", "\x0660", "\x2024", "\x06f0", "\xa60e", "\xa4f8", "\x0702", "\x10a50", "\xff0e", "\x1d16d"} },
        { "0", new string[]{"\x1d476", "\x0585", "\x004f", "\xfbaa", "\x1d4aa", "\x06be", "\x1d70e", "\x09e6", "\x0d02", "\x1d4de", "\xfee9", "\x1d630", "\x06c1", "\x1ee24", "\x1d45c", "\x0a66", "\x1d7bc", "\x0c02", "\x10ff", "\x1d490", "\x1d5c8", "\x0d82", "\xff4f", "\x1d744", "\x0d20", "\x1d5fc", "\xfba6", "\x0c66", "\x102ab", "\x1d11", "\x0665", "\xfbab", "\x1d6d0", "\x1d7b8", "\x118c8", "\x104c2", "\x1d546", "\xff10", "\x1d442", "\x039f", "\x10292", "\x1d79e", "\xfeec", "\x1d7ce", "\x1d782", "\x1d6d4", "\x06f5", "\xfbad", "\xa4f3", "\xfeeb", "\x1ee64", "\x118e0", "\x10404", "\x2d54", "\x1d7ec", "\xfeea", "\x3007", "\x1040", "\xfba7", "\x1d77e", "\x1d428", "\x0ae6", "\x118b5", "\x1d698", "\x104ea", "\x0ed0", "\x05e1", "\x1d4f8", "\x0647", "\x0c82", "\x0966", "\x0d66", "\x1d7e2", "\x118d7", "\x1d64a", "\xfbac", "\x1d764", "\x1042c", "\x1d748", "\x2134", "\x1d67e", "\x0b66", "\x041e", "\xab3d", "\x1ee84", "\x1d6f0", "\x1fbf0", "\x0ce6", "\x114d0", "\x1d7d8", "\x06d5", "\x1d70a", "\x1d40e", "\x0b20", "\x0e50", "\x1d52c", "\x1d594", "\x1d616", "\x1d5ae", "\x03c3", "\x043e", "\x12d0", "\x1d57a", "\x1d72a", "\x1d0f", "\x006f", "\x03bf", "\x2c9e", "\x1d560", "\x0555", "\x1d5e2", "\x10516", "\x0be6", "\x07c0", "\x1d6b6", "\x1d664", "\xff2f", "\x1d512", "\xfba8", "\xfba9", "\x1d7f6", "\x2c9f", "\x101d"} },
        { "1", new string[]{"\x1d55d", "\x0049", "\x1d574", "\x1d43c", "\x0196", "\x2d4f", "\x1ee00", "\xa4f2", "\xfe8d", "\xff4c", "\x1d661", "\x2223", "\x1d6b0", "\x0406", "\x2c92", "\x05c0", "\x1d7ed", "\x1d6ea", "\xff11", "\x1d610", "\x05df", "\x007c", "\x1d5c5", "\x1d695", "\xffe8", "\x0661", "\x1d408", "\x1d540", "\x05d5", "\x1d7e3", "\x1d678", "\x16f28", "\x1d5f9", "\x1d4c1", "\x1d7f7", "\x1d724", "\x1d4f5", "\x217c", "\x006c", "\x1d7cf", "\x1d5a8", "\x1d425", "\x04c0", "\x10309", "\x1d5dc", "\x10320", "\x1d459", "\x1e8c7", "\x23fd", "\x0399", "\x01c0", "\x1d529", "\x1d470", "\x1d62d", "\x07ca", "\xff29", "\x2111", "\x2160", "\xfe8e", "\x1ee80", "\x2113", "\x1028a", "\x1d75e", "\x2110", "\x1d798", "\x1fbf1", "\x1d4d8", "\x06f1", "\x1d48d", "\x1d7d9", "\x1d644", "\x0627", "\x1d591", "\x16c1"} },
        { "2", new string[]{"\xa75a", "\x14bf", "\x03e8", "\x1d7ee", "\xa6ef", "\x01a7", "\x1d7da", "\x1d7e4", "\x1fbf2", "\x1d7d0", "\xff12", "\xa644", "\x1d7f8"} },
        { "3", new string[]{"\x1d7e5", "\xa76a", "\x1d7f9", "\x021c", "\x1d206", "\x2ccc", "\x0417", "\x04e0", "\x1d7ef", "\x01b7", "\xff13", "\x1fbf3", "\x1d7db", "\x1d7d1", "\x16f3b", "\x118ca", "\xa7ab"} },
        { "4", new string[]{"\x1d7dc", "\x1fbf4", "\x1d7d2", "\x1d7f0", "\x118af", "\x1d7e6", "\xff14", "\x13ce", "\x1d7fa"} },
        { "5", new string[]{"\x1d7f1", "\x01bc", "\x118bb", "\x1fbf5", "\x1d7d3", "\xff15", "\x1d7fb", "\x1d7e7", "\x1d7dd"} },
        { "6", new string[]{"\x1d7f2", "\x1d7e8", "\xff16", "\x1fbf6", "\x1d7d4", "\x118d5", "\x2cd2", "\x0431", "\x13ee", "\x1d7de", "\x1d7fc"} },
        { "7", new string[]{"\x1d7df", "\x104d2", "\xff17", "\x118c6", "\x1fbf7", "\x1d7f3", "\x1d7e9", "\x1d212", "\x1d7d5", "\x1d7fd"} },
        { "8", new string[]{"\x1d7d6", "\x1d7fe", "\x0b03", "\x1e8cb", "\x0222", "\x09ea", "\x0a6a", "\x1d7f4", "\x0223", "\xff18", "\x1031a", "\x1d7e0", "\x1fbf8", "\x1d7ea"} },
        { "9", new string[]{"\x1d7ff", "\x1fbf9", "\xa76e", "\x118ac", "\x0a67", "\x1d7d7", "\x118d6", "\xff19", "\x1d7e1", "\x1d7eb", "\x09ed", "\x118cc", "\x0d6d", "\x2cca", "\x0b68", "\x1d7f5"} },
        { "A", new string[]{"\x1d6e2", "\x1d4d0", "\x1d608", "\xa4ee", "\x1d71c", "\x1d49c", "\x1d504", "\xab7a", "\x1d6a8", "\x1d5d4", "\x1d538", "\x1d63c", "\x1d56c", "\x1d670", "\x0391", "\x1d434", "\x16f40", "\x0410", "\x15c5", "\x1d00", "\x1d400", "\xff21", "\x1d756", "\x102a0", "\x13aa", "\x1d5a0", "\x1d468", "\x1d790"} },
        { "B", new string[]{"\x1d539", "\x1d4d1", "\x1d671", "\x0412", "\x1d5d5", "\xa7b4", "\x1d791", "\x1d56d", "\x1d757", "\x10282", "\x102a1", "\x0432", "\x1d6e3", "\x0392", "\x1d6a9", "\x13f4", "\x15f7", "\x16d2", "\x10301", "\x1d505", "\x1d469", "\x1d609", "\x1d71d", "\x1d401", "\x212c", "\x1d435", "\x13fc", "\x1d5a1", "\x1d63d", "\xa4d0", "\xff22", "\x0299"} },
        { "C", new string[]{"\x1d672", "\x1d5a2", "\x1d60a", "\x1d436", "\x118e9", "\x10415", "\x13df", "\x118f2", "\x212d", "\xff23", "\x03f9", "\x1d4d2", "\x2ca4", "\x1d63e", "\x0421", "\x1f74c", "\x216d", "\x1455", "\xa4da", "\x1d46a", "\x1d49e", "\x2282", "\x2102", "\x2e26", "\x10302", "\x1051c", "\x1d402", "\x1d56e", "\x1d5d6", "\x102a2"} },
        { "D", new string[]{"\x1d507", "\x1d63f", "\x1d49f", "\x1d673", "\xa4d3", "\x1d60b", "\x2145", "\x1d46b", "\x1d5d7", "\x1d53b", "\xab70", "\xff24", "\x1d5a3", "\x1d4d3", "\x1d05", "\x1d56f", "\x216e", "\x13a0", "\x15ea", "\x1d437", "\x15de", "\x1d403"} },
        { "E", new string[]{"\x1d46c", "\x1d6ac", "\x1d53c", "\x1d570", "\x1d5d8", "\x118a6", "\x1d404", "\x1d6e6", "\x1d508", "\x22ff", "\x1d674", "\x2130", "\x13ac", "\xa4f0", "\x1d794", "\x2d39", "\x118ae", "\x1d640", "\xff25", "\xab7c", "\x1d4d4", "\x1d438", "\x1d5a4", "\x0395", "\x1d60c", "\x1d720", "\x0415", "\x1d07", "\x1d75a", "\x10286"} },
        { "F", new string[]{"\x1d571", "\x2131", "\xa798", "\x1d405", "\xa4dd", "\x118c2", "\x1d5a5", "\x1d60d", "\x118a2", "\x15b4", "\x1d675", "\x1d5d9", "\x1d46d", "\x1d641", "\x10287", "\x10525", "\x1d509", "\xff26", "\x1d213", "\x1d7ca", "\x1d53d", "\x1d4d5", "\x1d439", "\x102a5", "\x03dc"} },
        { "G", new string[]{"\x1d4a2", "\x0262", "\x13c0", "\xa4d6", "\x1d43a", "\x1d53e", "\x1d5da", "\x050c", "\x1d676", "\x1d572", "\x1d60e", "\x1d4d6", "\x13f3", "\x1d642", "\x1d5a6", "\x1d46e", "\xab90", "\x050d", "\x1d50a", "\x1d406", "\x13fb", "\xff27"} },
        { "H", new string[]{"\x210d", "\x2c8e", "\xab8b", "\x1d46f", "\xff28", "\x041d", "\x1d677", "\x029c", "\x1d6e8", "\x1d43b", "\x1d4d7", "\x1d5db", "\x1d573", "\xa4e7", "\x1d722", "\x1d643", "\x043d", "\x1d5a7", "\x0397", "\x1d796", "\x157c", "\x1d407", "\x102cf", "\x210b", "\x210c", "\x13bb", "\x1d6ae", "\x1d60f", "\x1d75c"} },
        { "I", new string[]{"\x1d55d", "\x1d574", "\x0031", "\x1d43c", "\x0196", "\x2d4f", "\x1ee00", "\xa4f2", "\xfe8d", "\xff4c", "\x1d661", "\x2223", "\x1d6b0", "\x0406", "\x2c92", "\x05c0", "\x1d7ed", "\x1d6ea", "\xff11", "\x1d610", "\x05df", "\x007c", "\x1d5c5", "\x1d695", "\xffe8", "\x0661", "\x1d408", "\x1d540", "\x05d5", "\x1d7e3", "\x1d678", "\x16f28", "\x1d5f9", "\x1d4c1", "\x1d7f7", "\x1d724", "\x1d4f5", "\x217c", "\x006c", "\x1d7cf", "\x1d5a8", "\x1d425", "\x04c0", "\x10309", "\x1d5dc", "\x10320", "\x1d459", "\x1e8c7", "\x23fd", "\x0399", "\x01c0", "\x1d529", "\x1d470", "\x1d62d", "\x07ca", "\xff29", "\x2111", "\x2160", "\xfe8e", "\x1ee80", "\x2113", "\x1028a", "\x1d75e", "\x2110", "\x1d798", "\x1fbf1", "\x1d4d8", "\x06f1", "\x1d48d", "\x1d7d9", "\x1d644", "\x0627", "\x1d591", "\x16c1"} },
        { "J", new string[]{"\x0408", "\xa7b2", "\x1d645", "\x1d50d", "\x1d5a9", "\x1d575", "\x1d5dd", "\xab7b", "\x1d409", "\x1d0a", "\x148d", "\xff2a", "\x1d611", "\x1d43d", "\x1d679", "\xa4d9", "\x1d4a5", "\x037f", "\x1d541", "\x1d471", "\x1d4d9", "\x13ab"} },
        { "K", new string[]{"\x16d5", "\x1d646", "\x1d4a6", "\x1d5aa", "\x1d43e", "\x039a", "\x1d542", "\xa4d7", "\x1d4da", "\x1d5de", "\x1d612", "\x1d6b1", "\x10518", "\x1d6eb", "\x1d576", "\x041a", "\x1d75f", "\xff2b", "\x13e6", "\x1d799", "\x1d50e", "\x1d67a", "\x1d472", "\x1d40a", "\x1d725", "\x2c94", "\x212a"} },
        { "L", new string[]{"\x2cd1", "\x1d647", "\x1d43f", "\x1d5ab", "\x1d5df", "\xabae", "\x1d613", "\xff2c", "\x1d473", "\x1d50f", "\x10526", "\x1d577", "\x1d67b", "\x10443", "\xa4e1", "\x16f16", "\x216c", "\x14aa", "\x2cd0", "\x118a3", "\x1d543", "\x029f", "\x1d40b", "\x118b2", "\x1d4db", "\x2112", "\x13de", "\x1d22a", "\x1041b"} },
        { "M", new string[]{"\x102b0", "\x1d4dc", "\x216f", "\x10311", "\x15f0", "\x1d5ac", "\x16d6", "\x1d614", "\x039c", "\x1d510", "\x1d761", "\x1d6b3", "\x1d727", "\x1d40c", "\x1d474", "\x1d67c", "\x1d5e0", "\x13b7", "\x1d440", "\x041c", "\x2133", "\xa4df", "\x1d578", "\xff2d", "\x1d79b", "\x03fa", "\x1d648", "\x1d6ed", "\x1d544", "\x2c98"} },
        { "N", new string[]{"\x1d441", "\x1d762", "\x2c9a", "\x1d5ad", "\x1d615", "\x1d40d", "\x0274", "\x1d6b4", "\x1d579", "\x1d4a9", "\x1d649", "\xa4e0", "\x1d728", "\x2115", "\x10513", "\x1d5e1", "\x1d4dd", "\x1d79c", "\x1d511", "\x1d6ee", "\xff2e", "\x1d475", "\x1d67d", "\x039d"} },
        { "O", new string[]{"\x1d476", "\x0585", "\xfbaa", "\x1d4aa", "\x06be", "\x1d70e", "\x09e6", "\x0d02", "\x1d4de", "\xfee9", "\x1d630", "\x06c1", "\x1ee24", "\x1d45c", "\x0a66", "\x1d7bc", "\x0c02", "\x10ff", "\x1d490", "\x1d5c8", "\x0d82", "\xff4f", "\x1d744", "\x0d20", "\x1d5fc", "\xfba6", "\x0c66", "\x102ab", "\x1d11", "\x0665", "\xfbab", "\x1d6d0", "\x1d7b8", "\x118c8", "\x0030", "\x104c2", "\x1d546", "\xff10", "\x1d442", "\x039f", "\x10292", "\x1d79e", "\xfeec", "\x1d7ce", "\x1d782", "\x1d6d4", "\x06f5", "\xfbad", "\xa4f3", "\xfeeb", "\x1ee64", "\x118e0", "\x10404", "\x2d54", "\x1d7ec", "\xfeea", "\x3007", "\x1040", "\xfba7", "\x1d77e", "\x1d428", "\x0ae6", "\x118b5", "\x1d698", "\x104ea", "\x0ed0", "\x05e1", "\x1d4f8", "\x0647", "\x0c82", "\x0966", "\x0d66", "\x1d7e2", "\x118d7", "\x1d64a", "\xfbac", "\x1d764", "\x1042c", "\x1d748", "\x2134", "\x1d67e", "\x0b66", "\x041e", "\xab3d", "\x1ee84", "\x1d6f0", "\x1fbf0", "\x0ce6", "\x114d0", "\x1d7d8", "\x06d5", "\x1d70a", "\x1d40e", "\x0b20", "\x0e50", "\x1d52c", "\x1d594", "\x1d616", "\x1d5ae", "\x03c3", "\x043e", "\x12d0", "\x1d57a", "\x1d72a", "\x1d0f", "\x006f", "\x03bf", "\x2c9e", "\x1d560", "\x0555", "\x1d5e2", "\x10516", "\x0be6", "\x07c0", "\x1d6b6", "\x1d664", "\xff2f", "\x1d512", "\xfba8", "\xfba9", "\x1d7f6", "\x2c9f", "\x101d"} },
        { "P", new string[]{"\xabb2", "\x1d5e3", "\x1d29", "\x1d4ab", "\xff30", "\x1d64b", "\x1d5af", "\x1d513", "\x0420", "\x2119", "\x1d67f", "\x1d4df", "\xa4d1", "\x1d6b8", "\x03a1", "\x1d57b", "\x1d766", "\x1d7a0", "\x10295", "\x1d18", "\x1d443", "\x146d", "\x1d40f", "\x2ca2", "\x1d6f2", "\x1d617", "\x13e2", "\x1d72c", "\x1d477"} },
        { "Q", new string[]{"\x1d4ac", "\x1d57c", "\x2d55", "\x1d478", "\x1d444", "\x1d410", "\x211a", "\x1d514", "\x1d64c", "\xff31", "\x1d618", "\x1d5b0", "\x1d680", "\x1d5e4", "\x1d4e0"} },
        { "R", new string[]{"\x1d479", "\x211c", "\xab71", "\x1d216", "\x1d5e5", "\x1587", "\x0280", "\x1d5b1", "\x1d411", "\x16b1", "\x01a6", "\x13a1", "\xff32", "\x211b", "\xaba2", "\x1d64d", "\x13d2", "\x104b4", "\x1d57d", "\x211d", "\x1d445", "\x1d681", "\x16f35", "\x1d4e1", "\xa4e3", "\x1d619"} },
        { "S", new string[]{"\x054f", "\xff33", "\x1d4e2", "\x1d57e", "\x1d5b2", "\x10296", "\x13da", "\x1d47a", "\x1d446", "\x1d4ae", "\x1d61a", "\x1d64e", "\x10420", "\x13d5", "\x1d5e6", "\xa4e2", "\x1d516", "\x1d412", "\x1d54a", "\x16f3a", "\x1d682", "\x0405"} },
        { "T", new string[]{"\x1d6d5", "\x1d683", "\x1d47b", "\x1d54b", "\x27d9", "\x2ca6", "\x16f0a", "\x1d1b", "\x1d413", "\x10297", "\xab72", "\x1d4e3", "\x1d7bd", "\x1d61b", "\x03c4", "\x1d6bb", "\x1d783", "\x22a4", "\x0422", "\x0442", "\x1f768", "\x1d5b3", "\x1d769", "\x1d6f5", "\x1d4af", "\x1d5e7", "\x1d64f", "\x03a4", "\x102b1", "\x1d517", "\xff34", "\x1d7a3", "\x1d749", "\x1d447", "\x1d70f", "\x13a2", "\xa4d4", "\x1d72f", "\x118bc", "\x10315", "\x1d57f"} },
        { "U", new string[]{"\x1d448", "\x1d414", "\x22c3", "\x222a", "\x1d5b4", "\x1d518", "\x1d580", "\x1d47c", "\x1d4b0", "\x1d650", "\x144c", "\x104ce", "\x1d4e4", "\x1d5e8", "\x1d61c", "\x1d684", "\x118b8", "\x16f42", "\xa4f4", "\x1d54c", "\x1200", "\xff35", "\x054d"} },
        { "V", new string[]{"\x1d415", "\x1d685", "\x1051d", "\x2164", "\x1d581", "\x13d9", "\x142f", "\x0474", "\xa6df", "\x2d38", "\x06f7", "\xff36", "\x1d20d", "\x1d54d", "\x1d449", "\x1d61d", "\x1d4b1", "\x1d47d", "\x1d5b5", "\x118a0", "\xa4e6", "\x1d519", "\x0667", "\x1d4e5", "\x1d5e9", "\x16f08", "\x1d651"} },
        { "W", new string[]{"\x1d686", "\x118e6", "\x051c", "\x1d652", "\x1d47e", "\x1d4b2", "\x1d416", "\x1d4e6", "\x1d5ea", "\x118ef", "\x1d51a", "\x13d4", "\x1d5b6", "\xff37", "\x1d54e", "\x1d44a", "\x1d582", "\x13b3", "\xa4ea", "\x1d61e"} },
        { "X", new string[]{"\x1d5b7", "\x2169", "\x1d7a6", "\x1d4b3", "\x10322", "\x2573", "\x1d61f", "\x03a7", "\x1d6be", "\x10290", "\x102b4", "\x1d54f", "\x10317", "\xa4eb", "\x2cac", "\x1d47f", "\x0425", "\xff38", "\x1d51b", "\x1d76c", "\x1d44b", "\x118ec", "\x166d", "\x1d417", "\x1d732", "\x2d5d", "\x10527", "\x1d583", "\x1d653", "\x1d5eb", "\x1d687", "\x1d6f8", "\xa7b3", "\x1d4e7", "\x16b7"} },
        { "Y", new string[]{"\x1d584", "\xa4ec", "\x03a5", "\x1d4b4", "\x1d688", "\x1d480", "\x102b2", "\x1d620", "\x1d550", "\x1d76a", "\x1d654", "\x13bd", "\x1d5ec", "\x2ca8", "\x1d6bc", "\x13a9", "\x1d7a4", "\x1d730", "\x1d418", "\x0423", "\x1d4e8", "\x118a4", "\xff39", "\x04ae", "\x16f43", "\x1d51c", "\x03d2", "\x1d44c", "\x1d6f6", "\x1d5b8"} },
        { "Z", new string[]{"\x1d44d", "\x102f5", "\x1d6e7", "\x1d689", "\xff3a", "\x2124", "\x0396", "\x1d655", "\x1d6ad", "\x1d621", "\x1d721", "\x1d75b", "\x1d481", "\x1d585", "\x1d419", "\xa4dc", "\x1d5b9", "\x1d4e9", "\x13c3", "\x1d795", "\x1d4b5", "\x1d5ed", "\x118a9", "\x118e5", "\x2128"} },
        { "a", new string[]{"\xff41", "\x0251", "\x03b1", "\x1d41a", "\x1d656", "\x1d770", "\x1d482", "\x1d68a", "\x237a", "\x1d7aa", "\x1d4b6", "\x0430", "\x1d51e", "\x1d5ee", "\x1d622", "\x1d552", "\x1d5ba", "\x1d44e", "\x1d6fc", "\x1d6c2", "\x1d4ea", "\x1d736", "\x1d586"} },
        { "b", new string[]{"\x1d483", "\x1d41b", "\x1d4b7", "\x1d5bb", "\x15af", "\x1d587", "\x1d623", "\x13cf", "\x1d4eb", "\x0184", "\x1d5ef", "\x1d553", "\x042c", "\x1d51f", "\x1d44f", "\xff42", "\x1d68b", "\x1d657", "\x1472"} },
        { "c", new string[]{"\x1d520", "\x1d450", "\x1d5f0", "\x217d", "\x1d588", "\x1d04", "\x1043d", "\xabaf", "\x1d4ec", "\x1d624", "\x1d41c", "\x1d5bc", "\x1d658", "\x0441", "\x1d554", "\x03f2", "\x2ca5", "\x1d68c", "\x1d484", "\x1d4b8", "\xff43"} },
        { "d", new string[]{"\x1d5f1", "\x13e7", "\x1d41d", "\x1d4b9", "\x2146", "\xa4d2", "\x0501", "\xff44", "\x1d589", "\x1d521", "\x1d68d", "\x1d659", "\x1d5bd", "\x146f", "\x1d451", "\x1d625", "\x217e", "\x1d555", "\x1d485", "\x1d4ed"} },
        { "e", new string[]{"\x212f", "\x1d522", "\x04bd", "\xff45", "\x1d556", "\x2147", "\x1d65a", "\x212e", "\xab32", "\x1d486", "\x1d5f2", "\x1d452", "\x1d5be", "\x1d4ee", "\x1d58a", "\x1d626", "\x1d68e", "\x0435", "\x1d41e"} },
        { "f", new string[]{"\x1d65b", "\x1d487", "\x017f", "\x1d4bb", "\x1d523", "\x0584", "\x1d7cb", "\x1d5f3", "\xff46", "\x1d68f", "\x1d58b", "\xab35", "\x1d4ef", "\x1e9d", "\x1d557", "\x1d5bf", "\x1d453", "\x1d41f", "\x03dd", "\x1d627", "\xa799"} },
        { "g", new string[]{"\x1d58c", "\x1d420", "\x210a", "\x1d5f4", "\x1d558", "\x1d65c", "\x0261", "\x1d524", "\x1d690", "\x018d", "\x0581", "\x1d5c0", "\x1d628", "\xff47", "\x1d488", "\x1d83", "\x1d4f0", "\x1d454"} },
        { "h", new string[]{"\x1d421", "\x1d4bd", "\xff48", "\x1d58d", "\x1d65d", "\x1d691", "\x1d559", "\x1d5c1", "\x1d629", "\x13c2", "\x0570", "\x1d5f5", "\x1d4f1", "\x210e", "\x1d489", "\x1d525", "\x04bb"} },
        { "i", new string[]{"\x1d62a", "\x1fbe", "\x2148", "\x1d778", "\x1d58e", "\x1d422", "\x04cf", "\x037a", "\xff49", "\x1d5c2", "\x1d73e", "\xa647", "\x1d5f6", "\x13a5", "\x1d65e", "\x118c3", "\x0269", "\x1d4be", "\x1d6a4", "\x2373", "\x1d526", "\x1d456", "\x03b9", "\x1d4f2", "\x1d6ca", "\x1d7b2", "\x1d48a", "\x1d692", "\x026a", "\x1d704", "\x02db", "\xab75", "\x0456", "\x2170", "\x2139", "\x1d55a", "\x0131"} },
        { "j", new string[]{"\x1d423", "\x1d5c3", "\x1d693", "\x2149", "\x1d55b", "\x03f3", "\xff4a", "\x1d48b", "\x1d457", "\x1d4bf", "\x1d65f", "\x0458", "\x1d527", "\x1d62b", "\x1d5f7", "\x1d4f3", "\x1d58f"} },
        { "k", new string[]{"\xff4b", "\x1d55c", "\x1d458", "\x1d424", "\x1d660", "\x1d694", "\x1d590", "\x1d5c4", "\x1d5f8", "\x1d528", "\x1d62c", "\x1d48c", "\x1d4f4", "\x1d4c0"} },
        { "l", new string[]{"\x1d55d", "\x0049", "\x1d574", "\x0031", "\x1d43c", "\x0196", "\x2d4f", "\x1ee00", "\xa4f2", "\xfe8d", "\xff4c", "\x1d661", "\x2223", "\x1d6b0", "\x0406", "\x2c92", "\x05c0", "\x1d7ed", "\x1d6ea", "\xff11", "\x1d610", "\x05df", "\x007c", "\x1d5c5", "\x1d695", "\xffe8", "\x0661", "\x1d408", "\x1d540", "\x05d5", "\x1d7e3", "\x1d678", "\x16f28", "\x1d5f9", "\x1d4c1", "\x1d7f7", "\x1d724", "\x1d4f5", "\x217c", "\x1d7cf", "\x1d5a8", "\x1d425", "\x04c0", "\x10309", "\x1d5dc", "\x10320", "\x1d459", "\x1e8c7", "\x23fd", "\x0399", "\x01c0", "\x1d529", "\x1d470", "\x1d62d", "\x07ca", "\xff29", "\x2111", "\x2160", "\xfe8e", "\x1ee80", "\x2113", "\x1028a", "\x1d75e", "\x2110", "\x1d798", "\x1fbf1", "\x1d4d8", "\x06f1", "\x1d48d", "\x1d7d9", "\x1d644", "\x0627", "\x1d591", "\x16c1"} },
        { "m", new string[]{"\xff4d"} },
        { "n", new string[]{"\x1d52b", "\x1d593", "\x1d5c7", "\x1d45b", "\xff4e", "\x1d5fb", "\x0578", "\x1d62f", "\x1d4f7", "\x1d663", "\x1d48f", "\x1d4c3", "\x1d55f", "\x1d697", "\x1d427", "\x057c"} },
        { "o", new string[]{"\x1d476", "\x0585", "\x004f", "\xfbaa", "\x1d4aa", "\x06be", "\x1d70e", "\x09e6", "\x0d02", "\x1d4de", "\xfee9", "\x1d630", "\x06c1", "\x1ee24", "\x1d45c", "\x0a66", "\x1d7bc", "\x0c02", "\x10ff", "\x1d490", "\x1d5c8", "\x0d82", "\xff4f", "\x1d744", "\x0d20", "\x1d5fc", "\xfba6", "\x0c66", "\x102ab", "\x1d11", "\x0665", "\xfbab", "\x1d6d0", "\x1d7b8", "\x118c8", "\x0030", "\x104c2", "\x1d546", "\xff10", "\x1d442", "\x039f", "\x10292", "\x1d79e", "\xfeec", "\x1d7ce", "\x1d782", "\x1d6d4", "\x06f5", "\xfbad", "\xa4f3", "\xfeeb", "\x1ee64", "\x118e0", "\x10404", "\x2d54", "\x1d7ec", "\xfeea", "\x3007", "\x1040", "\xfba7", "\x1d77e", "\x1d428", "\x0ae6", "\x118b5", "\x1d698", "\x104ea", "\x0ed0", "\x05e1", "\x1d4f8", "\x0647", "\x0c82", "\x0966", "\x0d66", "\x1d7e2", "\x118d7", "\x1d64a", "\xfbac", "\x1d764", "\x1042c", "\x1d748", "\x2134", "\x1d67e", "\x0b66", "\x041e", "\xab3d", "\x1ee84", "\x1d6f0", "\x1fbf0", "\x0ce6", "\x114d0", "\x1d7d8", "\x06d5", "\x1d70a", "\x1d40e", "\x0b20", "\x0e50", "\x1d52c", "\x1d594", "\x1d616", "\x1d5ae", "\x03c3", "\x043e", "\x12d0", "\x1d57a", "\x1d72a", "\x1d0f", "\x03bf", "\x2c9e", "\x1d560", "\x0555", "\x1d5e2", "\x10516", "\x0be6", "\x07c0", "\x1d6b6", "\x1d664", "\xff2f", "\x1d512", "\xfba8", "\xfba9", "\x1d7f6", "\x2c9f", "\x101d"} },
        { "p", new string[]{"\x1d45d", "\x1d561", "\x1d78e", "\x03f1", "\x1d7c8", "\x1d70c", "\xff50", "\x2ca3", "\x1d4c5", "\x1d7ba", "\x1d491", "\x1d595", "\x1d746", "\x1d429", "\x1d71a", "\x1d665", "\x1d754", "\x1d780", "\x1d52d", "\x1d699", "\x03c1", "\x2374", "\x1d5c9", "\x1d6e0", "\x1d5fd", "\x0440", "\x1d631", "\x1d6d2", "\x1d4f9"} },
        { "q", new string[]{"\x051b", "\xff51", "\x1d4fa", "\x1d5ca", "\x1d52e", "\x1d562", "\x1d45e", "\x1d5fe", "\x1d666", "\x1d596", "\x1d69a", "\x0563", "\x1d492", "\x1d632", "\x1d42a", "\x0566", "\x1d4c6"} },
        { "r", new string[]{"\x1d597", "\x1d4fb", "\x2c85", "\x1d5cb", "\x1d45f", "\xab47", "\x1d69b", "\x1d42b", "\x1d667", "\x0433", "\x1d493", "\x1d4c7", "\xab48", "\x1d5ff", "\xff52", "\x1d52f", "\x1d26", "\x1d563", "\xab81", "\x1d633"} },
        { "s", new string[]{"\x1d69c", "\xa731", "\xabaa", "\x1d600", "\x01bd", "\x0455", "\x1d460", "\x118c1", "\x1d564", "\x1d668", "\x1d4fc", "\x1d494", "\x1d5cc", "\x1d634", "\x1d42c", "\x10448", "\x1d530", "\x1d598", "\xff53", "\x1d4c8"} },
        { "t", new string[]{"\x1d495", "\x1d5cd", "\x1d599", "\x1d669", "\x1d531", "\x1d4fd", "\x1d4c9", "\x1d42d", "\x1d601", "\x1d461", "\x1d69d", "\x1d565", "\xff54", "\x1d635"} },
        { "u", new string[]{"\x1d462", "\x104f6", "\x1d5ce", "\x1d6d6", "\x1d1c", "\xff55", "\x1d42e", "\x1d59a", "\x1d69e", "\x1d710", "\x1d602", "\x1d636", "\x1d496", "\x1d532", "\x1d66a", "\x118d8", "\x03c5", "\x1d7be", "\x1d4ca", "\x1d4fe", "\x1d566", "\x057d", "\xab4e", "\xab52", "\xa79f", "\x1d784", "\x028b", "\x1d74a"} },
        { "v", new string[]{"\x1d66b", "\x1d4ff", "\x0475", "\x1d7b6", "\xff56", "\x1d497", "\x1d533", "\x1d77c", "\x1d603", "\x1d69f", "\x1d42f", "\x1d20", "\x1d4cb", "\x1d59b", "\x05d8", "\x22c1", "\x1d742", "\x1d6ce", "\x11706", "\x03bd", "\x1d708", "\x1d463", "\x2228", "\x1d637", "\x1d5cf", "\xaba9", "\x118c0", "\x2174", "\x1d567"} },
        { "w", new string[]{"\x1d604", "\xff57", "\x1d5d0", "\x1d498", "\x1d430", "\x1170f", "\x1d638", "\x1d66c", "\x1d59c", "\x1d534", "\x1d500", "\xab83", "\x1d464", "\x026f", "\x1170a", "\x0561", "\x1d6a0", "\x1d568", "\x1d21", "\x1d4cc", "\x0461", "\x1170e", "\x051d"} },
        { "x", new string[]{"\x1d431", "\x1d465", "\x2a2f", "\x1d535", "\x1d5d1", "\x0445", "\x157d", "\x1d639", "\x1d4cd", "\x1d499", "\x2179", "\x292c", "\x1d605", "\x00d7", "\x166e", "\x1d6a1", "\xff58", "\x1541", "\x1d569", "\x292b", "\x1d59d", "\x1d501", "\x1d66d"} },
        { "y", new string[]{"\xab5a", "\x1eff", "\x0443", "\x028f", "\x1d606", "\x213d", "\x1d772", "\x04af", "\x10e7", "\x1d56a", "\x1d4ce", "\x1d6c4", "\x1d63a", "\xff59", "\x1d66e", "\x1d738", "\x0263", "\x1d7ac", "\x1d502", "\x1d466", "\x1d6a2", "\x03b3", "\x1d536", "\x1d8c", "\x1d49a", "\x118dc", "\x1d432", "\x1d59e", "\x1d6fe", "\x1d5d2"} },
        { "z", new string[] {"\x1d49b", "\x1d433", "\x1d59f", "\x1d63b", "\x1d56b", "\x1d607", "\x1d537", "\x1d22", "\x1d4cf", "\xab93", "\x1d467", "\x1d66f", "\x1d6a3", "\x118c4", "\x1d503", "\x1d5d3", "\xff5a"} }
    };
    private static readonly Regex INVITE_REGEX = new(@"discord(?:app.com\/invite|.gg|.me|.io)(?:[\\]+)?\/([a-zA-Z0-9\-]+)");

    public static async Task<bool> ContainsFilteredWord(SocketGuild guild, string input)
    {
        string cleaned = new(input.Where(c => !char.IsWhiteSpace(c)).ToArray());
        DbConfigOptionals optionals = await DbConfigOptionals.GetById(guild.Id);
        foreach (string regexStr in optionals.FilterRegexes)
        {
            Regex regex = new(regexStr);
            if (regex.IsMatch(cleaned))
                return true;
        }

        return false;
    }

    public static async Task DoInviteCheckAsync(SocketUserMessage message, SocketGuild guild, DiscordSocketClient client)
    {
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

    public static async Task DoFilteredWordCheckAsync(SocketUserMessage message, SocketGuild guild, IMessageChannel channel)
    {
        if (!channel.Name.In("extremely-funny", "bot-commands-for-retards", "private-godfather") && await ContainsFilteredWord(guild, message.Content))
            await message.DeleteAsync();
    }

    public static async Task DoScamCheckAsync(SocketUserMessage message, SocketGuild guild)
    {
        DbConfigOptionals optionals = await DbConfigOptionals.GetById(guild.Id);
        if (!optionals.ScamFilterEnabled || optionals.NoFilterChannels.Contains(message.Channel.Id))
            return;

        string content = message.Content.ToLower();
        if ((content.Contains("skins") && content.Contains("imgur"))
            || (content.Contains("nitro") && content.Contains("free") && content.Contains("http")))
        {
            await message.DeleteAsync();
            return;
        }

        foreach (Embed epicEmbed in message.Embeds)
        {
            if (Uri.TryCreate(epicEmbed.Url, UriKind.Absolute, out Uri uri))
            {
                string host = uri.Host.Replace("www.", "").ToLower();
                if ((epicEmbed.Title?.StartsWith("Trade offer") == true && host != "steamcommunity.com")
                    || (epicEmbed.Title?.StartsWith("Steam Community") == true && host != "steamcommunity.com")
                    || (epicEmbed.Title?.StartsWith("You've been gifted") == true && host != "discord.gift"))
                {
                    await message.DeleteAsync();
                }
            }
        }
    }
}