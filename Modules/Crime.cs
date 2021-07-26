using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using RRBot.Extensions;
using RRBot.Preconditions;
using RRBot.Systems;

namespace RRBot.Modules
{
    [Summary("Hell yeah! Crime! Reject the ways of being a law-abiding citizen for some cold hard cash and maybe even an item. Or, maybe not. Depends how good you are at being a criminal.")]
    public class Crime : ModuleBase<SocketCommandContext>
    {
        public CultureInfo CurrencyCulture { get; set; }
        public Logger Logger { get; set; }
        public static readonly Random random = new Random();

        private async Task RollRandomItem()
        {
            if (random.Next(20) == 1)
            {
                string item = await Items.RandomItem(Context.User as IGuildUser, random);
                if (!string.IsNullOrEmpty(item))
                {
                    await Items.RewardItem(Context.User as IGuildUser, item);
                    await ReplyAsync($"Well I'll be damned! You also got yourself a(n) {item}! Check out ``$module tasks`` to see how you can use it.");
                }
            }
        }

        private async Task StatUpdate(SocketUser user, bool success, double gain)
        {
            if (success)
            {
                await user.AddToStatsAsync(CurrencyCulture, Context.Guild, new Dictionary<string, string>
                {
                    { "Crimes Succeeded", "1" },
                    { "Money Gained from Crimes", gain.ToString("C2", CurrencyCulture) },
                    { "Net Gain/Loss from Crimes", gain.ToString("C2", CurrencyCulture) }
                });
            }
            else
            {
                await user.AddToStatsAsync(CurrencyCulture, Context.Guild, new Dictionary<string, string>
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
        [RequireCooldown("bullyCooldown", "you cannot bully anyone for {0}.")]
        public async Task<RuntimeResult> Bully(IGuildUser user, [Remainder] string nickname)
        {
            if (Filters.FUNNY_REGEX.Matches(new string(nickname.Where(char.IsLetter).ToArray()).ToLower()).Count != 0)
                return CommandResult.FromError($"{Context.User.Mention}, you cannot bully someone to the funny word.");
            if (nickname.Length > 32) 
                return CommandResult.FromError($"{Context.User.Mention}, the bully nickname is longer than the maximum accepted length.");
            if (user.Id == Context.User.Id) 
                return CommandResult.FromError($"{Context.User.Mention}, no masochism here!");
            if (user.IsBot) 
                return CommandResult.FromError("Nope.");

            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/config").Document("roles");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("houseRole", out ulong staffId))
            {
                if (user.RoleIds.Contains(staffId)) return CommandResult.FromError($"{Context.User.Mention}, you cannot bully someone who is a staff member.");

                await user.ModifyAsync(props => { props.Nickname = nickname; });
                await Logger.Custom_UserBullied(user, Context.User, nickname);
                await ReplyAsync($"**{Context.User.ToString()}** has **BULLIED** **{user.ToString()}** to ``{nickname}``!");

                DocumentReference userDoc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
                await doc.SetAsync(new { bullyCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(300) }, SetOptions.MergeAll);

                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError("This server's staff role has yet to be set.");
        }

        [Command("loot")]
        [Summary("Loot some locations.")]
        [Remarks("$loot")]
        [RequireCooldown("lootCooldown", "you cannot loot for {0}.")]
        public async Task<RuntimeResult> Loot()
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("usingSlots", out bool usingSlots) && usingSlots)
                return CommandResult.FromError($"{Context.User.Mention}, you appear to be currently gambling. I cannot do any transactions at the moment.");
            double cash = snap.GetValue<double>("cash");

            if (random.Next(10) < 8)
            {
                double moneyLooted = random.NextDouble(69, 691);
                await StatUpdate(Context.User, true, moneyLooted);

                switch (random.Next(3))
                {
                    case 0:
                        await Context.User.NotifyAsync(Context.Channel, "You joined your local BLM protest, looted a Footlocker, and sold what you got." +
                        $" You earned **{moneyLooted.ToString("C2")}**.");
                        break;
                    case 1:
                        await Context.User.NotifyAsync(Context.Channel, $"That mall had a lot of shit! You earned **{moneyLooted.ToString("C2")}**.");
                        break;
                    case 2:
                        moneyLooted /= 5;
                        await Context.User.NotifyAsync(Context.Channel, "You stole from a gas station because you're a fucking idiot." +
                        $" You earned **{moneyLooted.ToString("C2")}**, basically nothing.");
                        break;
                }

                await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash + moneyLooted);
            }
            else
            {
                double lostCash = random.NextDouble(69, 461);
                lostCash = (cash - lostCash) < 0 ? lostCash - Math.Abs(cash - lostCash) : lostCash;
                await StatUpdate(Context.User, false, lostCash);

                switch (random.Next(2))
                {
                    case 0:
                        await Context.User.NotifyAsync(Context.Channel, "There happened to be a cop coming out of the donut shop next door." +
                        $" You had to pay **{lostCash.ToString("C2")}** in fines.");
                        break;
                    case 1:
                        await Context.User.NotifyAsync(Context.Channel, "The manager gave no fucks and beat the **SHIT** out of you." +
                        $" You lost **{lostCash.ToString("C2")}** paying for face stitches.");
                        break;
                }

                await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash - lostCash);
            }

            await RollRandomItem();

            await doc.SetAsync(new { lootCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(3600) }, SetOptions.MergeAll);
            return CommandResult.FromSuccess();
        }

