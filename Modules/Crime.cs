namespace RRBot.Modules;
[Summary("Throw your morals out the window. It's time to make dough.")]
[CheckPacifist]
public partial class Crime : ModuleBase<SocketCommandContext>
{
    public InteractiveService Interactive { get; set; } = null!;

    [Command("bully")]
    [Summary("Change the nickname of any victim you wish!")]
    [Remarks("$bully \"John Boyer#2168\" gay lol")]
    [RequireCooldown("BullyCooldown", "You cannot bully anyone for {0}.")]
    [DoNotSanitize]
    public async Task<RuntimeResult> Bully(IGuildUser user, [Remainder] string nickname)
    {
        if (nickname.Length > 32)
            return CommandResult.FromError("The nickname you put is longer than the maximum accepted length (32).");
        if (user.Id == Context.User.Id)
            return CommandResult.FromError("No masochism here!");
        if (user.IsBot)
            return CommandResult.FromError("Nope.");
        
        DbUser target = await MongoManager.FetchUserAsync(user.Id, Context.Guild.Id);
        if (target.Perks.ContainsKey("Pacifist"))
            return CommandResult.FromError($"You cannot bully **{user.Sanitize()}** as they have the Pacifist perk equipped.");
        
        DbConfigRoles roles = await MongoManager.FetchConfigAsync<DbConfigRoles>(Context.Guild.Id);
        if (user.RoleIds.Contains(roles.StaffLvl1Role) || user.RoleIds.Contains(roles.StaffLvl2Role))
            return CommandResult.FromError($"You cannot bully **{user.Sanitize()}** as they are a staff member.");

        await user.ModifyAsync(props => props.Nickname = nickname);
        await LoggingSystem.Custom_UserBullied(user, Context.User, nickname);
        await Context.User.NotifyAsync(Context.Channel, $"You BULLIED **{user.Sanitize()}** to **{StringCleaner.Sanitize(nickname)}**!");
        
        DbUser author = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        await author.SetCooldown("BullyCooldown", Constants.BullyCooldown, Context.User);
        await MongoManager.UpdateObjectAsync(author);

        return CommandResult.FromSuccess();
    }

    [Command("deal")]
    [Summary("Deal some drugs.")]
    [RequireCooldown("DealCooldown", "You don't have any more drugs to deal! Your next shipment comes in {0}.")]
    public async Task<RuntimeResult> Deal()
    {
        string[] successes =
        [
            "Border patrol let your cocaine-stuffed dog through! You earned **{0}** from the cartel.",
            "You continue to capitalize off of some 17 year old's amphetamine addiction, yielding you **{0}**.",
            "You sold grass to some elementary schoolers and passed it off as weed. They didn't have a lot of course, only **{0}**, but money's money."
        ];
        string[] fails =
        [
            "You tripped balls on acid with the boys at a party. After waking up, you realize someone took all the money from your piggy bank, leaving you a whopping **{0}** poorer.",
            "The DEA were tipped off about your meth lab and you got caught red handed. You paid **{0}** in fines."
        ];
        return await GenericCrime(successes, fails, "DealCooldown", Constants.DealCooldown, true);
    }

