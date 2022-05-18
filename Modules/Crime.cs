namespace RRBot.Modules;
[Summary("Hell yeah! Crime! Reject the ways of being a law-abiding citizen for some cold hard cash and maybe even a tool. Or, maybe not. Depends how good you are at being a criminal.")]
[CheckPacifist]
public class Crime : ModuleBase<SocketCommandContext>
{
    public InteractiveService Interactive { get; set; }

    [Command("bully")]
    [Summary("Change the nickname of any victim you wish!")]
    [Remarks("$bully \"John Boyer#2168\" gay lol")]
    [RequireCooldown("BullyCooldown", "You cannot bully anyone for {0}.")]
    public async Task<RuntimeResult> Bully(IGuildUser user, [Remainder] string nickname)
    {
        if (await FilterSystem.ContainsFilteredWord(Context.Guild, nickname))
            return CommandResult.FromError("You cannot bully someone to the funny word.");
        if (nickname.Length > 32)
            return CommandResult.FromError("The nickname you put is longer than the maximum accepted length (32).");
        if (user.Id == Context.User.Id)
            return CommandResult.FromError("No masochism here!");
        if (user.IsBot)
            return CommandResult.FromError("Nope.");

        DbUser target = await DbUser.GetById(Context.Guild.Id, user.Id);
        if (target.Perks.ContainsKey("Pacifist"))
            return CommandResult.FromError($"You cannot bully **{user.Sanitize()}** as they have the Pacifist perk equipped.");

        DbConfigRoles roles = await DbConfigRoles.GetById(Context.Guild.Id);
        if (user.RoleIds.Contains(roles.StaffLvl1Role) || user.RoleIds.Contains(roles.StaffLvl2Role))
            return CommandResult.FromError($"You cannot bully **{user.Sanitize()}** as they are a staff member.");

        await user.ModifyAsync(props => props.Nickname = nickname);
        await LoggingSystem.Custom_UserBullied(user, Context.User, nickname);
        await Context.User.NotifyAsync(Context.Channel, $"You BULLIED **{user.Sanitize()}** to ``{nickname}``!");

        DbUser author = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        await author.SetCooldown("BullyCooldown", Constants.BULLY_COOLDOWN, Context.Guild, Context.User);
        return CommandResult.FromSuccess();
    }

    [Command("deal")]
    [Summary("Deal some drugs.")]
    [RequireCooldown("DealCooldown", "You don't have any more drugs to deal! Your next shipment comes in {0}.")]
    public async Task<RuntimeResult> Deal()
    {
        string[] successes = { "Border patrol let your cocaine-stuffed dog through! You earned **{0}** from the cartel.",
            "You continue to capitalize off of some 17 year old's meth addiction, yielding you **{0}**.",
            "You sold grass to some elementary schoolers and passed it off as weed. They didn't have a lot of course, only **{0}**, but money's money." };
        string[] fails = { "You tripped balls on acid with the boys at a party. After waking up, you realize not only did someone take money from your piggy bank, but you also gave out too much free acid, leaving you a whopping **{0}** poorer.",
            "The Democrats have launched yet another crime bill, leading to your hood being under heavy investigation. You could not escape the feds and paid **{0}** in fines." };
        return await GenericCrime(successes, fails, "DealCooldown", Constants.DEAL_COOLDOWN, true);
    }