        [Alias("strugglesnuggle")]
        [Command("rape")]
        [Summary("Go out on the prowl for some ass!")]
        [Remarks("$rape [user]")]
        [RequireCash]
        [RequireCooldown("rapeCooldown", "you cannot rape for {0}.")]
        public async Task<RuntimeResult> Rape(IGuildUser user)
        {
            if (user.Id == Context.User.Id) return CommandResult.FromError($"{Context.User.Mention}, how are you supposed to rape yourself?");
            if (user.IsBot) return CommandResult.FromError("Nope.");

            CollectionReference users = Program.database.Collection($"servers/{Context.Guild.Id}/users");
            DocumentReference aDoc = users.Document(Context.User.Id.ToString());
            DocumentSnapshot aSnap = await aDoc.GetSnapshotAsync();
            if (aSnap.TryGetValue("usingSlots", out bool usingSlots) && usingSlots)
                return CommandResult.FromError($"{Context.User.Mention}, you appear to be currently gambling. I cannot do any transactions at the moment.");

            double aCash = aSnap.GetValue<double>("cash");
            DocumentSnapshot tSnap = await users.Document(user.Id.ToString()).GetSnapshotAsync();
            double tCash = tSnap.GetValue<double>("cash");

            if (tCash > 0)
            {
                double rapePercent = random.NextDouble(5, 9);
                if (random.Next(10) > 4)
                {
                    double repairs = tCash / 100.0 * rapePercent;
                    await StatUpdate(user as SocketUser, false, repairs);
                    await CashSystem.SetCash(user, Context.Channel, tCash - repairs);
                    await Context.User.NotifyAsync(Context.Channel, $"You DEMOLISHED **{user.ToString()}**'s asshole! They just paid **{repairs.ToString("C2")}** in asshole repairs.");
                }
                else
                {
                    double repairs = aCash / 100.0 * rapePercent;
                    await StatUpdate(Context.User, false, repairs);
                    await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, aCash - repairs);
                    await Context.User.NotifyAsync(Context.Channel, $"You got COUNTER-RAPED by **{user.ToString()}**! You just paid **{repairs.ToString("C2")}** in asshole repairs.");
                }

                await aDoc.SetAsync(new { rapeCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(3600) }, SetOptions.MergeAll);
                return CommandResult.FromSuccess();
            }

            return CommandResult.FromError($"{Context.User.Mention} Jesus man, talk about kicking them while they're down! **{user.ToString()}** is broke! Have some decency.");
        }