    [Command("hack")]
    [Summary("Hack into someone's crypto wallet.")]
    [Remarks("$hack LYNESTAR XRP 10000")]
    [RequireCooldown("HackCooldown", "You exhausted all your brain power bro, you're gonna have to wait {0}.")]
    public async Task<RuntimeResult> Hack(IGuildUser user, string crypto, decimal amount)
    {
        if (amount < Constants.InvestmentMinAmount)
            return CommandResult.FromError($"You must hack {Constants.InvestmentMinAmount} or more.");
        if (user.Id == Context.User.Id)
            return CommandResult.FromError("How are you supposed to hack yourself?");
        if (user.IsBot)
            return CommandResult.FromError("Nope.");

        string? abbreviation = Investments.ResolveAbbreviation(crypto);
        if (abbreviation is null)
            return CommandResult.FromError($"**{crypto}** is not a currently accepted currency!");
        
        DbUser author = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        DbUser target = await MongoManager.FetchUserAsync(user.Id, Context.Guild.Id);
        if (target.UsingSlots)
            return CommandResult.FromError($"**{user.Sanitize()}** is currently gambling. They cannot do any transactions at the moment.");
        if (target.Perks.ContainsKey("Pacifist"))
            return CommandResult.FromError($"You cannot hack **{user.Sanitize()}** as they have the Pacifist perk equipped.");

        decimal authorBal = (decimal)author[abbreviation];
        decimal targetBal = (decimal)target[abbreviation];
        decimal robMax = Math.Round(targetBal / 100.0m * Constants.RobMaxPercent, 4);
        if (authorBal < amount)
            return CommandResult.FromError($"You don't have that much {abbreviation.ToUpper()}!");
        if (amount > robMax)
            return CommandResult.FromError($"You can only hack {Constants.RobMaxPercent}% of **{user.Sanitize()}**'s {abbreviation.ToUpper()}, that being **{robMax}**.");

        int roll = RandomUtil.Next(100);
        decimal cryptoValue = await Investments.QueryCryptoValue(abbreviation) * amount;
        double odds = author.UsedConsumables.GetValueOrDefault("Black Hat") > 0 ? Constants.HackOdds + 10 : Constants.HackOdds;
        if (author.Perks.ContainsKey("Speed Demon"))
            odds *= 0.95;
        if (roll < odds)
        {
            target[abbreviation] = targetBal - amount;
            author[abbreviation] = authorBal + amount;
            StatUpdate(author, true, cryptoValue);
            StatUpdate(target, false, cryptoValue);
            switch (RandomUtil.Next(2))
            {
                case 0:
                    await Context.User.NotifyAsync(Context.Channel,
                        $"The dumbass pushed his private keys to GitHub LMFAO! You sniped that shit and got **{amount:0.####} {abbreviation.ToUpper()}**.");
                    break;
                case 1:
                    await Context.User.NotifyAsync(Context.Channel,
                        $"You did an ol' SIM swap on {user.Sanitize()}'s phone while they weren't looking and yoinked **{amount:0.####} {abbreviation.ToUpper()}** right off their Coinbase. Easy claps!");
                    break;
            }
        }
        else
        {
            author[abbreviation] = authorBal - amount / 4;
            StatUpdate(author, false, amount / 4);
            switch (RandomUtil.Next(2))
            {
                case 0:
                    await Context.User.NotifyAsync(Context.Channel,
                        $"**{user.Sanitize()}** actually secured their shit properly, got your info, and sent it off to the feds. You got raided and lost **{amount / 4:0.####} {abbreviation.ToUpper()}** in the process.");
                    break;
                case 1:
                    await Context.User.NotifyAsync(Context.Channel,
                        $"That hacker dude on Instagram scammed your ass! You only had to pay 1/4 of what you were promising starting off, but still sucks. There goes **{amount / 4:0.####} {abbreviation.ToUpper()}**.");
                    break;
            }
        }

        await author.SetCooldown("HackCooldown", Constants.HackCooldown, Context.User);
        await MongoManager.UpdateObjectAsync(author);
        await MongoManager.UpdateObjectAsync(target);
        return CommandResult.FromSuccess();
    }

    [Command("loot")]
    [Summary("Loot some locations.")]
    [RequireCooldown("LootCooldown", "You cannot loot for {0}.")]
    public async Task<RuntimeResult> Loot()
    {
        string[] successes =
        [
            "You joined your local protest, looted a Footlocker, and sold what you got. You earned **{0}**.",
            "That mall had a lot of shit! You earned **{0}**.",
            "You stole from a gas station because you're a fucking idiot. You earned **{0}**, basically nothing."
        ];
        string[] fails =
        [
            "There happened to be a cop coming out of the donut shop next door. You had to pay **{0}** in fines.",
            "The manager gave no fucks and beat the SHIT out of you. You lost **{0}** paying for face stitches."
        ];
        return await GenericCrime(successes, fails, "LootCooldown", Constants.LootCooldown, true);
    }