    [Command("hack")]
    [Summary("Hack into someone's crypto wallet.")]
    [Remarks("$hack LYNESTAR XRP 10000")]
    [RequireCooldown("HackCooldown", "You exhausted all your brain power bro, you're gonna have to wait {0}.")]
    public async Task<RuntimeResult> Hack(IGuildUser user, string crypto, double amount)
    {
        if (amount < Constants.INVESTMENT_MIN_AMOUNT || double.IsNaN(amount))
            return CommandResult.FromError($"You must hack {Constants.INVESTMENT_MIN_AMOUNT} or more.");
        if (user.Id == Context.User.Id)
            return CommandResult.FromError("How are you supposed to hack yourself?");
        if (user.IsBot)
            return CommandResult.FromError("Nope.");

        string abbreviation = Investments.ResolveAbbreviation(crypto);
        if (abbreviation is null)
            return CommandResult.FromError($"**{crypto}** is not a currently accepted currency!");

        DbUser author = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        DbUser target = await DbUser.GetById(Context.Guild.Id, user.Id);
        if (author.UsingSlots || target.UsingSlots)
            return CommandResult.FromError("One of you is using the slot machine. I cannot do any transactions at the moment.");
        if (target.Perks.ContainsKey("Pacifist"))
            return CommandResult.FromError($"You cannot hack **{user.Sanitize()}** as they have the Pacifist perk equipped.");

        double authorBal = (double)author[abbreviation];
        double targetBal = (double)target[abbreviation];
        double robMax = Math.Round(targetBal / 100.0 * Constants.ROB_MAX_PERCENT, 4);
        if (authorBal < amount)
            return CommandResult.FromError($"You don't have that much {abbreviation}!");
        if (amount > robMax)
            return CommandResult.FromError($"You can only hack {Constants.ROB_MAX_PERCENT}% of **{user.Sanitize()}**'s {abbreviation}, that being **{robMax}**.");

        int roll = RandomUtil.Next(1, 101);
        double cryptoValue = await Investments.QueryCryptoValue(abbreviation) * amount;
        if (roll < Constants.HACK_ODDS)
        {
            target[abbreviation] = targetBal - amount;
            author[abbreviation] = authorBal + amount;
            StatUpdate(author, true, cryptoValue);
            StatUpdate(target, false, cryptoValue);
            switch (RandomUtil.Next(2))
            {
                case 0:
                    await Context.User.NotifyAsync(Context.Channel, $"The dumbass pushed his private keys to GitHub LMFAO! You sniped that shit and got **{amount:0.####} {abbreviation}**.");
                    break;
                case 1:
                    await Context.User.NotifyAsync(Context.Channel, $"You did an ol' SIM swap on {user.Sanitize()}'s phone while they weren't looking and yoinked **{amount:0.####} {abbreviation}** right off their Coinbase. Easy claps!");
                    break;
            }
        }
        else
        {
            author[abbreviation] = authorBal - (amount / 4);
            StatUpdate(author, false, amount / 4);
            switch (RandomUtil.Next(2))
            {
                case 0:
                    await Context.User.NotifyAsync(Context.Channel, $"**{user.Sanitize()}** actually secured their shit properly, got your info, and sent it off to the feds. You got raided and lost **{amount / 4:0.####} {abbreviation}** in the process.");
                    break;
                case 1:
                    await Context.User.NotifyAsync(Context.Channel, $"That hacker dude on Instagram scammed your ass! You only had to pay 1/4 of what you were promising starting off, but still sucks. There goes **{amount / 4:0.####} {abbreviation}**.");
                    break;
            }
        }

        await author.SetCooldown("HackCooldown", Constants.HACK_COOLDOWN, Context.Guild, Context.User);
        return CommandResult.FromSuccess();
    }

    [Command("loot")]
    [Summary("Loot some locations.")]
    [RequireCooldown("LootCooldown", "You cannot loot for {0}.")]
    public async Task<RuntimeResult> Loot()
    {
        string[] successes = { "You joined your local BLM protest, looted a Footlocker, and sold what you got. You earned **{0}**.",
            "That mall had a lot of shit! You earned **{0}**.",
            "You stole from a gas station because you're a fucking idiot. You earned **{0}**, basically nothing." };
        string[] fails = { "There happened to be a cop coming out of the donut shop next door. You had to pay **{0}** in fines.",
            "The manager gave no fucks and beat the SHIT out of you. You lost **{0}** paying for face stitches." };
        return await GenericCrime(successes, fails, "LootCooldown", Constants.LOOT_COOLDOWN, true);
    }