        [Alias("slavelabor", "labor")]
        [Command("slavery")]
        [Summary("Get some slave labor goin'.")]
        [Remarks("$slavery")]
        [RequireCooldown("slaveryCooldown", "the slaves will die if you keep going like this! You should wait {0}.")]
        [RequireRankLevel(2)]
        public async Task<RuntimeResult> Slavery()
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("usingSlots", out bool usingSlots) && usingSlots)
                return CommandResult.FromError($"{Context.User.Mention}, you appear to be currently gambling. I cannot do any transactions at the moment.");
            double cash = snap.GetValue<double>("cash");

            if (random.Next(10) < 8)
            {
                double moneyEarned = random.NextDouble(69, 691);
                await StatUpdate(Context.User, true, moneyEarned);

                switch (random.Next(3))
                {
                    case 0:
                        await Context.User.NotifyAsync(Context.Channel, "You got loads of newfags to tirelessly mine ender chests on the Oldest Anarchy Server in Minecraft." +
                        $" You made **{moneyEarned.ToString("C2")}** selling the newfound millions of obsidian to an interested party.");
                        break;
                    case 1:
                        await Context.User.NotifyAsync(Context.Channel, "The innocent Uyghur children working in your labor factory did an especially good job making shoes in the past hour." +
                        $" You made **{moneyEarned.ToString("C2")}** from all of them, and lost only like 2 cents paying them their wages.");
                        break;
                    case 2:
                        await Context.User.NotifyAsync(Context.Channel, $"This cotton is BUSSIN! The Confederacy is proud. You have been awarded **{moneyEarned.ToString("C2")}**.");
                        break;
                }

                await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash + moneyEarned);
            }
            else
            {
                double lostCash = random.NextDouble(69, 461);
                lostCash = (cash - lostCash) < 0 ? lostCash - Math.Abs(cash - lostCash) : lostCash;
                await StatUpdate(Context.User, false, lostCash);

                switch (random.Next(2))
                {
                    case 0:
                        await Context.User.NotifyAsync(Context.Channel, "Some fucker ratted you out and the police showed up." +
                        $" Thankfully, they're corrupt and you were able to sauce them **{lostCash.ToString("C2")}** to fuck off. Thank the lord.");
                        break;
                    case 1:
                        await Context.User.NotifyAsync(Context.Channel, $"A slave got away and yoinked **{lostCash.ToString("C2")}** from you. Sad day.");
                        break;
                }

                await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash - lostCash);
            }

            await RollRandomItem();

            await doc.SetAsync(new { slaveryCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(3600) }, SetOptions.MergeAll);
            return CommandResult.FromSuccess();
        }

        [Command("whore")]
        [Summary("Sell your body for quick cash.")]
        [Remarks("$whore")]
        [RequireCooldown("whoreCooldown", "you cannot whore yourself out for {0}.")]
        [RequireRankLevel(1)]
        public async Task<RuntimeResult> Whore()
        {
            DocumentReference doc = Program.database.Collection($"servers/{Context.Guild.Id}/users").Document(Context.User.Id.ToString());
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.TryGetValue("usingSlots", out bool usingSlots) && usingSlots)
                return CommandResult.FromError($"{Context.User.Mention}, you appear to be currently gambling. I cannot do any transactions at the moment.");

            double cash = snap.GetValue<double>("cash");

            if (random.Next(10) < 8)
            {
                double moneyWhored = random.NextDouble(69, 691);
                await StatUpdate(Context.User, true, moneyWhored);

                switch (random.Next(3))
                {
                    case 0:
                        await Context.User.NotifyAsync(Context.Channel, $"You went to the club and some weird fat dude sauced you **{moneyWhored.ToString("C2")}**.");
                        break;
                    case 1:
                        await Context.User.NotifyAsync(Context.Channel, $"The dude you fucked looked super shady, but he did pay up. You earned **{moneyWhored.ToString("C2")}**.");
                        break;
                    case 2:
                        await Context.User.NotifyAsync(Context.Channel, $"You found the Chad Thundercock himself! **{moneyWhored.ToString("C2")}** and some amazing sex." +
                        $" What a great night.");
                        break;
                }

                await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash + moneyWhored);
            }
            else
            {
                double lostCash = random.NextDouble(69, 461);
                lostCash = (cash - lostCash) < 0 ? lostCash - Math.Abs(cash - lostCash) : lostCash;
                await StatUpdate(Context.User, false, lostCash);

                switch (random.Next(2))
                {
                    case 0:
                        await Context.User.NotifyAsync(Context.Channel, $"You were too ugly and nobody wanted you. You lost **{lostCash.ToString("C2")}** buying clothes for the night.");
                        break;
                    case 1:
                        await Context.User.NotifyAsync(Context.Channel, $"You didn't give good enough head to the cop! You had to pay **{lostCash.ToString("C2")}** in fines.");
                        break;
                }

                await CashSystem.SetCash(Context.User as IGuildUser, Context.Channel, cash - lostCash);
            }

            await doc.SetAsync(new { whoreCooldown = DateTimeOffset.UtcNow.ToUnixTimeSeconds(3600) }, SetOptions.MergeAll);
            return CommandResult.FromSuccess();
        }
    }
}