    [Alias("strugglesnuggle")]
    [Command("rape")]
    [Summary("Go out on the prowl for some ass!")]
    [Remarks("$rape \"BowDown's Kitten\"")]
    [RequireCash]
    [RequireCooldown("RapeCooldown", "You cannot rape for {0}.")]
    public async Task<RuntimeResult> Rape([Remainder] IGuildUser user)
    {
        if (user.Id == Context.User.Id)
            return CommandResult.FromError("How are you supposed to rape yourself?");
        if (user.IsBot)
            return CommandResult.FromError("Nope.");
        
        DbUser author = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        DbUser target = await MongoManager.FetchUserAsync(user.Id, Context.Guild.Id);
        if (target.UsingSlots)
            return CommandResult.FromError($"**{user.Sanitize()}** is currently gambling. They cannot do any transactions at the moment.");
        if (target.Perks.ContainsKey("Pacifist"))
            return CommandResult.FromError($"You cannot rape **{user.Sanitize()}** as they have the Pacifist perk equipped.");
        if (target.Cash < 0.01m)
            return CommandResult.FromError($"Dear Lord, talk about kicking them while they're down! **{user.Sanitize()}** is broke! Have some decency.");

        decimal rapePercent = RandomUtil.NextDecimal(Constants.RapeMinPercent, Constants.RapeMaxPercent);
        double odds = author.UsedConsumables.GetValueOrDefault("Viagra") > 0 ? Constants.RapeOdds + 10 : Constants.RapeOdds;
        if (author.Perks.ContainsKey("Speed Demon"))
            odds *= 0.95;
        if (RandomUtil.NextDouble(1, 101) < odds)
        {
            decimal repairs = target.Cash / 100.0m * rapePercent;
            StatUpdate(target, false, repairs);
            await target.SetCash(user, target.Cash - repairs);
            await Context.User.NotifyAsync(Context.Channel,
                $"You DEMOLISHED **{user.Sanitize()}**'s asshole! They just paid **{repairs:C2}** in asshole repairs.");
        }
        else
        {
            decimal repairs = author.Cash / 100.0m * rapePercent;
            StatUpdate(author, false, repairs);
            await author.SetCash(Context.User, author.Cash - repairs);
            await Context.User.NotifyAsync(Context.Channel,
                $"You got COUNTER-RAPED by **{user.Sanitize()}**! You just paid **{repairs:C2}** in asshole repairs.");
        }

        await author.SetCooldown("RapeCooldown", Constants.RapeCooldown, Context.User);
        await MongoManager.UpdateObjectAsync(author);
        await MongoManager.UpdateObjectAsync(target);
        return CommandResult.FromSuccess();
    }