    [Alias("strugglesnuggle")]
    [Command("rape")]
    [Summary("Go out on the prowl for some ass!")]
    [Remarks("$rape \"BowDown's Kitten\"")]
    [RequireCash]
    [RequireCooldown("RapeCooldown", "You cannot rape for {0}.")]
    public async Task<RuntimeResult> Rape(IGuildUser user)
    {
        if (user.Id == Context.User.Id)
            return CommandResult.FromError("How are you supposed to rape yourself?");
        if (user.IsBot)
            return CommandResult.FromError("Nope.");

        DbUser author = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        DbUser target = await DbUser.GetById(Context.Guild.Id, user.Id);
        if (author.UsingSlots || target.UsingSlots)
            return CommandResult.FromError("One of you is using the slot machine. I cannot do any transactions at the moment.");
        if (target.Perks.ContainsKey("Pacifist"))
            return CommandResult.FromError($"You cannot rape **{user.Sanitize()}** as they have the Pacifist perk equipped.");
        if (target.Cash < 0.01)
            return CommandResult.FromError($"Dear Lord, talk about kicking them while they're down! **{user.Sanitize()}** is broke! Have some decency.");

        double rapePercent = RandomUtil.NextDouble(Constants.RAPE_MIN_PERCENT, Constants.RAPE_MAX_PERCENT);
        double winOdds = author.Perks.ContainsKey("Speed Demon") ? Constants.RAPE_ODDS * 0.95 : Constants.RAPE_ODDS;
        if (RandomUtil.NextDouble(1, 101) < winOdds)
        {
            double repairs = target.Cash / 100.0 * rapePercent;
            StatUpdate(target, false, repairs);
            await target.SetCash(user, target.Cash - repairs);
            await Context.User.NotifyAsync(Context.Channel, $"You DEMOLISHED **{user.Sanitize()}**'s asshole! They just paid **{repairs:C2}** in asshole repairs.");
        }
        else
        {
            double repairs = author.Cash / 100.0 * rapePercent;
            StatUpdate(author, false, repairs);
            await author.SetCash(Context.User, author.Cash - repairs);
            await Context.User.NotifyAsync(Context.Channel, $"You got COUNTER-RAPED by **{user.Sanitize()}**! You just paid **{repairs:C2}** in asshole repairs.");
        }

        await author.SetCooldown("RapeCooldown", Constants.RAPE_COOLDOWN, Context.Guild, Context.User);
        return CommandResult.FromSuccess();
    }

    [Command("rob")]
    [Summary("Yoink money from a user.")]
    [Remarks("$rob Romanian 160")]
    [RequireCooldown("RobCooldown", "It's best to avoid getting caught if you don't go out for {0}.")]
    public async Task<RuntimeResult> Rob(IGuildUser user, double amount)
    {
        if (amount < Constants.ROB_MIN_CASH)
            return CommandResult.FromError($"There's no point in robbing for less than {Constants.ROB_MIN_CASH:C2}!");
        if (user.Id == Context.User.Id)
            return CommandResult.FromError("How are you supposed to rob yourself?");
        if (user.IsBot)
            return CommandResult.FromError("Nope.");

        DbUser author = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        DbUser target = await DbUser.GetById(Context.Guild.Id, user.Id);
        if (author.UsingSlots || target.UsingSlots)
            return CommandResult.FromError("One of you is using the slot machine. I cannot do any transactions at the moment.");
        if (target.Perks.ContainsKey("Pacifist"))
            return CommandResult.FromError($"You cannot rob **{user.Sanitize()}** as they have the Pacifist perk equipped.");

        double robMax = Math.Round(target.Cash / 100.0 * Constants.ROB_MAX_PERCENT, 2);
        if (author.Cash < amount)
            return CommandResult.FromError("You don't have that much money!");
        if (amount > robMax)
            return CommandResult.FromError($"You can only rob {Constants.ROB_MAX_PERCENT}% of **{user.Sanitize()}**'s cash, that being **{robMax:C2}**.");

        int roll = RandomUtil.Next(1, 101);
        double odds = author.UsedConsumables["Romanian Flag"] > 0 ? Constants.ROB_ODDS + 10 : Constants.ROB_ODDS;
        if (roll < odds)
        {
            await target.SetCash(user, target.Cash - amount);
            await author.SetCash(Context.User, author.Cash + amount);
            StatUpdate(author, true, amount);
            StatUpdate(target, false, amount);
            switch (RandomUtil.Next(2))
            {
                case 0:
                    await Context.User.NotifyAsync(Context.Channel, $"You beat the shit out of **{user.Sanitize()}** and took **{amount:C2}** from their ass!" +
                        $"\nBalance: {author.Cash:C2}");
                    break;
                case 1:
                    await Context.User.NotifyAsync(Context.Channel, $"You walked up to **{user.Sanitize()}** and yoinked **{amount:C2}** straight from their pocket, without a trace." +
                        $"\nBalance: {author.Cash:C2}");
                    break;
            }
        }
        else
        {
            await author.SetCash(Context.User, author.Cash - amount);
            StatUpdate(author, false, amount);
            switch (RandomUtil.Next(2))
            {
                case 0:
                    await Context.User.NotifyAsync(Context.Channel, $"You yoinked the money from **{user.Sanitize()}**, but they noticed and shanked you when you were on your way out." +
                        $" You lost all the resources in the process.\nBalance: {author.Cash:C2}");
                    break;
                case 1:
                    await Context.User.NotifyAsync(Context.Channel, "The dude happened to be a fed and threw your ass straight into jail. You lost all the resources in the process." +
                        $"\nBalance: {author.Cash:C2}");
                    break;
            }
        }

        await author.SetCooldown("RobCooldown", Constants.ROB_COOLDOWN, Context.Guild, Context.User);
        return CommandResult.FromSuccess();
    }

