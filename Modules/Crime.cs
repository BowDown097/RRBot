using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using RRBot.Entities;
using RRBot.Extensions;
using RRBot.Preconditions;
using RRBot.Systems;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace RRBot.Modules
{
    [Summary("Hell yeah! Crime! Reject the ways of being a law-abiding citizen for some cold hard cash and maybe even an item. Or, maybe not. Depends how good you are at being a criminal.")]
    [CheckPacifist]
    public class Crime : ModuleBase<SocketCommandContext>
    {
        public CultureInfo CurrencyCulture { get; set; }

        private async Task<RuntimeResult> GenericCrime(string outcome1, string outcome2, string outcome3, string outcome4, string outcome5, string cooldown, double duration, bool funny = false)
        {
            DbUser user = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
            if (user.UsingSlots)
                return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");

            double winOdds = user.Perks?.Keys.Contains("Speed Demon") == true
                ? Constants.GENERIC_CRIME_WIN_ODDS * 0.95 : Constants.GENERIC_CRIME_WIN_ODDS;
            if (RandomUtil.NextDouble(1, 101) < winOdds)
            {
                double moneyEarned = RandomUtil.NextDouble(Constants.GENERIC_CRIME_WIN_MIN, Constants.GENERIC_CRIME_WIN_MAX);
                double totalCash = user.Cash + moneyEarned;

                switch (RandomUtil.Next(3))
                {
                    case 0:
                        await Context.User.NotifyAsync(Context.Channel,
                            string.Format($"{outcome1}\nBalance: {totalCash:C2}", moneyEarned.ToString("C2")));
                        break;
                    case 1:
                        await Context.User.NotifyAsync(Context.Channel,
                            string.Format($"{outcome2}\nBalance: {totalCash:C2}", moneyEarned.ToString("C2")));
                        break;
                    case 2:
                        if (funny)
                        {
                            moneyEarned /= 5;
                            totalCash = user.Cash + moneyEarned;
                        }

                        await Context.User.NotifyAsync(Context.Channel,
                            string.Format($"{outcome3}\nBalance: {totalCash:C2}", moneyEarned.ToString("C2")));
                        break;
                }

                StatUpdate(user, true, moneyEarned);
                await user.SetCash(Context.User, Context.Channel, totalCash);
            }
            else
            {
                double lostCash = RandomUtil.NextDouble(Constants.GENERIC_CRIME_LOSS_MIN, Constants.GENERIC_CRIME_LOSS_MAX);
                lostCash = (user.Cash - lostCash) < 0 ? lostCash - Math.Abs(user.Cash - lostCash) : lostCash;
                double totalCash = (user.Cash - lostCash) > 0 ? user.Cash - lostCash : 0;

                switch (RandomUtil.Next(2))
                {
                    case 0:
                        await Context.User.NotifyAsync(Context.Channel, string.Format(outcome4 + $"\nBalance: {totalCash:C2}", lostCash.ToString("C2")));
                        break;
                    case 1:
                        await Context.User.NotifyAsync(Context.Channel, string.Format(outcome5 + $"\nBalance: {totalCash:C2}", lostCash.ToString("C2")));
                        break;
                }

                StatUpdate(user, false, lostCash);
                await user.SetCash(Context.User, Context.Channel, totalCash);
            }

            await RollRandomItem();
            user[cooldown] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(duration);
            await user.Write();
            return CommandResult.FromSuccess();
        }

        private async Task RollRandomItem()
        {
            if (RandomUtil.NextDouble(1, 101) < Constants.GENERIC_CRIME_ITEM_ODDS)
            {
                string item = await Items.RandomItem(Context.User);
                if (!string.IsNullOrEmpty(item))
                {
                    await Items.RewardItem(Context.User as IGuildUser, item);
                    await ReplyAsync($"Well I'll be damned! You also got yourself a(n) {item}! Check out ``$module tasks`` to see how you can use it.");
                }
            }
        }

        private void StatUpdate(DbUser user, bool success, double gain)
        {
            if (success)
            {
                user.AddToStats(CurrencyCulture, new Dictionary<string, string>
                {
                    { "Crimes Succeeded", "1" },
                    { "Money Gained from Crimes", gain.ToString("C2", CurrencyCulture) },
                    { "Net Gain/Loss from Crimes", gain.ToString("C2", CurrencyCulture) }
                });
            }
            else
            {
                user.AddToStats(CurrencyCulture, new Dictionary<string, string>
                {
                    { "Crimes Failed", "1" },
                    { "Money Lost to Crimes", gain.ToString("C2", CurrencyCulture) },
                    { "Net Gain/Loss from Crimes", (-gain).ToString("C2", CurrencyCulture) }
                });
            }
        }

        [Command("bully")]
        [Summary("Change the nickname of any victim you wish!")]
        [Remarks("$bully [user] [nickname]")]
        [RequireCooldown("BullyCooldown", "You cannot bully anyone for {0}.")]
        public async Task<RuntimeResult> Bully(IGuildUser user, [Remainder] string nickname)
        {
            char[] cleaned = nickname.Where(c => char.IsLetterOrDigit(c) || char.IsSymbol(c) || char.IsPunctuation(c)).ToArray();
            if (Filters.NWORD_REGEX.Matches(new string(cleaned).ToLower()).Count != 0)
                return CommandResult.FromError("You cannot bully someone to the funny word.");
            if (nickname.Length > 32)
                return CommandResult.FromError("The nickname you put is longer than the maximum accepted length (32).");
            if (user.Id == Context.User.Id)
                return CommandResult.FromError("No masochism here!");
            if (user.IsBot)
                return CommandResult.FromError("Nope.");

            DbUser target = await DbUser.GetById(Context.Guild.Id, user.Id);
            if (target.Perks?.ContainsKey("Pacifist") == true)
                return CommandResult.FromError($"You cannot bully **{user}** as they have the Pacifist perk equipped.");

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("roles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("houseRole", out ulong staffId))
            {
                if (user.RoleIds.Contains(staffId))
                    return CommandResult.FromError($"You cannot bully **{user}** as they are a staff member.");

                await user.ModifyAsync(props => props.Nickname = nickname);
                await Logger.Custom_UserBullied(user, Context.User, nickname);
                await ReplyAsync($"**{Context.User}** has **BULLIED** **{user}** to ``{nickname}``!");

                DbUser author = await DbUser.GetById(Context.Guild.Id, Context.User.Id);
                author.BullyCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.BULLY_COOLDOWN);
                await author.Write();
                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError("This server's staff role has yet to be set.");
        }

        [Command("deal")]
        [Summary("Deal some drugs.")]
        [Remarks("$deal")]
        [RequireCooldown("DealCooldown", "You don't have any more drugs to deal! Your next shipment comes in {0}.")]
        public async Task<RuntimeResult> Deal()
        {
            return await GenericCrime("Border patrol let your cocaine-stuffed dog through! You earned **{0}** from the cartel.",
                "You continue to capitalize off of some 17 year old's meth addiction, yielding you **{0}**.",
                "You sold grass to some elementary schoolers and passed it off as weed. They didn't have a lot of course, only **{0}**, but money's money.",
                "You tripped balls on acid with the boys at a party. After waking up, you realize not only did someone take money from your piggy bank, but you also gave out too much free acid, leaving you a whopping **{0}** poorer.",
                "The Democrats have launched yet another crime bill, leading to your hood being under heavy investigation. You could not escape the feds and paid **{0}** in fines.",
                "DealCooldown", Constants.DEAL_COOLDOWN, true);
        }

        [Command("loot")]
        [Summary("Loot some locations.")]
        [Remarks("$loot")]
        [RequireCooldown("LootCooldown", "You cannot loot for {0}.")]
        public async Task<RuntimeResult> Loot()
        {
            return await GenericCrime("You joined your local BLM protest, looted a Footlocker, and sold what you got. You earned **{0}**.",
                "That mall had a lot of shit! You earned **{0}**.",
                "You stole from a gas station because you're a fucking idiot. You earned **{0}**, basically nothing.",
                "There happened to be a cop coming out of the donut shop next door. You had to pay **{0}** in fines.",
                "The manager gave no fucks and beat the SHIT out of you. You lost **{0}** paying for face stitches.",
                "LootCooldown", Constants.LOOT_COOLDOWN, true);
        }

        [Alias("strugglesnuggle")]
        [Command("rape")]
        [Summary("Go out on the prowl for some ass!")]
        [Remarks("$rape [user]")]
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
            if (author.UsingSlots)
                return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");
            if (target.Perks?.ContainsKey("Pacifist") == true)
                return CommandResult.FromError($"You cannot bully **{user}** as they have the Pacifist perk equipped.");

            if (target.Cash > 0)
            {
                double rapePercent = RandomUtil.NextDouble(Constants.RAPE_MIN_PERCENT, Constants.RAPE_MAX_PERCENT);
                double winOdds = author.Perks?.ContainsKey("Speed Demon") == true ? Constants.RAPE_ODDS * 0.95 : Constants.RAPE_ODDS;
                if (RandomUtil.NextDouble(1, 101) < winOdds)
                {
                    double repairs = target.Cash / 100.0 * rapePercent;
                    StatUpdate(target, false, repairs);
                    await target.SetCash(user as SocketUser, Context.Channel, target.Cash - repairs);
                    await Context.User.NotifyAsync(Context.Channel, $"You DEMOLISHED **{user}**'s asshole! They just paid **{repairs:C2}** in asshole repairs.");
                }
                else
                {
                    double repairs = author.Cash / 100.0 * rapePercent;
                    StatUpdate(author, false, repairs);
                    await author.SetCash(Context.User, Context.Channel, author.Cash - repairs);
                    await Context.User.NotifyAsync(Context.Channel, $"You got COUNTER-RAPED by **{user}**! You just paid **{repairs:C2}** in asshole repairs.");
                }

                author.RapeCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.RAPE_COOLDOWN);
                await author.Write();
                await target.Write();
                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError($"Dear Lord, talk about kicking them while they're down! **{user}** is broke! Have some decency.");
        }

        [Command("rob")]
        [Summary("Yoink money from a user.")]
        [Remarks("$rob [user] [amount]")]
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
            if (author.UsingSlots)
                return CommandResult.FromError("You appear to be currently gambling. I cannot do any transactions at the moment.");
            if (target.Perks?.ContainsKey("Pacifist") == true)
                return CommandResult.FromError($"You cannot bully **{user}** as they have the Pacifist perk equipped.");

            double robMax = target.Cash / 100.0 * Constants.ROB_MAX_PERCENT;
            if (author.Cash < amount)
                return CommandResult.FromError("You don't have that much money!");
            if (amount > robMax)
                return CommandResult.FromError($"You can only rob {Constants.ROB_MAX_PERCENT}% of **{user}**'s cash, that being **{robMax:C2}**.");

            int roll = RandomUtil.Next(1, 101);
            if (roll < Constants.ROB_ODDS)
            {
                await target.SetCash(user as SocketUser, Context.Channel, target.Cash - amount);
                await author.SetCash(Context.User, Context.Channel, author.Cash + amount);
                switch (RandomUtil.Next(2))
                {
                    case 0:
                        await Context.User.NotifyAsync(Context.Channel, $"You beat the shit out of **{user}** and took **{amount:C2}** from their ass!" +
                            $"\nBalance: {author.Cash + amount:C2}");
                        break;
                    case 1:
                        await Context.User.NotifyAsync(Context.Channel, $"You walked up to **{user}** and yoinked **{amount:C2}** straight from their pocket, without a trace." +
                            $"\nBalance: {author.Cash + amount:C2}");
                        break;
                }
            }
            else
            {
                await author.SetCash(Context.User, Context.Channel, author.Cash - amount);
                switch (RandomUtil.Next(2))
                {
                    case 0:
                        await Context.User.NotifyAsync(Context.Channel, $"You yoinked the money from **{user}**, but they noticed and shanked you when you were on your way out." +
                            $" You lost all the resources in the process.\nBalance: {author.Cash - amount:C2}");
                        break;
                    case 1:
                        await Context.User.NotifyAsync(Context.Channel, "The dude happened to be a fed and threw your ass straight into jail. You lost all the resources in the process." +
                            $"\nBalance: {author.Cash - amount:C2}");
                        break;
                }
            }

            author.RobCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(Constants.ROB_COOLDOWN);
            await author.Write();
            return CommandResult.FromSuccess();
        }

        [Alias("slavelabor", "labor")]
        [Command("slavery")]
        [Summary("Get some slave labor goin'.")]
        [Remarks("$slavery")]
        [RequireCooldown("SlaveryCooldown", "The slaves will die if you keep going like this! You should wait {0}.")]
        [RequireRankLevel(2)]
        public async Task<RuntimeResult> Slavery()
        {
            return await GenericCrime("You got loads of newfags to tirelessly mine ender chests on the Oldest Anarchy Server in Minecraft. You made **{0}** selling the newfound millions of obsidian to an interested party.",
                "The innocent Uyghur children working in your labor factory did an especially good job making shoes in the past hour. You made **{0}** from all of them, and lost only like 2 cents paying them their wages.",
                "This cotton is BUSSIN! The Confederacy is proud. You have been awarded **{0}**.",
                "Some fucker ratted you out and the police showed up. Thankfully, they're corrupt and you were able to sauce them **{0}** to fuck off. Thank the lord.",
                "A slave got away and yoinked **{0}** from you. Sad day.",
                "SlaveryCooldown", Constants.SLAVERY_COOLDOWN);
        }

        [Command("whore")]
        [Summary("Sell your body for quick cash.")]
        [Remarks("$whore")]
        [RequireCooldown("WhoreCooldown", "You cannot whore yourself out for {0}.")]
        [RequireRankLevel(1)]
        public async Task<RuntimeResult> Whore()
        {
            return await GenericCrime("You went to the club and some weird fat dude sauced you **{0}**.",
                "The dude you fucked looked super shady, but he did pay up. You earned **{0}**.",
                "You found the Chad Thundercock himself! **{0}** and some amazing sex. What a great night.",
                "You were too ugly and nobody wanted you. You lost **{0}** buying clothes for the night.",
                "You didn't give good enough head to the cop! You had to pay **{0}** in fines.",
                "WhoreCooldown", Constants.WHORE_COOLDOWN);
        }
    }
}