    [Command("rob")]
    [Summary("Yoink money from a user.")]
    [Remarks("$rob Alexandru 160")]
    [RequireCooldown("RobCooldown", "It's best to avoid getting caught if you don't go out for {0}.")]
    public async Task<RuntimeResult> Rob(IGuildUser user, decimal amount)
    {
        if (amount < Constants.RobMinCash)
            return CommandResult.FromError($"There's no point in robbing for less than {Constants.RobMinCash:C2}!");
        if (user.Id == Context.User.Id)
            return CommandResult.FromError("How are you supposed to rob yourself?");
        if (user.IsBot)
            return CommandResult.FromError("Nope.");
        
        DbUser author = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        DbUser target = await MongoManager.FetchUserAsync(user.Id, Context.Guild.Id);
        if (target.UsingSlots)
            return CommandResult.FromError($"**{user.Sanitize()}** is currently gambling. They cannot do any transactions at the moment.");
        if (target.Perks.ContainsKey("Pacifist"))
            return CommandResult.FromError($"You cannot rob **{user.Sanitize()}** as they have the Pacifist perk equipped.");

        decimal robMax = Math.Round(target.Cash / 100.0m * Constants.RobMaxPercent, 2);
        if (author.Cash < amount)
            return CommandResult.FromError("You don't have that much money!");
        if (amount > robMax)
            return CommandResult.FromError($"You can only rob {Constants.RobMaxPercent}% of **{user.Sanitize()}**'s cash, that being **{robMax:C2}**.");

        int roll = RandomUtil.Next(100);
        double odds = author.UsedConsumables.GetValueOrDefault("Romanian Flag") > 0 ? Constants.RobOdds + 10 : Constants.RobOdds;
        if (author.Perks.ContainsKey("Speed Demon"))
            odds *= 0.95;
        if (roll < odds)
        {
            await target.SetCash(user, target.Cash - amount);
            await author.SetCash(Context.User, author.Cash + amount);
            StatUpdate(author, true, amount);
            StatUpdate(target, false, amount);
            switch (RandomUtil.Next(2))
            {
                case 0:
                    await Context.User.NotifyAsync(Context.Channel,
                        $"You beat the shit out of **{user.Sanitize()}** and took **{amount:C2}** from their ass!\nBalance: {author.Cash:C2}");
                    break;
                case 1:
                    await Context.User.NotifyAsync(Context.Channel,
                        $"You walked up to **{user.Sanitize()}** and yoinked **{amount:C2}** straight from their pocket, without a trace.\nBalance: {author.Cash:C2}");
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
                    await Context.User.NotifyAsync(Context.Channel, 
                        $"You yoinked the money from **{user.Sanitize()}**, but they noticed and shanked you when you were on your way out. You lost all the resources in the process.\nBalance: {author.Cash:C2}");
                    break;
                case 1:
                    await Context.User.NotifyAsync(Context.Channel, 
                        $"The dude happened to be a cop and threw your ass straight into jail. You lost all the resources in the process.\nBalance: {author.Cash:C2}");
                    break;
            }
        }

        await author.SetCooldown("RobCooldown", Constants.RobCooldown, Context.User);
        await MongoManager.UpdateObjectAsync(author);
        await MongoManager.UpdateObjectAsync(target);
        return CommandResult.FromSuccess();
    }

    [Command("scavenge", RunMode = RunMode.Async)]
    [Summary("Scavenge around the street for some goods.")]
    [RequireCooldown("ScavengeCooldown", "You're out of prowling energy for now. You should wait {0}.")]
    public async Task Scavenge()
    {
        using HttpClient client = new();
        (string word, string espanol) = RandomUtil.GetRandomElement(Constants.ScavengeWordSet);
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);