    [Command("scavenge", RunMode = RunMode.Async)]
    [Summary("Scavenge around the street for some goods.")]
    [RequireCooldown("ScavengeCooldown", "You're out of prowling energy for now. You should wait {0}.")]
    public async Task Scavenge()
    {
        using HttpClient client = new();
        string response = await client.GetStringAsync("https://www.thegamegal.com/wordgenerator/generator.php?game=2&category=6");
        JToken[] words = JObject.Parse(response)["words"].ToArray();
        string originalWord = words[RandomUtil.Next(words.Length)].ToString();
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        if (originalWord.Any(c => !char.IsLetter(c) && !char.IsWhiteSpace(c)))
        {
            await Scavenge();
            return;
        }

        switch (RandomUtil.Next(2))
        {
            case 0:
                string scrambled = Regex.Replace(originalWord, @"\w+", new MatchEvaluator(ScrambleWord), RegexOptions.IgnorePatternWhitespace);
                if (scrambled != "egg" && scrambled.Equals(originalWord, StringComparison.OrdinalIgnoreCase))
                {
                    await Scavenge();
                    return;
                }

                EmbedBuilder scrambleEmbed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithTitle("Scramble!")
                    .WithDescription($"**Unscramble this word:**\n{scrambled}\n*Type your response in the chat. You have {Constants.SCAVENGE_TIMEOUT} seconds!*");
                IUserMessage scrambleMsg = await ReplyAsync(embed: scrambleEmbed.Build());
                if (scrambled == "egg")
                {
                    await ReplyAsync("This egg is hard boiled, not scrambled. https://cdn.discordapp.com/attachments/661812833771847703/926190266904346725/video0.mov");
                    await user.UnlockAchievement("Hard Boiled Egg", Context.User, Context.Channel);
                }

                InteractiveResult<SocketMessage> scrambleResult = await Interactive.NextMessageAsync(
                    x => x.Channel.Id == Context.Channel.Id && x.Author.Id == Context.User.Id,
                    timeout: TimeSpan.FromSeconds(Constants.SCAVENGE_TIMEOUT)
                );
                await HandleScavenge(scrambleMsg, scrambleResult, user, scrambleResult.Value.Content.Equals(originalWord, StringComparison.OrdinalIgnoreCase),
                    $"**{Context.User.Sanitize()}**, that's right! The answer was **{originalWord}**.",
                    $"**{Context.User.Sanitize()}**, TIMEOUT! You failed to respond within 15 seconds. Well, the answer was **{originalWord}**.",
                    $"**{Context.User.Sanitize()}**, F and an L, broski. That was not the right answer. It was **{originalWord}**.");
                break;
            case 1:
                FormUrlEncodedContent content = new(new Dictionary<string, string>()
                {
                    { "q", originalWord },
                    { "source", "en" },
                    { "target", "es" }
                });
                HttpResponseMessage message = await client.PostAsync("https://libretranslate.de/translate", content);
                string translatedText = JObject.Parse(await message.Content.ReadAsStringAsync())["translatedText"].ToString();
                if (translatedText.Equals(originalWord, StringComparison.OrdinalIgnoreCase))
                {
                    await Scavenge();
                    return;
                }

                EmbedBuilder translateEmbed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithTitle("Translate!")
                    .WithDescription($"**Translate this Spanish word/phrase to English:**\n{translatedText}\n*Type your response in the chat. You have {Constants.SCAVENGE_TIMEOUT} seconds!*");
                IUserMessage translateMsg = await ReplyAsync(embed: translateEmbed.Build());

                InteractiveResult<SocketMessage> translateResult = await Interactive.NextMessageAsync(
                    x => x.Channel.Id == Context.Channel.Id && x.Author.Id == Context.User.Id,
                    timeout: TimeSpan.FromSeconds(Constants.SCAVENGE_TIMEOUT)
                );
                await HandleScavenge(translateMsg, translateResult, user, translateResult.Value.Content.Equals(originalWord, StringComparison.OrdinalIgnoreCase),
                    $"**{Context.User.Sanitize()}**, that's right! The answer was **{originalWord}**.",
                    $"**{Context.User.Sanitize()}**, TIMEOUT! You failed to respond within 15 seconds. Well, the answer was **{originalWord}**.",
                    $"**{Context.User.Sanitize()}**, F and an L, broski. That was not the right answer. It was **{originalWord}**.");

                break;
        }

        if (RandomUtil.Next(50) == 1)
            await ItemSystem.GiveCollectible("Ape NFT", Context.Channel, user);

        await user.SetCooldown("ScavengeCooldown", Constants.SCAVENGE_COOLDOWN, Context.Guild, Context.User);
    }

    [Command("slavery")]
    [Summary("Get some slave labor goin'.")]
    [RequireCooldown("SlaveryCooldown", "The slaves will die if you keep going like this! You should wait {0}.")]
    [RequireRankLevel("2")]
    public async Task<RuntimeResult> Slavery()
    {
        string[] successes = { "You got loads of newfags to tirelessly mine ender chests on the Oldest Anarchy Server in Minecraft. You made **{0}** selling the newfound millions of obsidian to an interested party.",
            "The innocent Uyghur children working in your labor factory did an especially good job making shoes in the past hour. You made **{0}** from all of them, and lost only like 2 cents paying them their wages.",
            "This cotton is BUSSIN! The Confederacy is proud. You have been awarded **{0}**." };
        string[] fails = { "Some fucker ratted you out and the police showed up. Thankfully, they're corrupt and you were able to sauce them **{0}** to fuck off. Thank the lord.",
            "A slave got away and yoinked **{0}** from you. Sad day." };
        return await GenericCrime(successes, fails, "SlaveryCooldown", Constants.SLAVERY_COOLDOWN);
    }

    [Command("whore")]
    [Summary("Sell your body for quick cash.")]
    [RequireCooldown("WhoreCooldown", "You cannot whore yourself out for {0}.")]
    [RequireRankLevel("1")]
    public async Task<RuntimeResult> Whore()
    {
        string[] successes = { "You went to the club and some weird fat dude sauced you **{0}**.",
            "The dude you fucked looked super shady, but he did pay up. You earned **{0}**.",
            "You found the Chad Thundercock himself! **{0}** and some amazing sex. What a great night." };
        string[] fails = { "You were too ugly and nobody wanted you. You lost **{0}** buying clothes for the night.",
            "You didn't give good enough head to the cop! You had to pay **{0}** in fines." };
        return await GenericCrime(successes, fails, "WhoreCooldown", Constants.WHORE_COOLDOWN);
    }