        switch (RandomUtil.Next(2))
        {
            case 0:
                string scrambled = AlphanumericRegex().Replace(word, ScrambleWord);
                while (scrambled.Equals(word, StringComparison.OrdinalIgnoreCase) && scrambled != "egg")
                    scrambled = AlphanumericRegex().Replace(word, ScrambleWord);

                EmbedBuilder scrambleEmbed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithTitle("Scramble!")
                    .WithDescription($"**Unscramble this word:**\n{scrambled}\n*Type your response in the chat. You have {Constants.ScavengeTimeout} seconds!*");
                IUserMessage scrambleMsg = await ReplyAsync(embed: scrambleEmbed.Build());
                if (scrambled == "egg")
                {
                    await ReplyAsync("This egg is hard boiled, not scrambled. https://cdn.discordapp.com/attachments/661812833771847703/926190266904346725/video0.mov");
                    await user.UnlockAchievement("Hard Boiled Egg", Context.User, Context.Channel);
                }

                InteractiveResult<SocketMessage?> scrambleResult = await Interactive.NextMessageAsync(
                    x => x.Channel.Id == Context.Channel.Id && x.Author.Id == Context.User.Id,
                    timeout: TimeSpan.FromSeconds(Constants.ScavengeTimeout)
                );
                string scrambleContent = scrambleResult.Value?.Content ?? "";
                await HandleScavenge(scrambleMsg, scrambleResult, user, scrambleContent.Equals(word, StringComparison.OrdinalIgnoreCase),
                    $"**{Context.User.Sanitize()}**, that's right! The answer was **{word}**.",
                    $"**{Context.User.Sanitize()}**, TIMEOUT! You failed to respond within 15 seconds. Well, the answer was **{word}**.",
                    $"**{Context.User.Sanitize()}**, F and an L, broski. That was not the right answer. It was **{word}**.");
                break;
            case 1:
                EmbedBuilder translateEmbed = new EmbedBuilder()
                    .WithColor(Color.Red)
                    .WithTitle("Translate!")
                    .WithDescription($"**Translate this Spanish word/phrase to English:**\n{espanol}\n*Type your response in the chat. You have {Constants.ScavengeTimeout} seconds!*");
                IUserMessage translateMsg = await ReplyAsync(embed: translateEmbed.Build());

                InteractiveResult<SocketMessage?> translateResult = await Interactive.NextMessageAsync(
                    x => x.Channel.Id == Context.Channel.Id && x.Author.Id == Context.User.Id,
                    timeout: TimeSpan.FromSeconds(Constants.ScavengeTimeout)
                );
                string translateContent = translateResult.Value?.Content ?? "";
                await HandleScavenge(translateMsg, translateResult, user, translateContent.Equals(word, StringComparison.OrdinalIgnoreCase),
                    $"**{Context.User.Sanitize()}**, that's right! The answer was **{word}**.",
                    $"**{Context.User.Sanitize()}**, TIMEOUT! You failed to respond within 15 seconds. Well, the answer was **{word}**.",
                    $"**{Context.User.Sanitize()}**, F and an L, broski. That was not the right answer. It was **{word}**.");

                break;
        }

        if (RandomUtil.Next(50) == 1)
            await ItemSystem.GiveCollectible("Ape NFT", Context.Channel, user);

        await user.SetCooldown("ScavengeCooldown", Constants.ScavengeCooldown, Context.User);
        await MongoManager.UpdateObjectAsync(user);
    }

    [Command("slavery")]
    [Summary("Get some slave labor goin'.")]
    [RequireCooldown("SlaveryCooldown", "The slaves will die if you keep going like this! You should wait {0}.")]
    [RequireRankLevel(2)]
    public async Task<RuntimeResult> Slavery()
    {
        string[] successes =
        [
            "You got loads of 12 year olds to tirelessly mine ender chests on the Oldest Anarchy Server in Minecraft. You made **{0}** selling the newfound millions of obsidian to an interested party.",
            "The children working in your labor factory did a good job making shoes in the past hour. You made **{0}** from all of them, and lost only like 2 cents paying them their wages.",
            "This cotton is BUSSIN! The Confederacy is proud. You have been awarded **{0}**."
        ];
        string[] fails =
        [
            "Some dude died from inhumane working conditions and you had to cobble together **{0}** for his family. As if that's gonna do anything for those losers, though.",
            "A slave got away and yoinked **{0}** from you. Sad day."
        ];
        return await GenericCrime(successes, fails, "SlaveryCooldown", Constants.SlaveryCooldown);
    }

    [Command("whore")]
    [Summary("Sell your body for quick cash.")]
    [RequireCooldown("WhoreCooldown", "You cannot whore yourself out for {0}.")]
    [RequireRankLevel(1)]
    public async Task<RuntimeResult> Whore()
    {
        string[] successes =
        [
            "You went to the club and some weird fat dude sauced you **{0}**.",
            "The dude you fucked looked super shady, but he did pay up. You earned **{0}**.",
            "You found the Chad Thundercock himself! **{0}** and some amazing sex. What a great night."
        ];
        string[] fails =
        [
            "You were too ugly and nobody wanted you. You lost **{0}** buying clothes for the night.",
            "An undercover cop busted you for prostitution! There goes **{0}**."
        ];
        return await GenericCrime(successes, fails, "WhoreCooldown", Constants.WhoreCooldown);
    }

    [GeneratedRegex(@"\w+", RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex AlphanumericRegex();
}