    private async Task<RuntimeResult> GenericCrime(string[] successOutcomes, string[] failOutcomes, string cdKey,
        long duration, bool hasMehOutcome = false)
    {
        DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
        if (user.UsingSlots)
            return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");

        double winOdds = user.Perks.ContainsKey("Speed Demon") ? Constants.GENERIC_CRIME_WIN_ODDS * 0.95 : Constants.GENERIC_CRIME_WIN_ODDS;
        if (RandomUtil.NextDouble(1, 101) < winOdds)
        {
            int outcomeNum = RandomUtil.Next(successOutcomes.Length);
            string outcome = successOutcomes[outcomeNum];
            double moneyEarned = RandomUtil.NextDouble(Constants.GENERIC_CRIME_WIN_MIN, Constants.GENERIC_CRIME_WIN_MAX);
            if (hasMehOutcome && outcomeNum == successOutcomes.Length - 1)
                moneyEarned /= 5;
            double totalCash = user.Cash + moneyEarned;

            StatUpdate(user, true, moneyEarned);
            await user.SetCash(Context.User, totalCash, Context.Channel, string.Format($"{outcome}\nBalance: {totalCash:C2}", moneyEarned.ToString("C2")));
        }
        else
        {
            string outcome = failOutcomes[RandomUtil.Next(failOutcomes.Length)];
            double lostCash = RandomUtil.NextDouble(Constants.GENERIC_CRIME_LOSS_MIN, Constants.GENERIC_CRIME_LOSS_MAX);
            lostCash = (user.Cash - lostCash) < 0 ? lostCash - Math.Abs(user.Cash - lostCash) : lostCash;
            double totalCash = (user.Cash - lostCash) > 0 ? user.Cash - lostCash : 0;

            StatUpdate(user, false, lostCash);
            await user.SetCash(Context.User, totalCash);
            await Context.User.NotifyAsync(Context.Channel, string.Format($"{outcome}\nBalance: {totalCash:C2}", lostCash.ToString("C2")));
        }

        if (RandomUtil.NextDouble(1, 101) < Constants.GENERIC_CRIME_TOOL_ODDS)
        {
            string[] availableTools = ItemSystem.tools.Where(t => !user.Tools.Contains(t.Name)).Select(t => t.Name).ToArray();
            if (availableTools.Length > 0)
            {
                string tool = availableTools[RandomUtil.Next(availableTools.Length)];
                user.Tools.Add(tool);
                await ReplyAsync($"Well I'll be damned! You also got yourself a(n) {tool}! Check out ``$module tasks`` to see how you can use it.");
            }
        }

        await user.SetCooldown(cdKey, duration, Context.Guild, Context.User);
        return CommandResult.FromSuccess();
    }

    private async Task HandleScavenge(IUserMessage msg, InteractiveResult<SocketMessage> result, DbUser user, bool successCondition, string successResponse, string timeoutResponse, string failureResponse)
    {
        if (!result.IsSuccess)
        {
            EmbedBuilder timeoutEmbed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle(msg.Embeds.First().Title)
                .WithDescription(timeoutResponse);
            await msg.ModifyAsync(x => x.Embed = timeoutEmbed.Build());
        }
        else if (successCondition)
        {
            double rewardCash = RandomUtil.NextDouble(Constants.SCAVENGE_MIN_CASH, Constants.SCAVENGE_MAX_CASH);
            double prestigeCash = rewardCash * (0.20 * user.Prestige);
            double totalCash = user.Cash + rewardCash + prestigeCash;
            EmbedBuilder successEmbed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle(msg.Embeds.First().Title)
                .WithDescription(successResponse + $" Here's {rewardCash:C2}.\nBalance: {totalCash:C2}\n{(prestigeCash != 0 ? $"*(+{prestigeCash:C2} from Prestige)*" : "")}");
            await msg.ModifyAsync(x => x.Embed = successEmbed.Build());
            await user.SetCash(Context.User, totalCash);
        }
        else
        {
            EmbedBuilder failureEmbed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle(msg.Embeds.First().Title)
                .WithDescription(failureResponse);
            await msg.ModifyAsync(x => x.Embed = failureEmbed.Build());
        }
    }

    private static string ScrambleWord(Match match)
    {
        double[] keys = new double[match.Value.Length];
        char[] letters = new char[match.Value.Length];
        for (int ctr = 0; ctr < match.Value.Length; ctr++)
        {
            keys[ctr] = RandomUtil.NextDouble(0, 2);
            letters[ctr] = match.Value[ctr];
        }
        Array.Sort(keys, letters, 0, match.Value.Length);
        return new(letters);
    }

    private static void StatUpdate(DbUser user, bool success, double gain)
    {
        CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
        culture.NumberFormat.CurrencyNegativePattern = 2;
        if (success)
        {
            user.AddToStats(new()
            {
                { "Crimes Succeeded", "1" },
                { "Money Gained from Crimes", gain.ToString("C2", culture) },
                { "Net Gain/Loss from Crimes", gain.ToString("C2", culture) }
            });
        }
        else
        {
            user.AddToStats(new()
            {
                { "Crimes Failed", "1" },
                { "Money Lost to Crimes", gain.ToString("C2", culture) },
                { "Net Gain/Loss from Crimes", (-gain).ToString("C2", culture) }
            });
        }
    }
}